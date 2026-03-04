import logging
from pathlib import Path

import fitz  # pymupdf
from fastapi import UploadFile
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from app.config import settings
from app.models.document import Chunk, Document
from app.services.chunking.base import ChunkResult, ChunkingStrategy
from app.services.chunking.detector import detect_filetype_category, detect_pdf_category
from app.services.chunking.docx_chunker import DocxChunker
from app.services.chunking.html_chunker import HtmlChunker
from app.services.chunking.pdf_chunker import (
    ExerciseChunker,
    SlidesExportChunker,
    TextbookChunker,
)
from app.services.chunking.pptx_chunker import PptxChunker
from app.services.chunking.semantic_splitter import semantic_split_chunks
from app.services.embedding_service import embed_chunks

logger = logging.getLogger(__name__)


async def upload_document(db: AsyncSession, file: UploadFile) -> Document:
    """Save uploaded file and create a document record."""
    filename = file.filename or "unknown"
    filetype = Path(filename).suffix.lstrip(".")
    filepath = settings.upload_path / filename

    content = await file.read()
    filepath.write_bytes(content)

    doc = Document(
        title=Path(filename).stem,
        filename=filename,
        filepath=str(filepath),
        filetype=filetype,
        filesize=len(content),
        processing_status="pending",
    )
    db.add(doc)
    await db.flush()
    return doc


async def process_document(db: AsyncSession, doc_id: int) -> Document:
    """Run the full processing pipeline: detect → chunk → embed."""
    doc = await db.get(Document, doc_id)
    if not doc:
        raise ValueError(f"Document {doc_id} not found")

    doc.processing_status = "processing"
    await db.flush()

    try:
        # 1. Detect category and get chunker
        category, chunker = await _detect_and_get_chunker(doc)
        doc.doc_category = category

        # 2. Get content for chunker
        content = _get_content(doc)

        # 3. Chunk
        metadata = {"title": doc.title, "filename": doc.filename}
        chunk_results = await chunker.chunk(content, metadata)

        # 4. Save chunks to DB
        await _save_chunks(db, doc.id, chunk_results)

        # 4.5 AI semantic split (large multi-topic chunks → focused sub-chunks)
        split_count = await semantic_split_chunks(db, doc.id)
        if split_count:
            logger.info("Semantic split created %d additional chunks for doc %d", split_count, doc.id)

        # 5. Generate embeddings
        await embed_chunks(db, doc.id)

        # 6. Extract knowledge entities
        from app.services.knowledge_extraction import extract_entities_from_document
        await extract_entities_from_document(db, doc.id)

        doc.processing_status = "done"
    except Exception:
        logger.exception("Failed to process document %d", doc_id)
        doc.processing_status = "error"

    await db.commit()
    return doc


async def get_documents(db: AsyncSession) -> list[Document]:
    result = await db.execute(select(Document).order_by(Document.created_at.desc()))
    return list(result.scalars().all())


async def get_document(db: AsyncSession, doc_id: int) -> Document | None:
    result = await db.execute(
        select(Document).where(Document.id == doc_id).options(selectinload(Document.chunks))
    )
    return result.scalar_one_or_none()


async def get_chunks(db: AsyncSession, doc_id: int) -> list[Chunk]:
    result = await db.execute(
        select(Chunk).where(Chunk.document_id == doc_id).order_by(Chunk.chunk_index)
    )
    return list(result.scalars().all())


async def delete_document(db: AsyncSession, doc_id: int) -> bool:
    doc = await db.get(Document, doc_id)
    if not doc:
        return False

    # Delete file from disk
    filepath = Path(doc.filepath)
    if filepath.exists():
        filepath.unlink()

    await db.delete(doc)
    await db.commit()
    return True


async def _detect_and_get_chunker(doc: Document) -> tuple[str, ChunkingStrategy]:
    """Detect document category and return the appropriate chunker."""
    ft = doc.filetype.lower()

    if ft == "pdf":
        pages = _extract_pdf_pages(doc.filepath)
        category = detect_pdf_category(pages)
        chunker_map: dict[str, ChunkingStrategy] = {
            "slides_export": SlidesExportChunker(),
            "textbook": TextbookChunker(),
            "exercise_sheet": ExerciseChunker(),
            "paper": TextbookChunker(),
            "scan": SlidesExportChunker(),
        }
        return category, chunker_map.get(category, TextbookChunker())

    if ft == "pptx":
        return "slides_export", PptxChunker()
    if ft == "docx":
        return "textbook", DocxChunker()
    if ft in ("html", "htm"):
        return "textbook", HtmlChunker()

    category = detect_filetype_category(ft)
    return category, TextbookChunker()


def _get_content(doc: Document):
    """Get content appropriate for the chunker."""
    ft = doc.filetype.lower()

    if ft == "pdf":
        return _extract_pdf_pages(doc.filepath)
    if ft in ("pptx", "docx"):
        return doc.filepath
    if ft in ("html", "htm"):
        return Path(doc.filepath).read_text(encoding="utf-8")

    # Fallback: read as text, return as single-page list
    return [Path(doc.filepath).read_text(encoding="utf-8")]


def _extract_pdf_pages(filepath: str) -> list[str]:
    """Extract text from each page of a PDF using PyMuPDF."""
    pdf = fitz.open(filepath)
    pages = [page.get_text() for page in pdf]
    pdf.close()
    return pages


async def _save_chunks(
    db: AsyncSession, doc_id: int, results: list[ChunkResult]
) -> None:
    for r in results:
        chunk = Chunk(
            document_id=doc_id,
            content=r.content,
            chunk_index=r.chunk_index,
            chunk_type=r.chunk_type,
            topic_label=r.topic_label,
            section_heading=r.section_heading,
            page_number=r.page_number,
            metadata_json=r.metadata if r.metadata else None,
        )
        db.add(chunk)
    await db.flush()

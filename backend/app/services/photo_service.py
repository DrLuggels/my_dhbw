"""Photo upload → Gemini Vision OCR → Document → Chunking pipeline."""

import logging
from pathlib import Path

from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.models.document import Document
from app.services.ai_service import ai_service
from app.services.chunking.semantic_splitter import semantic_split_chunks
from app.services.document_service import _save_chunks
from app.services.chunking.base import ChunkResult
from app.services.embedding_service import embed_chunks

logger = logging.getLogger(__name__)

OCR_PROMPT = (
    "Extrahiere den gesamten Text aus diesem Bild. "
    "Behalte die Struktur bei (Überschriften, Aufzählungen, Absätze). "
    "Wenn es sich um eine Vorlesungsfolie handelt, gib auch den Folientitel an. "
    "Antworte ausschließlich mit dem extrahierten Text, keine Erklärungen."
)


async def process_photo(
    db: AsyncSession, filename: str, content: bytes
) -> Document:
    """Full pipeline: save → OCR → chunk → embed → extract entities."""
    filepath = settings.upload_path / filename
    filepath.write_bytes(content)

    doc = Document(
        title=Path(filename).stem,
        filename=filename,
        filepath=str(filepath),
        filetype=Path(filename).suffix.lstrip(".").lower(),
        filesize=len(content),
        doc_category="photo",
        processing_status="processing",
    )
    db.add(doc)
    await db.flush()

    try:
        # 1. Gemini Vision OCR
        extracted_text = await ai_service.vision_gemini(content, OCR_PROMPT)
        if not extracted_text.strip():
            doc.processing_status = "error"
            await db.commit()
            return doc

        # 2. Create chunks from extracted text
        chunks = _split_ocr_text(extracted_text)
        await _save_chunks(db, doc.id, chunks)

        # 3. AI semantic split for large chunks
        split_count = await semantic_split_chunks(db, doc.id)
        if split_count:
            logger.info("Semantic split: %d sub-chunks for photo doc %d", split_count, doc.id)

        # 4. Generate embeddings
        await embed_chunks(db, doc.id)

        # 5. Extract knowledge entities
        from app.services.knowledge_extraction import extract_entities_from_document
        await extract_entities_from_document(db, doc.id)

        doc.processing_status = "done"
    except Exception:
        logger.exception("Failed to process photo %d", doc.id)
        doc.processing_status = "error"

    await db.commit()
    return doc


def _split_ocr_text(text: str) -> list[ChunkResult]:
    """Split OCR text into chunks by double-newline paragraphs."""
    paragraphs = [p.strip() for p in text.split("\n\n") if p.strip()]

    if not paragraphs:
        return [ChunkResult(content=text.strip(), chunk_index=0, chunk_type="ocr_text")]

    # Merge small paragraphs to avoid tiny chunks
    merged: list[str] = []
    current = ""
    for para in paragraphs:
        if len(current) + len(para) < 800:
            current = f"{current}\n\n{para}" if current else para
        else:
            if current:
                merged.append(current)
            current = para
    if current:
        merged.append(current)

    return [
        ChunkResult(content=chunk, chunk_index=i, chunk_type="ocr_text")
        for i, chunk in enumerate(merged)
    ]

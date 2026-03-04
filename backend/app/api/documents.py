from fastapi import APIRouter, Depends, UploadFile
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.schemas.common import ApiResponse
from app.schemas.documents import ChunkOut, DocumentDetail, DocumentOut
from app.services import document_service

router = APIRouter(prefix="/api/documents", tags=["documents"])

ALLOWED_TYPES = {"pdf", "pptx", "docx", "html", "htm", "txt"}


@router.post("/upload", response_model=ApiResponse[DocumentOut])
async def upload_document(
    file: UploadFile,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[DocumentOut]:
    """Upload a document and start async processing."""
    ext = (file.filename or "").rsplit(".", 1)[-1].lower()
    if ext not in ALLOWED_TYPES:
        return ApiResponse(
            success=False,
            message=f"Dateityp .{ext} nicht unterstützt",
            errors=[f"Erlaubte Typen: {', '.join(ALLOWED_TYPES)}"],
        )

    doc = await document_service.upload_document(db, file)
    await db.commit()

    # Process in same request for now (background tasks in later phase)
    doc = await document_service.process_document(db, doc.id)
    await db.refresh(doc)

    return ApiResponse(data=DocumentOut.model_validate(doc), message="Dokument hochgeladen")


@router.get("", response_model=ApiResponse[list[DocumentOut]])
async def list_documents(
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[DocumentOut]]:
    """List all documents."""
    docs = await document_service.get_documents(db)
    return ApiResponse(
        data=[DocumentOut.model_validate(d) for d in docs],
        message=f"{len(docs)} Dokumente gefunden",
    )


@router.get("/{doc_id}", response_model=ApiResponse[DocumentDetail])
async def get_document(
    doc_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[DocumentDetail]:
    """Get a document with its chunks."""
    doc = await document_service.get_document(db, doc_id)
    if not doc:
        return ApiResponse(success=False, message="Dokument nicht gefunden")

    detail = DocumentDetail.model_validate(doc)
    detail.chunk_count = len(doc.chunks)
    return ApiResponse(data=detail)


@router.delete("/{doc_id}", response_model=ApiResponse[None])
async def delete_document(
    doc_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[None]:
    """Delete a document and its chunks."""
    deleted = await document_service.delete_document(db, doc_id)
    if not deleted:
        return ApiResponse(success=False, message="Dokument nicht gefunden")
    return ApiResponse(message="Dokument gelöscht")


@router.get("/{doc_id}/chunks", response_model=ApiResponse[list[ChunkOut]])
async def get_chunks(
    doc_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[ChunkOut]]:
    """Get all chunks for a document."""
    chunks = await document_service.get_chunks(db, doc_id)
    return ApiResponse(
        data=[ChunkOut.model_validate(c) for c in chunks],
        message=f"{len(chunks)} Chunks",
    )


@router.post("/{doc_id}/reprocess", response_model=ApiResponse[DocumentOut])
async def reprocess_document(
    doc_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[DocumentOut]:
    """Reprocess a document (delete chunks and re-chunk)."""
    doc = await document_service.get_document(db, doc_id)
    if not doc:
        return ApiResponse(success=False, message="Dokument nicht gefunden")

    # Delete existing chunks
    for chunk in doc.chunks:
        await db.delete(chunk)
    await db.flush()

    # Reprocess
    doc = await document_service.process_document(db, doc.id)
    await db.refresh(doc)
    return ApiResponse(data=DocumentOut.model_validate(doc), message="Neu verarbeitet")

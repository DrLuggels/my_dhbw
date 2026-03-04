from fastapi import APIRouter, Depends, UploadFile
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.schemas.common import ApiResponse
from app.schemas.documents import DocumentOut
from app.services.photo_service import process_photo

router = APIRouter(prefix="/api/photos", tags=["photos"])

ALLOWED_TYPES = {"jpg", "jpeg", "png", "heic"}


@router.post("/upload", response_model=ApiResponse[DocumentOut])
async def upload_photo(
    file: UploadFile,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[DocumentOut]:
    """Upload a photo for OCR processing via Gemini Vision."""
    ext = (file.filename or "").rsplit(".", 1)[-1].lower()
    if ext not in ALLOWED_TYPES:
        return ApiResponse(
            success=False,
            message=f"Dateityp .{ext} nicht unterstützt",
            errors=[f"Erlaubte Typen: {', '.join(ALLOWED_TYPES)}"],
        )

    content = await file.read()
    if not content:
        return ApiResponse(success=False, message="Leere Datei")

    doc = await process_photo(db, file.filename or "photo.jpg", content)
    return ApiResponse(data=DocumentOut.model_validate(doc), message="Foto verarbeitet")

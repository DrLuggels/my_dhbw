from fastapi import APIRouter, Depends
from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.schemas.common import ApiResponse

router = APIRouter(tags=["health"])


@router.get("/health", response_model=ApiResponse[dict])
async def health_check(db: AsyncSession = Depends(get_db)) -> ApiResponse[dict]:
    """Check application and database health."""
    try:
        await db.execute(text("SELECT 1"))
        db_status = "connected"
    except Exception:
        db_status = "disconnected"

    return ApiResponse(
        data={"status": "ok", "database": db_status},
        message="Service is running",
    )

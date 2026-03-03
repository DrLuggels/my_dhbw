"""Email API endpoints."""

import logging

from fastapi import APIRouter, Depends, Query
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.models.settings import AppSettings
from app.schemas.common import ApiResponse
from app.schemas.email import EmailDetail, EmailInboxResponse, EmailTestResult
from app.services.email_service import email_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/email", tags=["email"])


async def _get_email_settings(db: AsyncSession) -> AppSettings:
    """Load email settings from DB."""
    result = await db.execute(select(AppSettings).where(AppSettings.id == 1))
    row = result.scalar_one_or_none()
    if not row:
        raise ValueError("Settings not found")
    return row


def _validate_email_config(s: AppSettings) -> None:
    """Check all required email fields are set."""
    if not all([s.email_server, s.email_username, s.email_password, s.email_address]):
        raise ValueError(
            "E-Mail nicht konfiguriert. Bitte Server, Username, Passwort und Adresse eintragen."
        )


@router.post("/test", response_model=ApiResponse[EmailTestResult])
async def test_email(db: AsyncSession = Depends(get_db)):
    """Test the EWS email connection."""
    try:
        s = await _get_email_settings(db)
        _validate_email_config(s)
        result = await email_service.test_connection(
            server=s.email_server,
            username=s.email_username,
            password=s.email_password,
            email=s.email_address,
        )
        return ApiResponse(data=result, message="Verbindung erfolgreich")
    except ValueError as e:
        return ApiResponse(success=False, message=str(e))
    except Exception as e:
        logger.exception("Email connection test failed")
        return ApiResponse(success=False, message=f"Verbindung fehlgeschlagen: {e}")


@router.get("/inbox", response_model=ApiResponse[EmailInboxResponse])
async def get_inbox(
    limit: int = Query(30, ge=1, le=100),
    offset: int = Query(0, ge=0),
    db: AsyncSession = Depends(get_db),
):
    """Fetch inbox emails."""
    try:
        s = await _get_email_settings(db)
        _validate_email_config(s)
        result = await email_service.get_inbox(
            server=s.email_server,
            username=s.email_username,
            password=s.email_password,
            email=s.email_address,
            limit=limit,
            offset=offset,
        )
        return ApiResponse(data=result)
    except ValueError as e:
        return ApiResponse(success=False, message=str(e))
    except Exception as e:
        logger.exception("Failed to fetch inbox")
        return ApiResponse(success=False, message=f"Fehler beim Abrufen: {e}")


@router.get("/{item_id}", response_model=ApiResponse[EmailDetail])
async def get_email_detail(item_id: str, db: AsyncSession = Depends(get_db)):
    """Fetch a single email by its Exchange ID."""
    try:
        s = await _get_email_settings(db)
        _validate_email_config(s)
        result = await email_service.get_email(
            server=s.email_server,
            username=s.email_username,
            password=s.email_password,
            email=s.email_address,
            item_id=item_id,
        )
        return ApiResponse(data=result)
    except ValueError as e:
        return ApiResponse(success=False, message=str(e))
    except Exception as e:
        logger.exception("Failed to fetch email")
        return ApiResponse(success=False, message=f"Fehler beim Abrufen: {e}")

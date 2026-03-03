from datetime import datetime

from fastapi import APIRouter, Depends, Query
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.schemas.calendar import CalendarEventCreate, CalendarEventOut
from app.schemas.common import ApiResponse
from app.services import calendar_service

router = APIRouter(prefix="/api/calendar", tags=["calendar"])


@router.get("/events", response_model=ApiResponse[list[CalendarEventOut]])
async def list_events(
    start: datetime | None = Query(None),
    end: datetime | None = Query(None),
    source: str | None = Query(None),
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[CalendarEventOut]]:
    """List calendar events with optional filters."""
    events = await calendar_service.get_events(db, start=start, end=end, source=source)
    return ApiResponse(
        data=[CalendarEventOut.model_validate(e) for e in events],
        message=f"{len(events)} Events",
    )


@router.post("/events", response_model=ApiResponse[CalendarEventOut])
async def create_event(
    request: CalendarEventCreate,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[CalendarEventOut]:
    """Create a manual calendar event."""
    event = await calendar_service.create_event(
        db,
        title=request.title,
        description=request.description,
        start_time=request.start_time,
        end_time=request.end_time,
        all_day=request.all_day,
        event_type=request.event_type,
        source="manual",
        subject=request.subject,
        location=request.location,
    )
    await db.commit()
    return ApiResponse(data=CalendarEventOut.model_validate(event), message="Event erstellt")


@router.delete("/events/{event_id}", response_model=ApiResponse[None])
async def delete_event(
    event_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[None]:
    """Delete a calendar event."""
    deleted = await calendar_service.delete_event(db, event_id)
    if not deleted:
        return ApiResponse(success=False, message="Event nicht gefunden")
    return ApiResponse(message="Event gelöscht")


@router.post("/sync-rapla", response_model=ApiResponse[dict])
async def sync_rapla(
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[dict]:
    """Sync events from DHBW Rapla iCal feed."""
    count = await calendar_service.sync_rapla(db)
    await db.commit()
    return ApiResponse(
        data={"events_synced": count},
        message=f"{count} Rapla-Events synchronisiert",
    )

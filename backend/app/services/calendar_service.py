"""Calendar service: aggregates events from manual, Moodle, and Rapla sources."""

import logging
from datetime import datetime, timezone

import httpx
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.models.calendar import CalendarEvent

logger = logging.getLogger(__name__)


async def get_events(
    db: AsyncSession,
    start: datetime | None = None,
    end: datetime | None = None,
    source: str | None = None,
) -> list[CalendarEvent]:
    """Get calendar events with optional filters."""
    stmt = select(CalendarEvent).order_by(CalendarEvent.start_time)
    if start:
        stmt = stmt.where(CalendarEvent.start_time >= start)
    if end:
        stmt = stmt.where(CalendarEvent.start_time <= end)
    if source:
        stmt = stmt.where(CalendarEvent.source == source)
    result = await db.execute(stmt)
    return list(result.scalars().all())


async def create_event(db: AsyncSession, **kwargs) -> CalendarEvent:
    event = CalendarEvent(**kwargs)
    db.add(event)
    await db.flush()
    return event


async def delete_event(db: AsyncSession, event_id: int) -> bool:
    event = await db.get(CalendarEvent, event_id)
    if not event:
        return False
    await db.delete(event)
    await db.commit()
    return True


async def sync_rapla(db: AsyncSession) -> int:
    """Sync events from DHBW Rapla iCal feed.

    Returns:
        Number of events synced.
    """
    url = settings.rapla_base_url
    if not url:
        return 0

    try:
        async with httpx.AsyncClient(timeout=30.0) as client:
            response = await client.get(url)
            response.raise_for_status()
            ical_text = response.text
    except Exception:
        logger.exception("Failed to fetch Rapla calendar")
        return 0

    events = _parse_ical(ical_text)
    count = 0

    for ev in events:
        # Upsert by external_id
        result = await db.execute(
            select(CalendarEvent).where(
                CalendarEvent.source == "rapla",
                CalendarEvent.external_id == ev["uid"],
            )
        )
        existing = result.scalar_one_or_none()

        if existing:
            existing.title = ev["summary"]
            existing.start_time = ev["start"]
            existing.end_time = ev["end"]
            existing.location = ev.get("location")
            existing.description = ev.get("description")
        else:
            db.add(CalendarEvent(
                title=ev["summary"],
                start_time=ev["start"],
                end_time=ev["end"],
                location=ev.get("location"),
                description=ev.get("description"),
                event_type="lecture",
                source="rapla",
                external_id=ev["uid"],
            ))
        count += 1

    await db.flush()
    return count


def _parse_ical(text: str) -> list[dict]:
    """Minimal iCal parser for VEVENT blocks."""
    events: list[dict] = []
    current: dict | None = None

    for line in text.splitlines():
        line = line.strip()
        if line == "BEGIN:VEVENT":
            current = {}
        elif line == "END:VEVENT" and current is not None:
            if "summary" in current and "start" in current:
                events.append(current)
            current = None
        elif current is not None:
            if line.startswith("SUMMARY:"):
                current["summary"] = line[8:]
            elif line.startswith("LOCATION:"):
                current["location"] = line[9:]
            elif line.startswith("DESCRIPTION:"):
                current["description"] = line[12:]
            elif line.startswith("UID:"):
                current["uid"] = line[4:]
            elif line.startswith("DTSTART"):
                current["start"] = _parse_ical_dt(line)
            elif line.startswith("DTEND"):
                current["end"] = _parse_ical_dt(line)

    return events


def _parse_ical_dt(line: str) -> datetime:
    """Parse iCal datetime from a DTSTART/DTEND line."""
    value = line.split(":", 1)[-1].strip()
    for fmt in ("%Y%m%dT%H%M%SZ", "%Y%m%dT%H%M%S", "%Y%m%d"):
        try:
            dt = datetime.strptime(value, fmt)
            return dt.replace(tzinfo=timezone.utc)
        except ValueError:
            continue
    return datetime.now(timezone.utc)

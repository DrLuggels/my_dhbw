"""Calendar service: aggregates events from manual, Moodle, and Rapla sources."""

import logging
import re
from datetime import date, datetime, timedelta
from zoneinfo import ZoneInfo

import httpx
from bs4 import BeautifulSoup
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.models.calendar import CalendarEvent

logger = logging.getLogger(__name__)
BERLIN_TZ = ZoneInfo("Europe/Berlin")


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
    """Sync events by scraping the Rapla week-view HTML.

    Iterates week-by-week from 1 month ago to 4 months ahead,
    extracting events with room and professor info.
    """
    url = settings.rapla_calendar_url
    if not url:
        logger.warning("RAPLA_CALENDAR_URL not configured")
        return 0

    today = date.today()
    start_date = today - timedelta(days=30)
    end_date = today + timedelta(days=120)
    start_monday = start_date - timedelta(days=start_date.weekday())

    all_events: list[dict] = []
    seen_ids: set[str] = set()
    current = start_monday

    async with httpx.AsyncClient(timeout=30.0) as client:
        while current <= end_date:
            week_url = f"{url}&day={current.day}&month={current.month}&year={current.year}"
            try:
                resp = await client.get(week_url)
                resp.raise_for_status()
                for ev in _parse_rapla_html(resp.text):
                    eid = ev["external_id"]
                    if eid and eid not in seen_ids:
                        seen_ids.add(eid)
                        all_events.append(ev)
            except Exception:
                logger.warning("Failed to fetch Rapla week starting %s", current)
            current += timedelta(days=7)

    count = 0
    for ev in all_events:
        if not ev.get("start") or not ev.get("title"):
            continue

        result = await db.execute(
            select(CalendarEvent).where(
                CalendarEvent.source == "rapla",
                CalendarEvent.external_id == ev["external_id"],
            )
        )
        existing = result.scalar_one_or_none()

        if existing:
            existing.title = ev["title"]
            existing.start_time = ev["start"]
            existing.end_time = ev["end"]
            existing.location = ev["location"]
            existing.description = ev["description"]
            existing.event_type = ev["event_type"]
        else:
            db.add(CalendarEvent(
                title=ev["title"],
                start_time=ev["start"],
                end_time=ev["end"],
                location=ev["location"],
                description=ev["description"],
                event_type=ev["event_type"],
                source="rapla",
                external_id=ev["external_id"],
                all_day=False,
            ))
        count += 1

    await db.flush()
    return count


def _parse_rapla_html(html: str) -> list[dict]:
    """Parse events from a Rapla weekly calendar HTML page."""
    soup = BeautifulSoup(html, "lxml")
    events: list[dict] = []

    for cell in soup.select("td.week_block"):
        link = cell.find("a")
        if not link:
            continue
        tooltip = link.find("span", class_="tooltip")
        if not tooltip:
            continue

        # Key-value pairs from tooltip info table
        info: dict[str, str] = {}
        for row in tooltip.select("table.infotable tr"):
            label_td = row.find("td", class_="label")
            value_td = row.find("td", class_="value")
            if label_td and value_td:
                key = label_td.get_text(strip=True).rstrip(":")
                info[key] = value_td.get_text(strip=True)

        # Date/time from second <div>: "Mo 12.01.26 09:00-12:15"
        divs = tooltip.find_all("div")
        dt_text = divs[1].get_text(strip=True) if len(divs) > 1 else ""
        start_dt, end_dt = _parse_rapla_datetime(dt_text)

        # Event type from <strong>
        strong = tooltip.find("strong")
        type_text = strong.get_text(strip=True) if strong else ""
        event_type = "lecture" if type_text == "Lehrveranstaltung" else "other"

        title = info.get("Titel", "")
        location = _extract_room(info.get("Ressourcen", ""))
        professor = info.get("Personen") or None
        external_id = f"rapla_{start_dt.isoformat()}_{title}" if start_dt else None

        events.append({
            "title": title,
            "start": start_dt,
            "end": end_dt,
            "location": location,
            "description": professor,
            "event_type": event_type,
            "external_id": external_id,
        })

    return events


def _parse_rapla_datetime(text: str) -> tuple[datetime | None, datetime | None]:
    """Parse 'Mo 12.01.26 09:00-12:15' into timezone-aware datetimes."""
    m = re.match(
        r"[A-Za-z]{2}\s+(\d{2})\.(\d{2})\.(\d{2})\s+(\d{2}):(\d{2})-(\d{2}):(\d{2})",
        text,
    )
    if not m:
        return None, None
    day, month, yr = int(m.group(1)), int(m.group(2)), 2000 + int(m.group(3))
    start = datetime(yr, month, day, int(m.group(4)), int(m.group(5)), tzinfo=BERLIN_TZ)
    end = datetime(yr, month, day, int(m.group(6)), int(m.group(7)), tzinfo=BERLIN_TZ)
    return start, end


def _extract_room(resources: str) -> str | None:
    """Extract room from resources like 'RV-WDS125,MP124  Hörsaal'."""
    if not resources:
        return None
    parts = [p.strip() for p in resources.split(",")]
    rooms = [p for p in parts if not p.startswith("RV-")]
    return ", ".join(rooms) if rooms else None

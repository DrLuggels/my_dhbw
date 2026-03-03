from datetime import datetime

from pydantic import BaseModel


class CalendarEventOut(BaseModel):
    model_config = {"from_attributes": True}

    id: int
    title: str
    description: str | None = None
    start_time: datetime
    end_time: datetime | None = None
    all_day: bool
    event_type: str
    source: str
    external_id: str | None = None
    subject: str | None = None
    location: str | None = None
    created_at: datetime


class CalendarEventCreate(BaseModel):
    title: str
    description: str | None = None
    start_time: datetime
    end_time: datetime | None = None
    all_day: bool = False
    event_type: str = "other"
    subject: str | None = None
    location: str | None = None

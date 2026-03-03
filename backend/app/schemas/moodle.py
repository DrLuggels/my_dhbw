from datetime import datetime

from pydantic import BaseModel


class MoodleConnectRequest(BaseModel):
    token: str
    base_url: str = "https://moodle.dhbw-ravensburg.de"


class MoodleCourseOut(BaseModel):
    model_config = {"from_attributes": True}

    id: int
    moodle_id: int
    shortname: str
    fullname: str
    summary: str | None = None
    start_date: datetime | None = None
    end_date: datetime | None = None
    last_synced: datetime | None = None


class MoodleAssignmentOut(BaseModel):
    model_config = {"from_attributes": True}

    id: int
    course_id: int
    moodle_id: int
    name: str
    description: str | None = None
    due_date: datetime | None = None
    status: str


class MoodleResourceOut(BaseModel):
    model_config = {"from_attributes": True}

    id: int
    course_id: int
    moodle_id: int
    name: str
    resource_type: str
    url: str | None = None
    file_size: int | None = None
    is_downloaded: bool
    document_id: int | None = None
    last_modified: datetime | None = None


class MoodleSyncResult(BaseModel):
    courses: int = 0
    assignments: int = 0
    resources: int = 0

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
    new_resources: int = 0
    changed_resources: int = 0


class MoodleSyncStatus(BaseModel):
    status: str = "idle"
    courses_synced: int = 0
    resources_synced: int = 0
    new_resources: int = 0
    changed_resources: int = 0
    total_to_process: int = 0
    downloaded: int = 0
    processed: int = 0
    failed: int = 0
    current_file: str = ""
    errors: list[str] = []
    started_at: str | None = None
    finished_at: str | None = None

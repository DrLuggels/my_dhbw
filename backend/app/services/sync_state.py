"""In-memory sync progress tracking for Moodle auto-sync."""

from dataclasses import dataclass, field
from datetime import datetime, timezone


@dataclass
class SyncProgress:
    status: str = "idle"  # idle|syncing_metadata|downloading|processing|done|error

    # Metadata phase
    courses_synced: int = 0
    resources_synced: int = 0
    new_resources: int = 0
    changed_resources: int = 0

    # Download + process phase
    total_to_process: int = 0
    downloaded: int = 0
    processed: int = 0
    failed: int = 0
    current_file: str = ""

    errors: list[str] = field(default_factory=list)
    started_at: datetime | None = None
    finished_at: datetime | None = None

    def reset(self) -> None:
        self.status = "syncing_metadata"
        self.courses_synced = 0
        self.resources_synced = 0
        self.new_resources = 0
        self.changed_resources = 0
        self.total_to_process = 0
        self.downloaded = 0
        self.processed = 0
        self.failed = 0
        self.current_file = ""
        self.errors = []
        self.started_at = datetime.now(timezone.utc)
        self.finished_at = None

    def to_dict(self) -> dict:
        return {
            "status": self.status,
            "courses_synced": self.courses_synced,
            "resources_synced": self.resources_synced,
            "new_resources": self.new_resources,
            "changed_resources": self.changed_resources,
            "total_to_process": self.total_to_process,
            "downloaded": self.downloaded,
            "processed": self.processed,
            "failed": self.failed,
            "current_file": self.current_file,
            "errors": self.errors,
            "started_at": self.started_at.isoformat() if self.started_at else None,
            "finished_at": self.finished_at.isoformat() if self.finished_at else None,
        }


sync_progress = SyncProgress()

"""Moodle auto-sync orchestrator.

Runs metadata sync → delta detection → download → process as a background task.
"""

import logging
from datetime import datetime, timezone
from pathlib import Path

from sqlalchemy import select

from app.config import settings
from app.models.base import async_session
from app.models.document import Document
from app.models.moodle import MoodleResource
from app.services.document_service import process_document
from app.services.moodle_client import MoodleClient
from app.services.moodle_service import sync_all
from app.services.sync_state import sync_progress

logger = logging.getLogger(__name__)

SUPPORTED_EXTENSIONS = {"pdf", "pptx", "docx", "html", "htm", "txt"}


async def run_auto_sync() -> None:
    """Full auto-sync: metadata → download changed files → process pipeline."""
    sync_progress.reset()

    try:
        async with async_session() as db:
            # Phase 1: Metadata sync
            client = MoodleClient()
            try:
                result = await sync_all(db, client)
            finally:
                await client.close()

            sync_progress.courses_synced = result["courses"]
            sync_progress.resources_synced = result["resources"]
            sync_progress.new_resources = result.get("new_resources", 0)
            sync_progress.changed_resources = result.get("changed_resources", 0)

            # Phase 2: Find resources needing download + processing
            sync_progress.status = "downloading"
            pending = await db.execute(
                select(MoodleResource).where(
                    MoodleResource.is_downloaded == False,  # noqa: E712
                    MoodleResource.url.isnot(None),
                )
            )
            resources = [r for r in pending.scalars().all() if _is_supported(r)]
            sync_progress.total_to_process = len(resources)

            if not resources:
                sync_progress.status = "done"
                sync_progress.finished_at = datetime.now(timezone.utc)
                logger.info("Auto-sync done: no new files to process")
                return

            # Phase 3: Download + process each resource
            client = MoodleClient()
            try:
                for resource in resources:
                    await _download_and_process(db, client, resource)
            finally:
                await client.close()

        sync_progress.status = "done"
        sync_progress.finished_at = datetime.now(timezone.utc)
        logger.info(
            "Auto-sync done: %d downloaded, %d processed, %d failed",
            sync_progress.downloaded, sync_progress.processed, sync_progress.failed,
        )

    except Exception as e:
        sync_progress.status = "error"
        sync_progress.errors.append(str(e))
        sync_progress.finished_at = datetime.now(timezone.utc)
        logger.exception("Auto-sync failed")


async def _download_and_process(
    db, client: MoodleClient, resource: MoodleResource,
) -> None:
    sync_progress.current_file = resource.name
    try:
        # Download
        data = await client.download_file(resource.url)
        ext = _get_extension(resource.name, resource.url)
        filename = f"moodle_{resource.moodle_id}_{resource.name}"
        if not Path(filename).suffix:
            filename = f"{filename}.{ext}"
        filepath = settings.upload_path / filename
        filepath.write_bytes(data)

        resource.is_downloaded = True
        sync_progress.downloaded += 1

        # Create Document record
        sync_progress.status = "processing"
        doc = Document(
            title=Path(filename).stem,
            filename=filename,
            filepath=str(filepath),
            filetype=ext,
            filesize=len(data),
            processing_status="pending",
            metadata_json={"source": "moodle", "moodle_resource_id": resource.id},
        )
        db.add(doc)
        await db.flush()
        resource.document_id = doc.id

        # Process (chunk → embed → extract) — commits internally
        await process_document(db, doc.id)
        sync_progress.processed += 1

    except Exception as e:
        sync_progress.failed += 1
        error_msg = f"{resource.name}: {e}"
        sync_progress.errors.append(error_msg)
        logger.exception("Failed to process resource %d", resource.id)


def _is_supported(resource: MoodleResource) -> bool:
    ext = _get_extension(resource.name, resource.url or "")
    return ext in SUPPORTED_EXTENSIONS


def _get_extension(name: str, url: str) -> str:
    ext = Path(name).suffix.lstrip(".").lower()
    if not ext and url:
        ext = Path(url.split("?")[0]).suffix.lstrip(".").lower()
    return ext

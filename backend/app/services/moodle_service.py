"""Moodle synchronization service.

Syncs courses, assignments, resources from Moodle into local DB.
Auto-downloads resources into the document processing pipeline.
"""

import logging
from datetime import datetime, timezone
from pathlib import Path

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.models.moodle import MoodleAssignment, MoodleCourse, MoodleResource
from app.services.moodle_client import MoodleClient

logger = logging.getLogger(__name__)

DOWNLOADABLE_TYPES = {"resource", "file", "folder"}


async def sync_all(db: AsyncSession, client: MoodleClient | None = None) -> dict:
    """Full sync: courses → assignments → resources.

    Returns:
        Summary dict with counts of synced items.
    """
    cl = client or MoodleClient()
    try:
        courses_count = await _sync_courses(db, cl)
        assignments_count = await _sync_assignments(db, cl)
        resources_count = await _sync_resources(db, cl)

        await db.commit()
        return {
            "courses": courses_count,
            "assignments": assignments_count,
            "resources": resources_count,
        }
    finally:
        if not client:
            await cl.close()


async def _sync_courses(db: AsyncSession, cl: MoodleClient) -> int:
    raw_courses = await cl.get_courses()
    count = 0

    for raw in raw_courses:
        moodle_id = raw["id"]
        result = await db.execute(
            select(MoodleCourse).where(MoodleCourse.moodle_id == moodle_id)
        )
        course = result.scalar_one_or_none()

        if course:
            course.fullname = raw.get("fullname", "")
            course.shortname = raw.get("shortname", "")
            course.summary = raw.get("summary", "")
        else:
            course = MoodleCourse(
                moodle_id=moodle_id,
                shortname=raw.get("shortname", ""),
                fullname=raw.get("fullname", ""),
                summary=raw.get("summary", ""),
                start_date=_ts(raw.get("startdate")),
                end_date=_ts(raw.get("enddate")),
            )
            db.add(course)

        course.last_synced = datetime.now(timezone.utc)
        count += 1

    await db.flush()
    return count


async def _sync_assignments(db: AsyncSession, cl: MoodleClient) -> int:
    raw = await cl.get_assignments()
    count = 0

    for course_data in raw:
        course_mid = course_data.get("id")
        result = await db.execute(
            select(MoodleCourse).where(MoodleCourse.moodle_id == course_mid)
        )
        course = result.scalar_one_or_none()
        if not course:
            continue

        for a in course_data.get("assignments", []):
            mid = a["id"]
            existing = await db.execute(
                select(MoodleAssignment).where(
                    MoodleAssignment.course_id == course.id,
                    MoodleAssignment.moodle_id == mid,
                )
            )
            assignment = existing.scalar_one_or_none()

            if assignment:
                assignment.name = a.get("name", "")
                assignment.description = a.get("intro", "")
                assignment.due_date = _ts(a.get("duedate"))
            else:
                assignment = MoodleAssignment(
                    course_id=course.id,
                    moodle_id=mid,
                    name=a.get("name", ""),
                    description=a.get("intro", ""),
                    due_date=_ts(a.get("duedate")),
                )
                db.add(assignment)
            count += 1

    await db.flush()
    return count


async def _sync_resources(db: AsyncSession, cl: MoodleClient) -> int:
    result = await db.execute(select(MoodleCourse))
    courses = result.scalars().all()
    count = 0

    for course in courses:
        try:
            sections = await cl.get_course_contents(course.moodle_id)
        except Exception:
            logger.warning("Failed to get contents for course %d", course.moodle_id)
            continue

        for section in sections:
            for module in section.get("modules", []):
                modtype = module.get("modname", "")
                if modtype not in DOWNLOADABLE_TYPES:
                    continue

                for fileinfo in module.get("contents", []):
                    count += await _upsert_resource(
                        db, course.id, module, fileinfo,
                    )

    await db.flush()
    return count


async def _upsert_resource(
    db: AsyncSession, course_id: int, module: dict, fileinfo: dict,
) -> int:
    mid = module.get("id", 0)
    existing = await db.execute(
        select(MoodleResource).where(
            MoodleResource.course_id == course_id,
            MoodleResource.moodle_id == mid,
        )
    )
    resource = existing.scalar_one_or_none()

    if not resource:
        resource = MoodleResource(
            course_id=course_id,
            moodle_id=mid,
            name=module.get("name", ""),
            resource_type=fileinfo.get("type", "file"),
            url=fileinfo.get("fileurl"),
            file_size=fileinfo.get("filesize"),
            last_modified=_ts(fileinfo.get("timemodified")),
        )
        db.add(resource)
        return 1

    resource.name = module.get("name", "")
    resource.url = fileinfo.get("fileurl")
    return 0


async def download_resource(
    db: AsyncSession,
    resource_id: int,
    client: MoodleClient | None = None,
) -> Path | None:
    """Download a single resource file and save to uploads dir."""
    resource = await db.get(MoodleResource, resource_id)
    if not resource or not resource.url:
        return None

    cl = client or MoodleClient()
    try:
        data = await cl.download_file(resource.url)
        filename = f"moodle_{resource.moodle_id}_{resource.name}"
        filepath = settings.upload_path / filename
        filepath.write_bytes(data)

        resource.is_downloaded = True
        await db.flush()
        return filepath
    except Exception:
        logger.exception("Failed to download resource %d", resource_id)
        return None
    finally:
        if not client:
            await cl.close()


def _ts(value) -> datetime | None:
    if not value or value == 0:
        return None
    try:
        return datetime.fromtimestamp(int(value), tz=timezone.utc)
    except (ValueError, TypeError, OSError):
        return None

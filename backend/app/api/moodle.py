from fastapi import APIRouter, Depends
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.models.moodle import MoodleAssignment, MoodleCourse, MoodleResource
from app.schemas.common import ApiResponse
from app.schemas.moodle import (
    MoodleAssignmentOut,
    MoodleConnectRequest,
    MoodleCourseOut,
    MoodleResourceOut,
    MoodleSyncResult,
)
from app.services.moodle_client import MoodleClient
from app.services.moodle_service import download_resource, sync_all

router = APIRouter(prefix="/api/moodle", tags=["moodle"])


@router.post("/connect", response_model=ApiResponse[dict])
async def connect(request: MoodleConnectRequest) -> ApiResponse[dict]:
    """Test Moodle connection with token."""
    client = MoodleClient(base_url=request.base_url, token=request.token)
    try:
        info = await client.test_connection()
        return ApiResponse(
            data={
                "username": info.get("username"),
                "fullname": info.get("fullname"),
                "sitename": info.get("sitename"),
            },
            message="Verbindung erfolgreich",
        )
    except Exception as e:
        return ApiResponse(success=False, message=f"Verbindung fehlgeschlagen: {e}")
    finally:
        await client.close()


@router.post("/sync", response_model=ApiResponse[MoodleSyncResult])
async def sync(db: AsyncSession = Depends(get_db)) -> ApiResponse[MoodleSyncResult]:
    """Trigger a full Moodle sync."""
    try:
        result = await sync_all(db)
        return ApiResponse(
            data=MoodleSyncResult(**result),
            message="Sync abgeschlossen",
        )
    except Exception as e:
        return ApiResponse(success=False, message=f"Sync fehlgeschlagen: {e}")


@router.get("/courses", response_model=ApiResponse[list[MoodleCourseOut]])
async def list_courses(
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[MoodleCourseOut]]:
    """List all synced Moodle courses."""
    result = await db.execute(select(MoodleCourse).order_by(MoodleCourse.fullname))
    courses = result.scalars().all()
    return ApiResponse(data=[MoodleCourseOut.model_validate(c) for c in courses])


@router.get("/assignments", response_model=ApiResponse[list[MoodleAssignmentOut]])
async def list_assignments(
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[MoodleAssignmentOut]]:
    """List all synced assignments."""
    result = await db.execute(
        select(MoodleAssignment).order_by(MoodleAssignment.due_date.desc())
    )
    assignments = result.scalars().all()
    return ApiResponse(data=[MoodleAssignmentOut.model_validate(a) for a in assignments])


@router.get("/courses/{course_id}/resources", response_model=ApiResponse[list[MoodleResourceOut]])
async def list_resources(
    course_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[MoodleResourceOut]]:
    """List resources for a course."""
    result = await db.execute(
        select(MoodleResource)
        .where(MoodleResource.course_id == course_id)
        .order_by(MoodleResource.name)
    )
    resources = result.scalars().all()
    return ApiResponse(data=[MoodleResourceOut.model_validate(r) for r in resources])


@router.post("/resources/{resource_id}/download", response_model=ApiResponse[dict])
async def download(
    resource_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[dict]:
    """Download a Moodle resource and feed into document pipeline."""
    filepath = await download_resource(db, resource_id)
    if not filepath:
        return ApiResponse(success=False, message="Download fehlgeschlagen")

    await db.commit()
    return ApiResponse(
        data={"filepath": str(filepath), "resource_id": resource_id},
        message="Ressource heruntergeladen",
    )

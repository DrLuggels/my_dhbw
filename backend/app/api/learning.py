from fastapi import APIRouter, Depends, Query
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.schemas.common import ApiResponse
from app.schemas.learning import (
    AnswerRequest,
    ExerciseOut,
    ExerciseRequest,
    PriorityOut,
    SessionRequest,
    StatsOut,
    StreakOut,
)
from app.services import learning_service
from app.services.exercise_generator import generate_exercise

router = APIRouter(prefix="/api/learning", tags=["learning"])

STREAK_RATE = 0.02
STREAK_CAP = 0.50


@router.get("/next", response_model=ApiResponse[ExerciseOut])
async def get_next(db: AsyncSession = Depends(get_db)) -> ApiResponse[ExerciseOut]:
    """Get the next recommended exercise."""
    exercise = await learning_service.get_next_exercise(db)
    if not exercise:
        return ApiResponse(success=False, message="Keine Übungen verfügbar")
    await db.commit()
    return ApiResponse(data=ExerciseOut.model_validate(exercise))


@router.post("/exercise", response_model=ApiResponse[ExerciseOut])
async def create_exercise(
    request: ExerciseRequest,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[ExerciseOut]:
    """Generate a new exercise for a specific entity."""
    exercise = await generate_exercise(
        db, request.entity_id, request.difficulty, request.bloom_level,
    )
    if not exercise:
        return ApiResponse(success=False, message="Übung konnte nicht generiert werden")
    await db.commit()
    return ApiResponse(data=ExerciseOut.model_validate(exercise), message="Übung generiert")


@router.post("/exercise/{exercise_id}/answer", response_model=ApiResponse[ExerciseOut])
async def answer_exercise(
    exercise_id: int,
    request: AnswerRequest,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[ExerciseOut]:
    """Submit an answer for an exercise."""
    exercise = await learning_service.answer_exercise(
        db, exercise_id, request.user_answer, request.rating,
    )
    if not exercise:
        return ApiResponse(success=False, message="Übung nicht gefunden oder bereits beantwortet")
    await db.commit()

    out = ExerciseOut.model_validate(exercise)
    out.correct_answer = exercise.correct_answer
    out.explanation = exercise.explanation
    return ApiResponse(data=out, message="Richtig!" if exercise.is_correct else "Leider falsch")


@router.post("/session", response_model=ApiResponse[list[ExerciseOut]])
async def create_session(
    request: SessionRequest,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[ExerciseOut]]:
    """Generate a learning session with multiple exercises."""
    exercises = await learning_service.generate_session(db, request.count)
    await db.commit()
    return ApiResponse(
        data=[ExerciseOut.model_validate(e) for e in exercises],
        message=f"Session mit {len(exercises)} Übungen",
    )


@router.get("/stats", response_model=ApiResponse[StatsOut])
async def get_stats(db: AsyncSession = Depends(get_db)) -> ApiResponse[StatsOut]:
    """Get learning statistics."""
    stats = await learning_service.get_stats(db)
    return ApiResponse(data=StatsOut(**stats))


@router.get("/streak", response_model=ApiResponse[StreakOut])
async def get_streak(db: AsyncSession = Depends(get_db)) -> ApiResponse[StreakOut]:
    """Get current learning streak."""
    streak = await learning_service.get_streak(db)
    out = StreakOut.model_validate(streak)
    out.multiplier = 1.0 + min(STREAK_CAP, streak.current_streak * STREAK_RATE)
    return ApiResponse(data=out)


@router.get("/priorities", response_model=ApiResponse[list[PriorityOut]])
async def get_priorities(
    limit: int = Query(20, ge=1, le=100),
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[PriorityOut]]:
    """Get learning priorities ordered by score."""
    priorities = await learning_service.get_priorities(db, limit)
    return ApiResponse(data=[PriorityOut.model_validate(p) for p in priorities])


@router.post("/priorities/recalculate", response_model=ApiResponse[dict])
async def recalculate_priorities(
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[dict]:
    """Recalculate all learning priorities."""
    count = await learning_service.recalculate_priorities(db)
    await db.commit()
    return ApiResponse(data={"calculated": count}, message=f"{count} Prioritäten berechnet")


@router.get("/due", response_model=ApiResponse[list[ExerciseOut]])
async def get_due(db: AsyncSession = Depends(get_db)) -> ApiResponse[list[ExerciseOut]]:
    """Get exercises that are due for review."""
    exercises = await learning_service.get_due_exercises(db)
    return ApiResponse(
        data=[ExerciseOut.model_validate(e) for e in exercises],
        message=f"{len(exercises)} fällige Übungen",
    )

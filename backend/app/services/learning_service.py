"""Learning service: answers, sessions, stats, streaks.

Orchestrates FSRS, exercise generation, priority engine, and streak tracking.
"""

from datetime import date, datetime

from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.knowledge import Entity
from app.models.learning import Exercise, LearningPriority, LearningStreak
from app.services.exercise_generator import generate_exercise
from app.services.fsrs import FSRSState, process_review
from app.services.priority_engine import (
    calculate_priorities,
    check_bloom_advancement,
    select_difficulty,
)

STREAK_MULTIPLIER_RATE = 0.02
STREAK_MULTIPLIER_CAP = 0.50


async def answer_exercise(
    db: AsyncSession,
    exercise_id: int,
    user_answer: str,
    rating: int,
) -> Exercise | None:
    """Process a user's answer to an exercise.

    Updates FSRS state, mastery, bloom level, and streak.
    """
    exercise = await db.get(Exercise, exercise_id)
    if not exercise or exercise.is_answered:
        return None

    entity = await db.get(Entity, exercise.entity_id)
    if not entity:
        return None

    # Score the answer
    is_correct = _evaluate_answer(exercise, user_answer)
    exercise.is_answered = True
    exercise.is_correct = is_correct
    exercise.user_answer = user_answer
    exercise.score = 1.0 if is_correct else 0.0
    exercise.answered_at = datetime.utcnow()

    # FSRS update
    fsrs_state = FSRSState(
        state=entity.fsrs_state,
        stability=entity.fsrs_stability,
        difficulty=entity.fsrs_difficulty,
        last_review=entity.last_interaction,
    )
    result = process_review(fsrs_state, rating)

    entity.fsrs_state = result.new_state
    entity.fsrs_stability = result.stability
    entity.fsrs_difficulty = result.difficulty
    entity.next_review = result.next_review
    entity.last_interaction = datetime.utcnow()

    # Performance tracking
    entity.total_attempts += 1
    if is_correct:
        entity.correct_attempts += 1

    _update_difficulty_stats(entity, exercise.difficulty, is_correct)

    # Mastery update
    entity.mastery_score = entity.correct_attempts / max(entity.total_attempts, 1)

    # Bloom advancement check
    if check_bloom_advancement(entity):
        entity.bloom_level = min(6, entity.bloom_level + 1)

    # Streak
    await _update_streak(db)

    await db.flush()
    return exercise


async def get_next_exercise(db: AsyncSession) -> Exercise | None:
    """Get the next recommended exercise based on priority."""
    # Get highest priority entity
    result = await db.execute(
        select(LearningPriority)
        .where(LearningPriority.is_blocked.is_(False))
        .order_by(LearningPriority.composite_score.desc())
        .limit(1)
    )
    priority = result.scalar_one_or_none()
    if not priority:
        return None

    entity = await db.get(Entity, priority.entity_id)
    if not entity:
        return None

    difficulty = select_difficulty(entity)
    return await generate_exercise(db, entity.id, difficulty)


async def generate_session(db: AsyncSession, count: int = 10) -> list[Exercise]:
    """Generate a learning session with N exercises from top priorities."""
    result = await db.execute(
        select(LearningPriority)
        .where(LearningPriority.is_blocked.is_(False))
        .order_by(LearningPriority.composite_score.desc())
        .limit(count)
    )
    priorities = result.scalars().all()

    exercises: list[Exercise] = []
    for p in priorities:
        entity = await db.get(Entity, p.entity_id)
        if not entity:
            continue
        difficulty = select_difficulty(entity)
        ex = await generate_exercise(db, entity.id, difficulty)
        if ex:
            exercises.append(ex)

    return exercises


async def get_due_exercises(db: AsyncSession) -> list[Exercise]:
    """Get exercises that are due for review."""
    result = await db.execute(
        select(Exercise)
        .where(
            Exercise.is_answered.is_(True),
            Exercise.next_review <= datetime.utcnow(),
        )
        .order_by(Exercise.next_review)
        .limit(50)
    )
    return list(result.scalars().all())


async def get_stats(db: AsyncSession) -> dict:
    """Get learning statistics."""
    total_entities = await db.scalar(select(func.count(Entity.id)))
    mastered = await db.scalar(
        select(func.count(Entity.id)).where(Entity.mastery_score >= 0.7)
    )
    avg_mastery = await db.scalar(select(func.avg(Entity.mastery_score)))
    total_exercises = await db.scalar(select(func.count(Exercise.id)))
    answered = await db.scalar(
        select(func.count(Exercise.id)).where(Exercise.is_answered.is_(True))
    )
    correct = await db.scalar(
        select(func.count(Exercise.id)).where(Exercise.is_correct.is_(True))
    )

    return {
        "total_entities": total_entities or 0,
        "mastered_entities": mastered or 0,
        "average_mastery": round(float(avg_mastery or 0), 3),
        "total_exercises": total_exercises or 0,
        "answered_exercises": answered or 0,
        "correct_exercises": correct or 0,
        "accuracy": round(correct / max(answered, 1), 3) if answered else 0,
    }


async def get_streak(db: AsyncSession) -> LearningStreak:
    """Get or create the learning streak record."""
    result = await db.execute(select(LearningStreak).limit(1))
    streak = result.scalar_one_or_none()
    if not streak:
        streak = LearningStreak()
        db.add(streak)
        await db.flush()
    return streak


async def get_priorities(db: AsyncSession, limit: int = 20) -> list[LearningPriority]:
    result = await db.execute(
        select(LearningPriority)
        .order_by(LearningPriority.composite_score.desc())
        .limit(limit)
    )
    return list(result.scalars().all())


async def recalculate_priorities(db: AsyncSession) -> int:
    return await calculate_priorities(db)


def _evaluate_answer(exercise: Exercise, user_answer: str) -> bool:
    """Simple answer evaluation. Exact match for MC/fill-in, always true for free text."""
    if exercise.exercise_type == "free_text":
        return True  # Will be AI-graded in a future phase
    return user_answer.strip().lower() == exercise.correct_answer.strip().lower()


def _update_difficulty_stats(entity: Entity, difficulty: str, is_correct: bool) -> None:
    if difficulty == "easy":
        entity.easy_total += 1
        if is_correct:
            entity.easy_correct += 1
    elif difficulty == "medium":
        entity.medium_total += 1
        if is_correct:
            entity.medium_correct += 1
    else:
        entity.hard_total += 1
        if is_correct:
            entity.hard_correct += 1


async def _update_streak(db: AsyncSession) -> None:
    streak = await get_streak(db)
    today = date.today()

    if streak.last_activity_date == today:
        return  # Already counted today

    if streak.last_activity_date and (today - streak.last_activity_date).days == 1:
        streak.current_streak += 1
    elif streak.last_activity_date and (today - streak.last_activity_date).days > 1:
        streak.current_streak = 1
    else:
        streak.current_streak = 1

    streak.last_activity_date = today
    streak.total_active_days += 1
    streak.longest_streak = max(streak.longest_streak, streak.current_streak)

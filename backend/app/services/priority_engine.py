"""Multi-factor priority calculation for learning queue.

Composite Score = 30% Deadline + 20% Relevance + 25% Mastery Gap + 15% Decay + 10% Bloom Gap
"""

from datetime import datetime

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.knowledge import Entity, Relationship
from app.models.learning import LearningPriority
from app.services.fsrs import DECAY_RATE_DEFAULT, effective_mastery

# Weight constants
W_DEADLINE = 0.30
W_RELEVANCE = 0.20
W_MASTERY = 0.25
W_DECAY = 0.15
W_BLOOM = 0.10

TARGET_BLOOM = 6
PREREQUISITE_PENALTY = 0.5


async def calculate_priorities(db: AsyncSession) -> int:
    """Recalculate learning priorities for all entities.

    Returns:
        Number of priorities calculated.
    """
    result = await db.execute(select(Entity))
    entities = list(result.scalars().all())

    # Clear old priorities
    await db.execute(
        LearningPriority.__table__.delete()  # type: ignore[attr-defined]
    )

    now = datetime.utcnow()
    count = 0

    for entity in entities:
        days_since = _days_since(entity.last_interaction, now)
        mastery = effective_mastery(entity.mastery_score, days_since, entity.decay_rate)

        deadline_urgency = 0.0  # Will be enriched when calendar integration exists
        topic_relevance = entity.importance * 100
        mastery_gap = (1.0 - mastery) * 100
        decay_amount = (1.0 - mastery / max(entity.mastery_score, 0.01)) * 100 if entity.mastery_score > 0 else 0.0
        bloom_gap = (TARGET_BLOOM - entity.bloom_level) * 20

        composite = (
            W_DEADLINE * deadline_urgency
            + W_RELEVANCE * topic_relevance
            + W_MASTERY * mastery_gap
            + W_DECAY * decay_amount
            + W_BLOOM * bloom_gap
        )

        # Check prerequisite blocking
        is_blocked, block_reason = await _check_prerequisites(db, entity.id)
        if is_blocked:
            composite *= PREREQUISITE_PENALTY

        priority = LearningPriority(
            entity_id=entity.id,
            composite_score=composite,
            deadline_urgency=deadline_urgency,
            topic_relevance=topic_relevance,
            mastery_gap=mastery_gap,
            decay_amount=decay_amount,
            bloom_gap=bloom_gap,
            is_blocked=is_blocked,
            block_reason=block_reason,
        )
        db.add(priority)
        count += 1

    await db.flush()
    return count


async def _check_prerequisites(db: AsyncSession, entity_id: int) -> tuple[bool, str | None]:
    """Check if entity has unmet hard prerequisites."""
    result = await db.execute(
        select(Relationship)
        .where(
            Relationship.target_entity_id == entity_id,
            Relationship.is_prerequisite.is_(True),
            Relationship.prerequisite_strictness == "hard",
        )
    )
    prerequisites = result.scalars().all()

    for rel in prerequisites:
        source = await db.get(Entity, rel.source_entity_id)
        if source and source.mastery_score < 0.5:
            return True, f"Voraussetzung nicht erfüllt: {source.name} ({source.mastery_score:.0%})"

    return False, None


def select_difficulty(entity: Entity) -> str:
    """Select exercise difficulty using 20/40/40 distribution (Vygotsky ZPD).

    Returns:
        "easy", "medium", or "hard".
    """
    total = entity.total_attempts
    if total < 5:
        return "easy"

    easy_ratio = entity.easy_total / total if total > 0 else 0
    medium_ratio = entity.medium_total / total if total > 0 else 0

    if easy_ratio < 0.20:
        return "easy"
    if medium_ratio < 0.40:
        return "medium"
    return "hard"


def check_bloom_advancement(entity: Entity) -> bool:
    """Check if entity qualifies for Bloom level advancement.

    Rule: ≥3 attempts AND ≥70% success rate on current level.
    """
    if entity.bloom_level >= 6:
        return False
    if entity.total_attempts < 3:
        return False

    success_rate = entity.correct_attempts / entity.total_attempts
    return success_rate >= 0.70


def _days_since(last: datetime | None, now: datetime) -> int:
    if not last:
        return 30  # Default high value for never-interacted entities
    return max(0, (now - last).days)

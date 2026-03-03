from datetime import date, datetime

from sqlalchemy import (
    Boolean,
    Date,
    DateTime,
    Float,
    ForeignKey,
    Integer,
    String,
    Text,
    func,
)
from sqlalchemy.dialects.postgresql import JSONB
from sqlalchemy.orm import Mapped, mapped_column

from app.models.base import Base

EXERCISE_TYPES = ("multiple_choice", "fill_in_blank", "free_text")
DIFFICULTIES = ("easy", "medium", "hard")


class Exercise(Base):
    __tablename__ = "exercises"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    entity_id: Mapped[int] = mapped_column(ForeignKey("entities.id", ondelete="CASCADE"))
    question: Mapped[str] = mapped_column(Text)
    correct_answer: Mapped[str] = mapped_column(Text)
    explanation: Mapped[str | None] = mapped_column(Text, nullable=True)
    exercise_type: Mapped[str] = mapped_column(String(50))
    difficulty: Mapped[str] = mapped_column(String(20), default="medium")
    bloom_level: Mapped[int] = mapped_column(Integer, default=1)
    options_json: Mapped[dict | None] = mapped_column(JSONB, nullable=True)

    # Answer tracking
    is_answered: Mapped[bool] = mapped_column(Boolean, default=False)
    is_correct: Mapped[bool | None] = mapped_column(Boolean, nullable=True)
    user_answer: Mapped[str | None] = mapped_column(Text, nullable=True)
    score: Mapped[float | None] = mapped_column(Float, nullable=True)

    # Spaced repetition
    next_review: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    fsrs_state: Mapped[int] = mapped_column(Integer, default=0)

    # Source
    source_chunk_id: Mapped[int | None] = mapped_column(
        ForeignKey("chunks.id", ondelete="SET NULL"), nullable=True,
    )

    # Timestamps
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        server_default=func.now(),
    )
    answered_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)


class LearningStreak(Base):
    __tablename__ = "learning_streak"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    current_streak: Mapped[int] = mapped_column(Integer, default=0)
    longest_streak: Mapped[int] = mapped_column(Integer, default=0)
    last_activity_date: Mapped[date | None] = mapped_column(Date, nullable=True)
    total_active_days: Mapped[int] = mapped_column(Integer, default=0)


class LearningPriority(Base):
    __tablename__ = "learning_priorities"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    entity_id: Mapped[int] = mapped_column(ForeignKey("entities.id", ondelete="CASCADE"))
    composite_score: Mapped[float] = mapped_column(Float, default=0.0)
    deadline_urgency: Mapped[float] = mapped_column(Float, default=0.0)
    topic_relevance: Mapped[float] = mapped_column(Float, default=0.0)
    mastery_gap: Mapped[float] = mapped_column(Float, default=0.0)
    decay_amount: Mapped[float] = mapped_column(Float, default=0.0)
    bloom_gap: Mapped[float] = mapped_column(Float, default=0.0)
    is_blocked: Mapped[bool] = mapped_column(Boolean, default=False)
    block_reason: Mapped[str | None] = mapped_column(String(500), nullable=True)
    calculated_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        server_default=func.now(),
    )

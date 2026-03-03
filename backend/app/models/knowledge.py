from datetime import datetime

from sqlalchemy import (
    Boolean,
    DateTime,
    Float,
    ForeignKey,
    Integer,
    String,
    Text,
    func,
)
from sqlalchemy.dialects.postgresql import JSONB
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.base import Base, TimestampMixin

ENTITY_TYPES = (
    "concept", "definition", "formula", "person", "date", "example",
    "theorem", "method", "term", "algorithm", "data_structure", "principle",
)

RELATIONSHIP_TYPES = (
    "is_a", "part_of", "relates_to", "requires", "contradicts",
    "example_of", "defines", "uses", "precedes", "derives_from",
    "extends", "implements", "similar_to",
)


class Entity(Base, TimestampMixin):
    __tablename__ = "entities"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    name: Mapped[str] = mapped_column(String(500))
    description: Mapped[str | None] = mapped_column(Text, nullable=True)
    entity_type: Mapped[str] = mapped_column(String(50))
    subject: Mapped[str | None] = mapped_column(String(200), nullable=True)
    topic: Mapped[str | None] = mapped_column(String(200), nullable=True)
    subtopic: Mapped[str | None] = mapped_column(String(200), nullable=True)
    importance: Mapped[float] = mapped_column(Float, default=0.5)
    confidence: Mapped[float] = mapped_column(Float, default=0.8)

    # Source references
    source_document_id: Mapped[int | None] = mapped_column(
        ForeignKey("documents.id", ondelete="SET NULL"), nullable=True,
    )
    source_chunk_id: Mapped[int | None] = mapped_column(
        ForeignKey("chunks.id", ondelete="SET NULL"), nullable=True,
    )

    # Learning state
    mastery_score: Mapped[float] = mapped_column(Float, default=0.0)
    bloom_level: Mapped[int] = mapped_column(Integer, default=1)
    next_review: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    fsrs_state: Mapped[int] = mapped_column(Integer, default=0)
    fsrs_stability: Mapped[float] = mapped_column(Float, default=0.0)
    fsrs_difficulty: Mapped[float] = mapped_column(Float, default=0.0)

    # Performance tracking
    total_attempts: Mapped[int] = mapped_column(Integer, default=0)
    correct_attempts: Mapped[int] = mapped_column(Integer, default=0)
    easy_total: Mapped[int] = mapped_column(Integer, default=0)
    easy_correct: Mapped[int] = mapped_column(Integer, default=0)
    medium_total: Mapped[int] = mapped_column(Integer, default=0)
    medium_correct: Mapped[int] = mapped_column(Integer, default=0)
    hard_total: Mapped[int] = mapped_column(Integer, default=0)
    hard_correct: Mapped[int] = mapped_column(Integer, default=0)

    # Decay
    last_interaction: Mapped[datetime | None] = mapped_column(
        DateTime(timezone=True), nullable=True,
    )
    decay_rate: Mapped[float] = mapped_column(Float, default=0.05)

    # Relationships
    outgoing_relationships: Mapped[list["Relationship"]] = relationship(
        foreign_keys="Relationship.source_entity_id",
        back_populates="source_entity",
        cascade="all, delete-orphan",
    )
    incoming_relationships: Mapped[list["Relationship"]] = relationship(
        foreign_keys="Relationship.target_entity_id",
        back_populates="target_entity",
        cascade="all, delete-orphan",
    )


class Relationship(Base):
    __tablename__ = "relationships"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    source_entity_id: Mapped[int] = mapped_column(
        ForeignKey("entities.id", ondelete="CASCADE"),
    )
    target_entity_id: Mapped[int] = mapped_column(
        ForeignKey("entities.id", ondelete="CASCADE"),
    )
    relationship_type: Mapped[str] = mapped_column(String(50))
    strength: Mapped[float] = mapped_column(Float, default=0.5)
    evidence: Mapped[str | None] = mapped_column(Text, nullable=True)
    confidence: Mapped[float] = mapped_column(Float, default=0.8)
    is_prerequisite: Mapped[bool] = mapped_column(Boolean, default=False)
    prerequisite_strictness: Mapped[str | None] = mapped_column(String(10), nullable=True)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        server_default=func.now(),
    )

    source_entity: Mapped["Entity"] = relationship(
        foreign_keys=[source_entity_id],
        back_populates="outgoing_relationships",
    )
    target_entity: Mapped["Entity"] = relationship(
        foreign_keys=[target_entity_id],
        back_populates="incoming_relationships",
    )

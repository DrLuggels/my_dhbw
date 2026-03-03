from datetime import datetime

from pgvector.sqlalchemy import Vector
from sqlalchemy import DateTime, ForeignKey, Integer, String, Text, func
from sqlalchemy.dialects.postgresql import JSONB
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.base import Base, TimestampMixin

DOC_CATEGORIES = ("slides_export", "textbook", "exercise_sheet", "paper", "scan", "unknown")
PROCESSING_STATUSES = ("pending", "processing", "done", "error")
CHUNK_TYPES = ("definition", "example", "exercise", "theory", "overview", "formula", "mixed")


class Document(Base, TimestampMixin):
    __tablename__ = "documents"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    title: Mapped[str] = mapped_column(String(500))
    filename: Mapped[str] = mapped_column(String(500))
    filepath: Mapped[str] = mapped_column(String(1000))
    filetype: Mapped[str] = mapped_column(String(50))
    filesize: Mapped[int] = mapped_column(Integer, default=0)
    doc_category: Mapped[str] = mapped_column(String(50), default="unknown")
    processing_status: Mapped[str] = mapped_column(String(50), default="pending")
    metadata_json: Mapped[dict | None] = mapped_column(JSONB, nullable=True)

    chunks: Mapped[list["Chunk"]] = relationship(
        back_populates="document",
        cascade="all, delete-orphan",
    )


class Chunk(Base):
    __tablename__ = "chunks"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    document_id: Mapped[int] = mapped_column(ForeignKey("documents.id", ondelete="CASCADE"))
    content: Mapped[str] = mapped_column(Text)
    chunk_index: Mapped[int] = mapped_column(Integer)
    chunk_type: Mapped[str] = mapped_column(String(50), default="mixed")
    topic_label: Mapped[str | None] = mapped_column(String(200), nullable=True)
    section_heading: Mapped[str | None] = mapped_column(String(500), nullable=True)
    page_number: Mapped[int | None] = mapped_column(Integer, nullable=True)
    metadata_json: Mapped[dict | None] = mapped_column(JSONB, nullable=True)
    embedding = mapped_column(Vector(1536), nullable=True)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True),
        server_default=func.now(),
    )

    document: Mapped["Document"] = relationship(back_populates="chunks")

from datetime import datetime

from sqlalchemy import (
    Boolean,
    DateTime,
    ForeignKey,
    Integer,
    String,
    Text,
    func,
)
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.models.base import Base


class MoodleCourse(Base):
    __tablename__ = "moodle_courses"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    moodle_id: Mapped[int] = mapped_column(Integer, unique=True)
    shortname: Mapped[str] = mapped_column(String(100))
    fullname: Mapped[str] = mapped_column(String(500))
    summary: Mapped[str | None] = mapped_column(Text, nullable=True)
    start_date: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    end_date: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    last_synced: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)

    assignments: Mapped[list["MoodleAssignment"]] = relationship(
        back_populates="course",
        cascade="all, delete-orphan",
    )
    resources: Mapped[list["MoodleResource"]] = relationship(
        back_populates="course",
        cascade="all, delete-orphan",
    )


class MoodleAssignment(Base):
    __tablename__ = "moodle_assignments"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    course_id: Mapped[int] = mapped_column(ForeignKey("moodle_courses.id", ondelete="CASCADE"))
    moodle_id: Mapped[int] = mapped_column(Integer)
    name: Mapped[str] = mapped_column(String(500))
    description: Mapped[str | None] = mapped_column(Text, nullable=True)
    due_date: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    status: Mapped[str] = mapped_column(String(50), default="open")

    course: Mapped["MoodleCourse"] = relationship(back_populates="assignments")


class MoodleResource(Base):
    __tablename__ = "moodle_resources"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    course_id: Mapped[int] = mapped_column(ForeignKey("moodle_courses.id", ondelete="CASCADE"))
    moodle_id: Mapped[int] = mapped_column(Integer)
    name: Mapped[str] = mapped_column(String(500))
    resource_type: Mapped[str] = mapped_column(String(50))
    url: Mapped[str | None] = mapped_column(String(1000), nullable=True)
    file_size: Mapped[int | None] = mapped_column(Integer, nullable=True)
    is_downloaded: Mapped[bool] = mapped_column(Boolean, default=False)
    document_id: Mapped[int | None] = mapped_column(
        ForeignKey("documents.id", ondelete="SET NULL"), nullable=True,
    )
    last_modified: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)

    course: Mapped["MoodleCourse"] = relationship(back_populates="resources")

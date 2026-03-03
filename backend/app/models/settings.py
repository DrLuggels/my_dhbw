"""App settings and token usage models."""

from datetime import datetime

from sqlalchemy import DateTime, Integer, String, Text, func
from sqlalchemy.orm import Mapped, mapped_column

from app.models.base import Base


class AppSettings(Base):
    """Single-row table holding all user-configurable settings."""

    __tablename__ = "app_settings"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, default=1)

    # API Provider: "direct" or "github"
    ai_provider: Mapped[str] = mapped_column(String(20), default="direct")
    github_token: Mapped[str] = mapped_column(Text, default="")

    # Direct API Keys
    openai_api_key: Mapped[str] = mapped_column(Text, default="")
    anthropic_api_key: Mapped[str] = mapped_column(Text, default="")
    gemini_api_key: Mapped[str] = mapped_column(Text, default="")

    # Model Selection
    openai_model: Mapped[str] = mapped_column(String(100), default="gpt-4.1-mini")
    anthropic_model: Mapped[str] = mapped_column(String(100), default="claude-sonnet-4-6")
    gemini_model: Mapped[str] = mapped_column(String(100), default="gemini-2.5-flash")
    embedding_model: Mapped[str] = mapped_column(String(100), default="text-embedding-3-small")
    embedding_dimensions: Mapped[int] = mapped_column(Integer, default=1536)

    # Moodle
    moodle_base_url: Mapped[str] = mapped_column(
        String(500), default="https://moodle.dhbw-ravensburg.de"
    )
    moodle_token: Mapped[str] = mapped_column(Text, default="")

    # Email
    email_address: Mapped[str] = mapped_column(String(200), default="")
    email_password: Mapped[str] = mapped_column(Text, default="")
    email_imap_server: Mapped[str] = mapped_column(String(200), default="")

    # Rapla
    rapla_calendar_url: Mapped[str] = mapped_column(Text, default="")

    updated_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), onupdate=func.now()
    )


class TokenUsage(Base):
    """Logs each AI API call for usage tracking."""

    __tablename__ = "token_usage"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    provider: Mapped[str] = mapped_column(String(30))  # openai, anthropic, gemini
    model: Mapped[str] = mapped_column(String(100))
    input_tokens: Mapped[int] = mapped_column(Integer, default=0)
    output_tokens: Mapped[int] = mapped_column(Integer, default=0)
    task_type: Mapped[str] = mapped_column(String(50))  # chat, embedding, extraction, vision
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now()
    )

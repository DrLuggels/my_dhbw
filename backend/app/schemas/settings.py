"""Pydantic schemas for settings API."""

from pydantic import BaseModel


class SettingsOut(BaseModel):
    """Current settings (API keys masked)."""

    ai_provider: str
    github_token_set: bool
    openai_key_set: bool
    anthropic_key_set: bool
    gemini_key_set: bool

    openai_model: str
    anthropic_model: str
    gemini_model: str
    embedding_model: str
    embedding_dimensions: int

    moodle_base_url: str
    moodle_token_set: bool

    email_address: str
    email_password_set: bool
    email_imap_server: str

    rapla_calendar_url: str


class SettingsUpdate(BaseModel):
    """Partial update of settings. Only provided fields are updated."""

    ai_provider: str | None = None
    github_token: str | None = None
    openai_api_key: str | None = None
    anthropic_api_key: str | None = None
    gemini_api_key: str | None = None

    openai_model: str | None = None
    anthropic_model: str | None = None
    gemini_model: str | None = None
    embedding_model: str | None = None
    embedding_dimensions: int | None = None

    moodle_base_url: str | None = None
    moodle_token: str | None = None

    email_address: str | None = None
    email_password: str | None = None
    email_imap_server: str | None = None

    rapla_calendar_url: str | None = None


class ModelOption(BaseModel):
    """A single model option."""

    id: str
    name: str
    description: str


class ModelCategory(BaseModel):
    """Models grouped by provider and task."""

    provider: str
    task: str
    models: list[ModelOption]


class UsageSummary(BaseModel):
    """Token usage summary."""

    provider: str
    model: str
    total_input: int
    total_output: int
    total_calls: int


class UsageResponse(BaseModel):
    """Full usage stats."""

    by_provider: list[UsageSummary]
    total_input: int
    total_output: int
    total_calls: int

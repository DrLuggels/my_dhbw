from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application configuration loaded from environment variables."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    # Database
    database_url: str = (
        "postgresql+asyncpg://dhbw_user:dhbw_secret@localhost:5432/dhbw_automation"
    )

    # AI Services
    openai_api_key: str = ""
    anthropic_api_key: str = ""
    gemini_api_key: str = ""

    # AI Models
    openai_model: str = "gpt-5-mini"
    anthropic_model: str = "claude-sonnet-4-5"
    gemini_model: str = "gemini-3-flash-preview"  # or gemini-3.1-pro-preview
    embedding_model: str = "text-embedding-3-small"
    embedding_dimensions: int = 1536

    # Moodle
    moodle_base_url: str = "https://moodle.dhbw-ravensburg.de"
    moodle_token: str = ""

    # Rapla
    rapla_base_url: str = "https://rapla-ravensburg.dhbw.de/rapla"
    rapla_calendar_url: str = ""

    # Server
    backend_port: int = 8000
    frontend_url: str = "http://localhost:5173"
    public_domain: str = ""
    upload_dir: str = "uploads"

    @property
    def upload_path(self) -> Path:
        path = Path(self.upload_dir)
        path.mkdir(parents=True, exist_ok=True)
        return path


settings = Settings()

"""Multi-model AI gateway with token usage tracking."""

import logging

import anthropic
import google.generativeai as genai
import openai
from sqlalchemy import select

from app.config import settings
from app.models.base import async_session
from app.models.settings import AppSettings, TokenUsage

logger = logging.getLogger(__name__)


class AIService:
    """Multi-model AI gateway for OpenAI, Anthropic, and Google Gemini."""

    def __init__(self) -> None:
        self._openai: openai.AsyncOpenAI | None = None
        self._anthropic: anthropic.AsyncAnthropic | None = None
        self._gemini_configured = False
        self._db_settings: AppSettings | None = None

    def invalidate(self) -> None:
        """Reset cached clients so they pick up new keys/settings."""
        self._openai = None
        self._anthropic = None
        self._gemini_configured = False
        self._db_settings = None

    async def _get_db_settings(self) -> AppSettings | None:
        """Load settings from DB (cached per service lifetime)."""
        if self._db_settings is not None:
            return self._db_settings
        try:
            async with async_session() as db:
                result = await db.execute(select(AppSettings).where(AppSettings.id == 1))
                self._db_settings = result.scalar_one_or_none()
        except Exception:
            logger.debug("Could not load DB settings, using env defaults")
        return self._db_settings

    async def _get_key(self, provider: str) -> str:
        """Get API key from DB settings, falling back to env."""
        s = await self._get_db_settings()
        if s:
            if provider == "openai" and s.openai_api_key:
                return s.openai_api_key
            if provider == "anthropic" and s.anthropic_api_key:
                return s.anthropic_api_key
            if provider == "gemini" and s.gemini_api_key:
                return s.gemini_api_key
        # Fallback to env
        return {
            "openai": settings.openai_api_key,
            "anthropic": settings.anthropic_api_key,
            "gemini": settings.gemini_api_key,
        }.get(provider, "")

    async def _get_model(self, key: str) -> str:
        """Get model name from DB settings, falling back to env."""
        s = await self._get_db_settings()
        if s:
            val = getattr(s, key, None)
            if val:
                return val
        return getattr(settings, key, "")

    async def _log_usage(
        self, provider: str, model: str, task_type: str, input_tokens: int, output_tokens: int
    ) -> None:
        try:
            async with async_session() as db:
                db.add(TokenUsage(
                    provider=provider,
                    model=model,
                    input_tokens=input_tokens,
                    output_tokens=output_tokens,
                    task_type=task_type,
                ))
                await db.commit()
        except Exception:
            logger.debug("Failed to log token usage", exc_info=True)

    async def _get_openai_client(self) -> openai.AsyncOpenAI:
        if self._openai is None:
            key = await self._get_key("openai")
            self._openai = openai.AsyncOpenAI(api_key=key)
        return self._openai

    async def _get_anthropic_client(self) -> anthropic.AsyncAnthropic:
        if self._anthropic is None:
            key = await self._get_key("anthropic")
            self._anthropic = anthropic.AsyncAnthropic(api_key=key)
        return self._anthropic

    async def _ensure_gemini(self) -> None:
        if not self._gemini_configured:
            key = await self._get_key("gemini")
            genai.configure(api_key=key)
            self._gemini_configured = True

    async def chat_openai(self, prompt: str, system: str = "") -> str:
        """Send a chat completion request to OpenAI."""
        client = await self._get_openai_client()
        model = await self._get_model("openai_model")

        messages = []
        if system:
            messages.append({"role": "system", "content": system})
        messages.append({"role": "user", "content": prompt})

        response = await client.chat.completions.create(model=model, messages=messages)

        usage = response.usage
        if usage:
            await self._log_usage(
                "openai", model, "chat", usage.prompt_tokens, usage.completion_tokens
            )

        return response.choices[0].message.content or ""

    async def chat_claude(self, prompt: str, system: str = "") -> str:
        """Send a message to Anthropic Claude."""
        client = await self._get_anthropic_client()
        model = await self._get_model("anthropic_model")

        response = await client.messages.create(
            model=model,
            max_tokens=4096,
            system=system or "Du bist ein hilfreicher Assistent für akademische Inhalte.",
            messages=[{"role": "user", "content": prompt}],
        )

        await self._log_usage(
            "anthropic", model, "chat",
            response.usage.input_tokens, response.usage.output_tokens,
        )

        return response.content[0].text

    async def vision_gemini(self, image_bytes: bytes, prompt: str) -> str:
        """Send an image to Google Gemini for vision/OCR analysis."""
        await self._ensure_gemini()
        model_name = await self._get_model("gemini_model")
        model = genai.GenerativeModel(model_name)

        response = await model.generate_content_async([
            prompt,
            {"mime_type": "image/png", "data": image_bytes},
        ])

        # Gemini usage tracking (approximate)
        input_est = len(prompt) // 4 + len(image_bytes) // 100
        output_est = len(response.text or "") // 4
        await self._log_usage("gemini", model_name, "vision", input_est, output_est)

        return response.text or ""

    async def generate_embeddings(self, texts: list[str]) -> list[list[float]]:
        """Generate embeddings for a batch of texts using OpenAI."""
        client = await self._get_openai_client()
        model = await self._get_model("embedding_model")
        s = await self._get_db_settings()
        dimensions = s.embedding_dimensions if s else settings.embedding_dimensions

        response = await client.embeddings.create(
            model=model, input=texts, dimensions=dimensions,
        )

        total_tokens = response.usage.total_tokens if response.usage else 0
        await self._log_usage("openai", model, "embedding", total_tokens, 0)

        return [item.embedding for item in response.data]


ai_service = AIService()

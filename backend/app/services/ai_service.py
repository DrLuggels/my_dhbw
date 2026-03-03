import logging

import anthropic
import google.generativeai as genai
import openai

from app.config import settings

logger = logging.getLogger(__name__)


class AIService:
    """Multi-model AI gateway for OpenAI, Anthropic, and Google Gemini."""

    def __init__(self) -> None:
        self._openai: openai.AsyncOpenAI | None = None
        self._anthropic: anthropic.AsyncAnthropic | None = None
        self._gemini_configured = False

    @property
    def openai_client(self) -> openai.AsyncOpenAI:
        if self._openai is None:
            self._openai = openai.AsyncOpenAI(api_key=settings.openai_api_key)
        return self._openai

    @property
    def anthropic_client(self) -> anthropic.AsyncAnthropic:
        if self._anthropic is None:
            self._anthropic = anthropic.AsyncAnthropic(api_key=settings.anthropic_api_key)
        return self._anthropic

    def _ensure_gemini(self) -> None:
        if not self._gemini_configured:
            genai.configure(api_key=settings.gemini_api_key)
            self._gemini_configured = True

    async def chat_openai(self, prompt: str, system: str = "") -> str:
        """Send a chat completion request to OpenAI."""
        messages = []
        if system:
            messages.append({"role": "system", "content": system})
        messages.append({"role": "user", "content": prompt})

        response = await self.openai_client.chat.completions.create(
            model=settings.openai_model,
            messages=messages,
        )
        return response.choices[0].message.content or ""

    async def chat_claude(self, prompt: str, system: str = "") -> str:
        """Send a message to Anthropic Claude."""
        response = await self.anthropic_client.messages.create(
            model=settings.anthropic_model,
            max_tokens=4096,
            system=system or "Du bist ein hilfreicher Assistent für akademische Inhalte.",
            messages=[{"role": "user", "content": prompt}],
        )
        return response.content[0].text

    async def vision_gemini(self, image_bytes: bytes, prompt: str) -> str:
        """Send an image to Google Gemini for vision/OCR analysis."""
        self._ensure_gemini()
        model = genai.GenerativeModel(settings.gemini_model)
        response = await model.generate_content_async([
            prompt,
            {"mime_type": "image/png", "data": image_bytes},
        ])
        return response.text or ""

    async def generate_embeddings(self, texts: list[str]) -> list[list[float]]:
        """Generate embeddings for a batch of texts using OpenAI."""
        response = await self.openai_client.embeddings.create(
            model=settings.embedding_model,
            input=texts,
            dimensions=settings.embedding_dimensions,
        )
        return [item.embedding for item in response.data]


ai_service = AIService()

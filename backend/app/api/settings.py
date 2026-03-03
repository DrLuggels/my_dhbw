"""Settings API endpoints."""

import logging

from fastapi import APIRouter, Depends
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.models.settings import AppSettings, TokenUsage
from app.schemas.common import ApiResponse
from app.schemas.settings import (
    ModelCategory,
    ModelOption,
    SettingsOut,
    SettingsUpdate,
    UsageResponse,
    UsageSummary,
)

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/settings", tags=["settings"])

AVAILABLE_MODELS: list[ModelCategory] = [
    ModelCategory(
        provider="openai",
        task="Chat / Allgemein",
        models=[
            ModelOption(id="gpt-5.2", name="GPT-5.2", description="Bestes Modell, Coding & Agenten"),
            ModelOption(id="gpt-5.1", name="GPT-5.1", description="Reasoning mit konfig. Aufwand"),
            ModelOption(id="gpt-5", name="GPT-5", description="Intelligent, breit einsetzbar"),
            ModelOption(id="gpt-5-mini", name="GPT-5 Mini", description="Schnell & kosteneffizient"),
            ModelOption(id="gpt-5-nano", name="GPT-5 Nano", description="Ultraschnell & günstig"),
            ModelOption(id="gpt-4.1", name="GPT-4.1", description="Bestes Non-Reasoning-Modell"),
            ModelOption(id="gpt-4.1-mini", name="GPT-4.1 Mini", description="Schnell & günstig"),
            ModelOption(id="gpt-4.1-nano", name="GPT-4.1 Nano", description="Minimale Kosten"),
            ModelOption(id="o4-mini", name="o4-mini", description="Schnelles Reasoning"),
            ModelOption(id="o3", name="o3", description="Komplexes Reasoning"),
            ModelOption(id="o3-mini", name="o3-mini", description="Reasoning, kompakt"),
        ],
    ),
    ModelCategory(
        provider="anthropic",
        task="Entity-Extraktion / Übungen",
        models=[
            ModelOption(
                id="claude-sonnet-4-6", name="Claude Sonnet 4.6", description="Beste Balance"
            ),
            ModelOption(
                id="claude-haiku-4-5", name="Claude Haiku 4.5", description="Schnell & günstig"
            ),
            ModelOption(
                id="claude-opus-4-6", name="Claude Opus 4.6", description="Höchste Qualität"
            ),
        ],
    ),
    ModelCategory(
        provider="gemini",
        task="Vision / OCR",
        models=[
            ModelOption(
                id="gemini-3.1-pro-preview",
                name="Gemini 3.1 Pro",
                description="Bestes Modell, Reasoning & Agenten",
            ),
            ModelOption(
                id="gemini-3-flash-preview",
                name="Gemini 3 Flash",
                description="Frontier-Leistung, günstig",
            ),
            ModelOption(
                id="gemini-3.1-flash-lite-preview",
                name="Gemini 3.1 Flash Lite",
                description="Ultraschnell, niedrigste Kosten",
            ),
            ModelOption(
                id="gemini-2.5-pro",
                name="Gemini 2.5 Pro",
                description="Deep Reasoning, stabil",
            ),
            ModelOption(
                id="gemini-2.5-flash",
                name="Gemini 2.5 Flash",
                description="Preis-Leistung, stabil",
            ),
        ],
    ),
    ModelCategory(
        provider="openai",
        task="Embeddings",
        models=[
            ModelOption(
                id="text-embedding-3-small",
                name="text-embedding-3-small",
                description="1536D, gute Balance",
            ),
            ModelOption(
                id="text-embedding-3-large",
                name="text-embedding-3-large",
                description="3072D, höchste Qualität",
            ),
        ],
    ),
]


async def _get_settings(db: AsyncSession) -> AppSettings:
    """Get or create the singleton settings row."""
    result = await db.execute(select(AppSettings).where(AppSettings.id == 1))
    row = result.scalar_one_or_none()
    if not row:
        row = AppSettings(id=1)
        db.add(row)
        await db.flush()
    return row


def _to_out(s: AppSettings) -> SettingsOut:
    return SettingsOut(
        ai_provider=s.ai_provider,
        github_token_set=bool(s.github_token),
        openai_key_set=bool(s.openai_api_key),
        anthropic_key_set=bool(s.anthropic_api_key),
        gemini_key_set=bool(s.gemini_api_key),
        openai_model=s.openai_model,
        anthropic_model=s.anthropic_model,
        gemini_model=s.gemini_model,
        embedding_model=s.embedding_model,
        embedding_dimensions=s.embedding_dimensions,
        moodle_base_url=s.moodle_base_url,
        moodle_token_set=bool(s.moodle_token),
        email_address=s.email_address,
        email_password_set=bool(s.email_password),
        email_imap_server=s.email_imap_server,
        rapla_calendar_url=s.rapla_calendar_url,
    )


@router.get("", response_model=ApiResponse[SettingsOut])
async def get_settings(db: AsyncSession = Depends(get_db)):
    s = await _get_settings(db)
    return ApiResponse(data=_to_out(s))


@router.put("", response_model=ApiResponse[SettingsOut])
async def update_settings(body: SettingsUpdate, db: AsyncSession = Depends(get_db)):
    s = await _get_settings(db)

    for field, value in body.model_dump(exclude_unset=True).items():
        setattr(s, field, value)

    await db.commit()
    await db.refresh(s)

    # Invalidate cached AI clients so they pick up new keys
    from app.services.ai_service import ai_service
    ai_service.invalidate()

    return ApiResponse(data=_to_out(s), message="Einstellungen gespeichert")


@router.get("/models", response_model=ApiResponse[list[ModelCategory]])
async def get_available_models():
    return ApiResponse(data=AVAILABLE_MODELS)


@router.get("/usage", response_model=ApiResponse[UsageResponse])
async def get_usage(db: AsyncSession = Depends(get_db)):
    stmt = (
        select(
            TokenUsage.provider,
            TokenUsage.model,
            func.sum(TokenUsage.input_tokens).label("total_input"),
            func.sum(TokenUsage.output_tokens).label("total_output"),
            func.count().label("total_calls"),
        )
        .group_by(TokenUsage.provider, TokenUsage.model)
        .order_by(func.count().desc())
    )
    result = await db.execute(stmt)
    rows = result.all()

    by_provider = [
        UsageSummary(
            provider=r.provider,
            model=r.model,
            total_input=r.total_input or 0,
            total_output=r.total_output or 0,
            total_calls=r.total_calls,
        )
        for r in rows
    ]

    return ApiResponse(
        data=UsageResponse(
            by_provider=by_provider,
            total_input=sum(s.total_input for s in by_provider),
            total_output=sum(s.total_output for s in by_provider),
            total_calls=sum(s.total_calls for s in by_provider),
        )
    )

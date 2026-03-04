"""AI-powered semantic splitting of chunks into focused sub-chunks."""

import json
import logging

from sqlalchemy import select, delete
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.document import Chunk
from app.services.ai_service import ai_service
from app.utils.text import count_tokens

logger = logging.getLogger(__name__)

MIN_TOKENS_FOR_SPLIT = 150

SPLIT_SYSTEM = """Du bist ein akademischer Text-Analyst. Deine Aufgabe ist es, akademische Texte
in eigenständige, thematisch fokussierte Lerneinheiten aufzuteilen.

Regeln:
- Nur aufteilen, wenn der Text MEHRERE klar verschiedene Konzepte/Themen behandelt
- Jeder Teil muss eigenständig verständlich sein (inkl. nötigem Kontext)
- Behalte die section_heading-Information als Präfix bei, falls vorhanden
- Antworte NUR mit einem JSON-Array von Strings
- Falls der Text bereits fokussiert ist, antworte mit einem Array mit nur dem Originaltext
- Mindestens 2 Sätze pro Teil"""


async def semantic_split_chunks(db: AsyncSession, document_id: int) -> int:
    """Split large multi-topic chunks into focused sub-chunks using AI.

    Returns:
        Number of new chunks created (0 if no splits occurred).
    """
    result = await db.execute(
        select(Chunk)
        .where(Chunk.document_id == document_id)
        .order_by(Chunk.chunk_index)
    )
    chunks = list(result.scalars().all())
    if not chunks:
        return 0

    new_count = 0
    for chunk in chunks:
        tokens = count_tokens(chunk.content)
        if tokens <= MIN_TOKENS_FOR_SPLIT:
            continue

        try:
            sub_texts = await _split_chunk_with_ai(chunk.content)
        except Exception:
            logger.exception("AI split failed for chunk %d", chunk.id)
            continue

        if len(sub_texts) <= 1:
            continue

        # Replace original chunk with sub-chunks
        original_index = chunk.chunk_index
        original_type = chunk.chunk_type
        original_heading = chunk.section_heading
        original_page = chunk.page_number
        original_meta = chunk.metadata_json or {}
        chunk_id = chunk.id

        await db.delete(chunk)
        await db.flush()

        for i, text in enumerate(sub_texts):
            sub_chunk = Chunk(
                document_id=document_id,
                content=text.strip(),
                chunk_index=original_index * 100 + i,
                chunk_type=original_type,
                section_heading=original_heading,
                page_number=original_page,
                metadata_json={**original_meta, "split_from": chunk_id, "split_part": i},
            )
            db.add(sub_chunk)

        new_count += len(sub_texts) - 1
        logger.info("Split chunk %d into %d sub-chunks", chunk_id, len(sub_texts))

    if new_count > 0:
        await _reindex_chunks(db, document_id)

    await db.flush()
    return new_count


async def _split_chunk_with_ai(content: str) -> list[str]:
    """Ask AI to split a chunk into focused sub-texts."""
    prompt = f"Analysiere und teile folgenden akademischen Text in thematisch fokussierte Lerneinheiten auf:\n\n{content}"
    raw = await ai_service.chat_openai(prompt, system=SPLIT_SYSTEM)

    texts = _parse_json_array(raw)
    if not texts:
        return [content]

    # Filter out empty or too-short parts
    return [t for t in texts if len(t.strip()) > 50]


def _parse_json_array(raw: str) -> list[str] | None:
    """Parse AI response as a JSON array of strings."""
    text = raw.strip()
    if text.startswith("```"):
        lines = text.split("\n")
        text = "\n".join(lines[1:-1] if lines[-1].strip() == "```" else lines[1:])

    try:
        result = json.loads(text)
        if isinstance(result, list) and all(isinstance(s, str) for s in result):
            return result
    except json.JSONDecodeError:
        logger.warning("Failed to parse semantic split JSON: %s...", text[:100])

    return None


async def _reindex_chunks(db: AsyncSession, document_id: int) -> None:
    """Re-assign sequential chunk_index values after splitting."""
    result = await db.execute(
        select(Chunk)
        .where(Chunk.document_id == document_id)
        .order_by(Chunk.chunk_index)
    )
    for i, chunk in enumerate(result.scalars().all()):
        chunk.chunk_index = i

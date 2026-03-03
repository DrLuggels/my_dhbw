"""Exercise generation via Claude AI.

Generates multiple choice, fill-in-blank, and free text exercises
from knowledge entities and their source chunks.
"""

import json
import logging

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.document import Chunk
from app.models.knowledge import Entity
from app.models.learning import Exercise
from app.services.ai_service import ai_service

logger = logging.getLogger(__name__)

# Bloom level → allowed exercise types
BLOOM_EXERCISE_MAP = {
    1: ["multiple_choice", "fill_in_blank"],
    2: ["multiple_choice", "fill_in_blank"],
    3: ["multiple_choice", "fill_in_blank", "free_text"],
    4: ["free_text"],
    5: ["free_text"],
    6: ["free_text"],
}

GENERATION_SYSTEM = """Du bist ein Übungsgenerator für Hochschulstudenten.
Erstelle eine Übung basierend auf dem gegebenen Konzept und Kontext.

Antworte NUR mit validem JSON:
{
  "question": "Die Frage",
  "correct_answer": "Die korrekte Antwort",
  "explanation": "Erklärung warum das korrekt ist",
  "options": ["Option A", "Option B", "Option C", "Option D"]
}

Bei Lückentext: Markiere die Lücke mit _____ in der Frage.
Bei Freitext: Keine options nötig, setze options auf null.
Bei Multiple Choice: Genau 4 Optionen, correct_answer muss eine davon sein."""


async def generate_exercise(
    db: AsyncSession,
    entity_id: int,
    difficulty: str = "medium",
    bloom_level: int | None = None,
) -> Exercise | None:
    """Generate an exercise for a knowledge entity.

    Args:
        db: Database session.
        entity_id: Target entity.
        difficulty: easy, medium, or hard.
        bloom_level: Override bloom level (default: entity's current level).

    Returns:
        Created exercise or None on failure.
    """
    entity = await db.get(Entity, entity_id)
    if not entity:
        return None

    bloom = bloom_level or entity.bloom_level
    exercise_type = _select_exercise_type(bloom)

    # Get source context
    context = ""
    if entity.source_chunk_id:
        chunk = await db.get(Chunk, entity.source_chunk_id)
        if chunk:
            context = chunk.content[:1500]

    prompt = _build_prompt(entity, exercise_type, difficulty, bloom, context)
    raw = await ai_service.chat_claude(prompt, system=GENERATION_SYSTEM)
    data = _parse_response(raw)
    if not data:
        return None

    exercise = Exercise(
        entity_id=entity.id,
        question=data["question"],
        correct_answer=data["correct_answer"],
        explanation=data.get("explanation"),
        exercise_type=exercise_type,
        difficulty=difficulty,
        bloom_level=bloom,
        options_json={"options": data["options"]} if data.get("options") else None,
        source_chunk_id=entity.source_chunk_id,
    )
    db.add(exercise)
    await db.flush()
    return exercise


def _select_exercise_type(bloom_level: int) -> str:
    allowed = BLOOM_EXERCISE_MAP.get(bloom_level, ["multiple_choice"])
    # Prefer variety: rotate based on bloom
    return allowed[bloom_level % len(allowed)]


def _build_prompt(
    entity: Entity, ex_type: str, difficulty: str, bloom: int, context: str,
) -> str:
    type_labels = {
        "multiple_choice": "Multiple Choice (4 Optionen)",
        "fill_in_blank": "Lückentext (eine Lücke mit _____)",
        "free_text": "Freitext (offene Frage)",
    }
    bloom_labels = {
        1: "Erinnern", 2: "Verstehen", 3: "Anwenden",
        4: "Analysieren", 5: "Bewerten", 6: "Erschaffen",
    }

    parts = [
        f"Konzept: {entity.name}",
        f"Beschreibung: {entity.description or 'Keine'}",
        f"Thema: {entity.topic or 'Allgemein'}",
        f"Übungstyp: {type_labels.get(ex_type, ex_type)}",
        f"Schwierigkeit: {difficulty}",
        f"Bloom-Level: {bloom} ({bloom_labels.get(bloom, '')})",
    ]
    if context:
        parts.append(f"\nKontext aus dem Lehrmaterial:\n{context}")

    return "\n".join(parts)


def _parse_response(raw: str) -> dict | None:
    text = raw.strip()
    if text.startswith("```"):
        lines = text.split("\n")
        text = "\n".join(lines[1:-1] if lines[-1].strip() == "```" else lines[1:])
    try:
        data = json.loads(text)
        if "question" not in data or "correct_answer" not in data:
            return None
        return data
    except json.JSONDecodeError:
        logger.warning("Failed to parse exercise JSON")
        return None

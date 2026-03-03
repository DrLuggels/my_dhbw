import json
import logging

from sqlalchemy import select, text
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.document import Chunk
from app.models.knowledge import Entity, Relationship
from app.services.ai_service import ai_service

logger = logging.getLogger(__name__)

EXTRACTION_SYSTEM = """Du bist ein akademischer Wissensextraktor. Analysiere den gegebenen Text
und extrahiere strukturierte Wissensentitäten und ihre Beziehungen.

Antworte NUR mit validem JSON in diesem Format:
{
  "entities": [
    {
      "name": "Name der Entität",
      "description": "Kurze Beschreibung",
      "entity_type": "concept|definition|formula|person|date|example|theorem|method|term|algorithm|data_structure|principle",
      "importance": 0.0-1.0,
      "topic": "Themenbereich"
    }
  ],
  "relationships": [
    {
      "source": "Name Entity A",
      "target": "Name Entity B",
      "type": "is_a|part_of|relates_to|requires|contradicts|example_of|defines|uses|precedes|derives_from|extends|implements|similar_to",
      "strength": 0.0-1.0,
      "evidence": "Kurze Begründung",
      "is_prerequisite": false,
      "strictness": "soft|hard"
    }
  ]
}"""

AUTO_LINK_THRESHOLD = 0.8


async def extract_entities_from_document(db: AsyncSession, document_id: int) -> int:
    """Extract knowledge entities from all chunks of a document.

    Returns:
        Number of entities created.
    """
    result = await db.execute(
        select(Chunk)
        .where(Chunk.document_id == document_id)
        .order_by(Chunk.chunk_index)
    )
    chunks = list(result.scalars().all())
    if not chunks:
        return 0

    total_entities = 0
    for chunk in chunks:
        try:
            total_entities += await _extract_from_chunk(db, chunk)
        except Exception:
            logger.exception("Entity extraction failed for chunk %d", chunk.id)

    await auto_link_entities(db, document_id)
    await db.flush()
    return total_entities


async def _extract_from_chunk(db: AsyncSession, chunk: Chunk) -> int:
    prompt = f"Extrahiere Wissensentitäten aus folgendem akademischen Text:\n\n{chunk.content}"

    raw = await ai_service.chat_claude(prompt, system=EXTRACTION_SYSTEM)
    data = _parse_json(raw)
    if not data:
        return 0

    name_to_entity: dict[str, Entity] = {}
    for e in data.get("entities", []):
        entity = Entity(
            name=e["name"],
            description=e.get("description"),
            entity_type=e.get("entity_type", "concept"),
            topic=e.get("topic"),
            importance=e.get("importance", 0.5),
            confidence=0.8,
            source_document_id=chunk.document_id,
            source_chunk_id=chunk.id,
        )
        db.add(entity)
        await db.flush()
        name_to_entity[e["name"]] = entity

    for r in data.get("relationships", []):
        src = name_to_entity.get(r.get("source"))
        tgt = name_to_entity.get(r.get("target"))
        if not src or not tgt:
            continue
        db.add(Relationship(
            source_entity_id=src.id,
            target_entity_id=tgt.id,
            relationship_type=r.get("type", "relates_to"),
            strength=r.get("strength", 0.5),
            evidence=r.get("evidence"),
            confidence=0.8,
            is_prerequisite=r.get("is_prerequisite", False),
            prerequisite_strictness=r.get("strictness"),
        ))

    await db.flush()
    return len(name_to_entity)


def _parse_json(raw: str) -> dict | None:
    text = raw.strip()
    if text.startswith("```"):
        lines = text.split("\n")
        text = "\n".join(lines[1:-1] if lines[-1].strip() == "```" else lines[1:])
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        logger.warning("Failed to parse extraction JSON")
        return None


async def auto_link_entities(db: AsyncSession, document_id: int) -> int:
    """Link entities whose source chunks have high cosine similarity."""
    query = text("""
        SELECT c1.id AS chunk1_id, c2.id AS chunk2_id,
               1 - (c1.embedding <=> c2.embedding) AS similarity
        FROM chunks c1
        JOIN chunks c2 ON c1.id < c2.id
            AND c1.document_id = :doc_id AND c2.document_id = :doc_id
        WHERE c1.embedding IS NOT NULL AND c2.embedding IS NOT NULL
            AND 1 - (c1.embedding <=> c2.embedding) > :threshold
        ORDER BY similarity DESC
        LIMIT 100
    """)

    result = await db.execute(
        query, {"doc_id": document_id, "threshold": AUTO_LINK_THRESHOLD}
    )

    linked = 0
    for chunk1_id, chunk2_id, similarity in result.fetchall():
        e1 = (await db.execute(
            select(Entity).where(Entity.source_chunk_id == chunk1_id)
        )).scalars().all()
        e2 = (await db.execute(
            select(Entity).where(Entity.source_chunk_id == chunk2_id)
        )).scalars().all()

        for ent1 in e1:
            for ent2 in e2:
                if ent1.id == ent2.id:
                    continue
                db.add(Relationship(
                    source_entity_id=ent1.id,
                    target_entity_id=ent2.id,
                    relationship_type="similar_to",
                    strength=float(similarity),
                    evidence=f"Automatisch verknüpft (Ähnlichkeit: {similarity:.2f})",
                    confidence=float(similarity),
                ))
                linked += 1

    await db.flush()
    return linked

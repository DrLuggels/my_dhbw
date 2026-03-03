from sqlalchemy import select, text
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from app.models.knowledge import Entity, Relationship
from app.services.ai_service import ai_service

# Re-export for backward compatibility
from app.services.knowledge_extraction import extract_entities_from_document  # noqa: F401


async def get_entities(
    db: AsyncSession,
    subject: str | None = None,
    entity_type: str | None = None,
) -> list[Entity]:
    """Get entities with optional filters."""
    stmt = select(Entity).order_by(Entity.importance.desc())
    if subject:
        stmt = stmt.where(Entity.subject == subject)
    if entity_type:
        stmt = stmt.where(Entity.entity_type == entity_type)
    result = await db.execute(stmt)
    return list(result.scalars().all())


async def get_entity(db: AsyncSession, entity_id: int) -> Entity | None:
    result = await db.execute(
        select(Entity)
        .where(Entity.id == entity_id)
        .options(
            selectinload(Entity.outgoing_relationships),
            selectinload(Entity.incoming_relationships),
        )
    )
    return result.scalar_one_or_none()


async def get_graph_data(db: AsyncSession) -> dict:
    """Get all entities and relationships for graph visualization."""
    entities = await db.execute(select(Entity))
    relationships = await db.execute(select(Relationship))

    nodes = [
        {
            "id": e.id,
            "label": e.name,
            "type": e.entity_type,
            "mastery": e.mastery_score,
            "bloom": e.bloom_level,
            "subject": e.subject,
            "topic": e.topic,
        }
        for e in entities.scalars().all()
    ]

    edges = [
        {
            "id": r.id,
            "from": r.source_entity_id,
            "to": r.target_entity_id,
            "label": r.relationship_type,
            "strength": r.strength,
            "is_prerequisite": r.is_prerequisite,
        }
        for r in relationships.scalars().all()
    ]

    return {"nodes": nodes, "edges": edges}


async def semantic_search(db: AsyncSession, query: str, limit: int = 10) -> list[dict]:
    """Search entities by semantic similarity to a query string."""
    embeddings = await ai_service.generate_embeddings([query])
    query_embedding = embeddings[0]

    sql = text("""
        SELECT e.id, e.name, e.description, e.entity_type, e.topic,
               e.mastery_score, e.bloom_level,
               1 - (c.embedding <=> :query_vec::vector) AS similarity
        FROM entities e
        JOIN chunks c ON c.id = e.source_chunk_id
        WHERE c.embedding IS NOT NULL
        ORDER BY c.embedding <=> :query_vec::vector
        LIMIT :lim
    """)

    result = await db.execute(sql, {"query_vec": str(query_embedding), "lim": limit})
    return [
        {
            "id": row.id,
            "name": row.name,
            "description": row.description,
            "entity_type": row.entity_type,
            "topic": row.topic,
            "mastery_score": row.mastery_score,
            "bloom_level": row.bloom_level,
            "similarity": float(row.similarity),
        }
        for row in result.fetchall()
    ]


async def get_weak_areas(db: AsyncSession, limit: int = 20) -> list[Entity]:
    """Get entities with lowest mastery scores."""
    result = await db.execute(
        select(Entity)
        .where(Entity.total_attempts > 0)
        .order_by(Entity.mastery_score.asc())
        .limit(limit)
    )
    return list(result.scalars().all())

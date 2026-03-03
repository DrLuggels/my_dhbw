import logging

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.document import Chunk
from app.services.ai_service import ai_service

logger = logging.getLogger(__name__)

BATCH_SIZE = 50


async def embed_chunks(db: AsyncSession, document_id: int) -> int:
    """Generate and store embeddings for all chunks of a document.

    Args:
        db: Database session.
        document_id: ID of the document whose chunks need embeddings.

    Returns:
        Number of chunks embedded.
    """
    result = await db.execute(
        select(Chunk)
        .where(Chunk.document_id == document_id, Chunk.embedding.is_(None))
        .order_by(Chunk.chunk_index)
    )
    chunks = list(result.scalars().all())

    if not chunks:
        return 0

    embedded_count = 0
    for i in range(0, len(chunks), BATCH_SIZE):
        batch = chunks[i : i + BATCH_SIZE]
        texts = [c.content for c in batch]

        try:
            embeddings = await ai_service.generate_embeddings(texts)
            for chunk, embedding in zip(batch, embeddings):
                chunk.embedding = embedding
            embedded_count += len(batch)
        except Exception:
            logger.exception("Failed to embed batch %d for document %d", i, document_id)
            continue

    await db.flush()
    return embedded_count

from fastapi import APIRouter, Depends, Query
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.base import get_db
from app.schemas.common import ApiResponse
from app.schemas.knowledge import (
    EntityDetail,
    EntityOut,
    GraphData,
    GraphEdge,
    GraphNode,
    SearchResult,
    SemanticSearchRequest,
)
from app.services import knowledge_service

router = APIRouter(prefix="/api/knowledge", tags=["knowledge"])


@router.get("/entities", response_model=ApiResponse[list[EntityOut]])
async def list_entities(
    subject: str | None = Query(None),
    entity_type: str | None = Query(None),
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[EntityOut]]:
    """List entities with optional filters."""
    entities = await knowledge_service.get_entities(db, subject=subject, entity_type=entity_type)
    return ApiResponse(
        data=[EntityOut.model_validate(e) for e in entities],
        message=f"{len(entities)} Entitäten",
    )


@router.get("/entities/{entity_id}", response_model=ApiResponse[EntityDetail])
async def get_entity(
    entity_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[EntityDetail]:
    """Get an entity with its relationships."""
    entity = await knowledge_service.get_entity(db, entity_id)
    if not entity:
        return ApiResponse(success=False, message="Entität nicht gefunden")
    return ApiResponse(data=EntityDetail.model_validate(entity))


@router.get("/graph", response_model=ApiResponse[GraphData])
async def get_graph(
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[GraphData]:
    """Get graph data for visualization (nodes + edges)."""
    raw = await knowledge_service.get_graph_data(db)
    data = GraphData(
        nodes=[GraphNode(**n) for n in raw["nodes"]],
        edges=[
            GraphEdge(
                id=e["id"],
                source=e["from"],
                target=e["to"],
                label=e["label"],
                strength=e["strength"],
                is_prerequisite=e["is_prerequisite"],
            )
            for e in raw["edges"]
        ],
    )
    return ApiResponse(data=data, message=f"{len(data.nodes)} Knoten, {len(data.edges)} Kanten")


@router.post("/search", response_model=ApiResponse[list[SearchResult]])
async def semantic_search(
    request: SemanticSearchRequest,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[SearchResult]]:
    """Search entities by semantic similarity."""
    results = await knowledge_service.semantic_search(db, request.query, request.limit)
    return ApiResponse(
        data=[SearchResult(**r) for r in results],
        message=f"{len(results)} Ergebnisse",
    )


@router.get("/weak-areas", response_model=ApiResponse[list[EntityOut]])
async def get_weak_areas(
    limit: int = Query(20, ge=1, le=100),
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[list[EntityOut]]:
    """Get entities with the lowest mastery scores."""
    entities = await knowledge_service.get_weak_areas(db, limit)
    return ApiResponse(
        data=[EntityOut.model_validate(e) for e in entities],
        message=f"{len(entities)} Schwachstellen",
    )


@router.post("/extract/{document_id}", response_model=ApiResponse[dict])
async def extract_entities(
    document_id: int,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[dict]:
    """Extract knowledge entities from a document's chunks."""
    count = await knowledge_service.extract_entities_from_document(db, document_id)
    await db.commit()
    return ApiResponse(
        data={"entities_created": count, "document_id": document_id},
        message=f"{count} Entitäten extrahiert",
    )

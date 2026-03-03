from datetime import datetime

from pydantic import BaseModel


class EntityOut(BaseModel):
    """Entity response schema."""

    model_config = {"from_attributes": True}

    id: int
    name: str
    description: str | None = None
    entity_type: str
    subject: str | None = None
    topic: str | None = None
    subtopic: str | None = None
    importance: float
    confidence: float
    mastery_score: float
    bloom_level: int
    total_attempts: int
    correct_attempts: int
    next_review: datetime | None = None
    created_at: datetime


class RelationshipOut(BaseModel):
    """Relationship response schema."""

    model_config = {"from_attributes": True}

    id: int
    source_entity_id: int
    target_entity_id: int
    relationship_type: str
    strength: float
    evidence: str | None = None
    is_prerequisite: bool


class EntityDetail(EntityOut):
    """Entity with its relationships."""

    outgoing_relationships: list[RelationshipOut] = []
    incoming_relationships: list[RelationshipOut] = []


class GraphNode(BaseModel):
    id: int
    label: str
    type: str
    mastery: float
    bloom: int
    subject: str | None = None
    topic: str | None = None


class GraphEdge(BaseModel):
    id: int
    source: int  # "from" in vis.js
    target: int  # "to" in vis.js
    label: str
    strength: float
    is_prerequisite: bool


class GraphData(BaseModel):
    nodes: list[GraphNode] = []
    edges: list[GraphEdge] = []


class SemanticSearchRequest(BaseModel):
    query: str
    limit: int = 10


class SearchResult(BaseModel):
    id: int
    name: str
    description: str | None = None
    entity_type: str
    topic: str | None = None
    mastery_score: float
    bloom_level: int
    similarity: float

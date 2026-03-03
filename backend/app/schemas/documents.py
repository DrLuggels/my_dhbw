from datetime import datetime

from pydantic import BaseModel


class DocumentOut(BaseModel):
    """Document response schema."""

    model_config = {"from_attributes": True}

    id: int
    title: str
    filename: str
    filetype: str
    filesize: int
    doc_category: str
    processing_status: str
    metadata_json: dict | None = None
    created_at: datetime
    updated_at: datetime


class ChunkOut(BaseModel):
    """Chunk response schema (without embedding vector)."""

    model_config = {"from_attributes": True}

    id: int
    document_id: int
    content: str
    chunk_index: int
    chunk_type: str
    topic_label: str | None = None
    section_heading: str | None = None
    page_number: int | None = None
    metadata_json: dict | None = None
    created_at: datetime


class DocumentDetail(DocumentOut):
    """Document with its chunks."""

    chunks: list[ChunkOut] = []
    chunk_count: int = 0


class ProcessingStatus(BaseModel):
    """Status update during document processing."""

    document_id: int
    status: str
    message: str
    chunks_created: int = 0
    chunks_embedded: int = 0

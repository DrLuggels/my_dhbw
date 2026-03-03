from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Any


@dataclass
class ChunkResult:
    """Output of a chunking strategy."""

    content: str
    chunk_index: int
    chunk_type: str = "mixed"
    topic_label: str | None = None
    section_heading: str | None = None
    page_number: int | None = None
    metadata: dict[str, Any] = field(default_factory=dict)


class ChunkingStrategy(ABC):
    """Abstract base for all document chunking strategies."""

    @abstractmethod
    async def chunk(self, content: Any, metadata: dict[str, Any]) -> list[ChunkResult]:
        """Split document content into semantic chunks.

        Args:
            content: Parsed document content (type depends on strategy).
            metadata: Document-level metadata (title, filename, etc.).

        Returns:
            Ordered list of chunks with metadata.
        """

    def _build_context_header(
        self,
        doc_title: str,
        section: str | None = None,
        prev_topic: str | None = None,
    ) -> str:
        """Build a context header for chunk overlap."""
        parts = [f"Dokument: {doc_title}"]
        if section:
            parts.append(f"Abschnitt: {section}")
        if prev_topic:
            parts.append(f"Vorheriges Thema: {prev_topic}")
        return " | ".join(parts)

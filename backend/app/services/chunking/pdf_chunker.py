import re
from typing import Any

from app.services.chunking.base import ChunkResult, ChunkingStrategy
from app.utils.text import clean_text, count_tokens

MIN_CHUNK_TOKENS = 50
TARGET_CHUNK_TOKENS = 600
MAX_CHUNK_TOKENS = 800


class SlidesExportChunker(ChunkingStrategy):
    """Chunks slide-export PDFs: one page = one slide, grouped by topic."""

    async def chunk(self, content: Any, metadata: dict[str, Any]) -> list[ChunkResult]:
        pages: list[str] = content
        doc_title = metadata.get("title", "")
        chunks: list[ChunkResult] = []

        for i, page_text in enumerate(pages):
            text = clean_text(page_text)
            if count_tokens(text) < MIN_CHUNK_TOKENS:
                continue

            header = self._build_context_header(
                doc_title,
                prev_topic=chunks[-1].topic_label if chunks else None,
            )
            full_content = f"[{header}]\n\n{text}" if header else text

            chunks.append(ChunkResult(
                content=full_content,
                chunk_index=len(chunks),
                chunk_type=_detect_slide_type(text),
                page_number=i + 1,
                metadata={"source_page": i + 1},
            ))

        return chunks


class TextbookChunker(ChunkingStrategy):
    """Chunks textbook/script PDFs by headings, 500-800 tokens per chunk."""

    async def chunk(self, content: Any, metadata: dict[str, Any]) -> list[ChunkResult]:
        pages: list[str] = content
        doc_title = metadata.get("title", "")
        full_text = "\n\n".join(clean_text(p) for p in pages)

        sections = _split_by_headings(full_text)
        chunks: list[ChunkResult] = []

        for heading, body in sections:
            sub_chunks = _split_to_token_limit(body, TARGET_CHUNK_TOKENS, MAX_CHUNK_TOKENS)

            for j, text in enumerate(sub_chunks):
                if count_tokens(text) < MIN_CHUNK_TOKENS:
                    continue

                header = self._build_context_header(
                    doc_title,
                    section=heading,
                    prev_topic=chunks[-1].topic_label if chunks else None,
                )
                full_content = f"[{header}]\n\n{text}"

                chunks.append(ChunkResult(
                    content=full_content,
                    chunk_index=len(chunks),
                    chunk_type=_detect_text_type(text),
                    section_heading=heading,
                    metadata={"section": heading, "sub_index": j},
                ))

        return chunks


class ExerciseChunker(ChunkingStrategy):
    """Chunks exercise sheets: one task = one chunk."""

    async def chunk(self, content: Any, metadata: dict[str, Any]) -> list[ChunkResult]:
        pages: list[str] = content
        doc_title = metadata.get("title", "")
        full_text = "\n\n".join(clean_text(p) for p in pages)

        tasks = _split_by_tasks(full_text)
        chunks: list[ChunkResult] = []

        for task_num, task_text in tasks:
            if count_tokens(task_text) < MIN_CHUNK_TOKENS:
                continue

            header = self._build_context_header(doc_title, section=f"Aufgabe {task_num}")

            chunks.append(ChunkResult(
                content=f"[{header}]\n\n{task_text}",
                chunk_index=len(chunks),
                chunk_type="exercise",
                section_heading=f"Aufgabe {task_num}",
                metadata={"task_number": task_num},
            ))

        return chunks


def _detect_slide_type(text: str) -> str:
    """Guess the chunk type from slide content."""
    lower = text.lower()
    if any(w in lower for w in ("definition:", "def:", "ist definiert als")):
        return "definition"
    if any(w in lower for w in ("beispiel", "example", "z.b.", "e.g.")):
        return "example"
    if any(w in lower for w in ("formel", "formula", "=", "∑", "∫")):
        return "formula"
    if any(w in lower for w in ("übersicht", "agenda", "gliederung", "inhalt")):
        return "overview"
    return "theory"


def _detect_text_type(text: str) -> str:
    lower = text.lower()
    if re.search(r"(definition|definiert als|wird .* bezeichnet)", lower):
        return "definition"
    if re.search(r"(beispiel|example|z\.b\.|bspw\.)", lower):
        return "example"
    return "theory"


def _split_by_headings(text: str) -> list[tuple[str, str]]:
    """Split text into (heading, body) pairs by markdown-style or uppercase headings."""
    heading_re = re.compile(
        r"^(#{1,3}\s+.+|[A-ZÄÖÜ][A-ZÄÖÜ\s\d\.]{4,}|\d+(?:\.\d+)*\.?\s+[A-ZÄÖÜ].{2,})$",
        re.MULTILINE,
    )
    parts: list[tuple[str, str]] = []
    matches = list(heading_re.finditer(text))

    if not matches:
        return [("", text)]

    # Text before first heading
    if matches[0].start() > 0:
        parts.append(("Einleitung", text[: matches[0].start()].strip()))

    for i, m in enumerate(matches):
        heading = m.group().strip().lstrip("#").strip()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        body = text[m.end() : end].strip()
        if body:
            parts.append((heading, body))

    return parts


def _split_to_token_limit(text: str, target: int, maximum: int) -> list[str]:
    """Split text into chunks respecting token limits, splitting at paragraph boundaries."""
    paragraphs = text.split("\n\n")
    chunks: list[str] = []
    current: list[str] = []
    current_tokens = 0

    for para in paragraphs:
        para_tokens = count_tokens(para)
        if current_tokens + para_tokens > maximum and current:
            chunks.append("\n\n".join(current))
            current = [para]
            current_tokens = para_tokens
        else:
            current.append(para)
            current_tokens += para_tokens

    if current:
        chunks.append("\n\n".join(current))

    return chunks


def _split_by_tasks(text: str) -> list[tuple[str, str]]:
    """Split exercise text into individual tasks by task number patterns."""
    task_re = re.compile(
        r"(?:^|\n)((?:Aufgabe|Übung|Exercise|Task|Frage)\s*(\d+))",
        re.IGNORECASE,
    )
    matches = list(task_re.finditer(text))

    if not matches:
        return [("1", text)]

    tasks: list[tuple[str, str]] = []
    for i, m in enumerate(matches):
        num = m.group(2)
        start = m.start()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        tasks.append((num, text[start:end].strip()))

    return tasks

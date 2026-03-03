from typing import Any

from bs4 import BeautifulSoup

from app.services.chunking.base import ChunkResult, ChunkingStrategy
from app.utils.text import clean_text, count_tokens

TARGET_TOKENS = 600
MAX_TOKENS = 800
MIN_TOKENS = 50

HEADING_TAGS = {"h1", "h2", "h3", "h4"}


class HtmlChunker(ChunkingStrategy):
    """Chunks HTML content by headings, preserving code blocks."""

    async def chunk(self, content: Any, metadata: dict[str, Any]) -> list[ChunkResult]:
        """Content should be an HTML string."""
        html: str = content
        doc_title = metadata.get("title", "")
        soup = BeautifulSoup(html, "lxml")

        # Remove scripts and styles
        for tag in soup(["script", "style", "nav", "footer", "header"]):
            tag.decompose()

        sections = _extract_sections(soup)
        chunks: list[ChunkResult] = []

        for heading, body, is_code in sections:
            if count_tokens(body) < MIN_TOKENS:
                continue

            header = self._build_context_header(
                doc_title,
                section=heading,
                prev_topic=chunks[-1].topic_label if chunks else None,
            )

            chunk_type = "exercise" if is_code else _detect_type(body)
            text = clean_text(f"[{header}]\n\n{body}")

            if count_tokens(text) > MAX_TOKENS:
                sub_parts = _split_text(body, TARGET_TOKENS, MAX_TOKENS)
                for j, part in enumerate(sub_parts):
                    chunks.append(ChunkResult(
                        content=clean_text(f"[{header}]\n\n{part}"),
                        chunk_index=len(chunks),
                        chunk_type=chunk_type,
                        section_heading=heading,
                        metadata={"sub_index": j},
                    ))
            else:
                chunks.append(ChunkResult(
                    content=text,
                    chunk_index=len(chunks),
                    chunk_type=chunk_type,
                    section_heading=heading,
                ))

        return chunks


def _extract_sections(soup: BeautifulSoup) -> list[tuple[str, str, bool]]:
    """Extract (heading, body, is_code) triples from parsed HTML."""
    sections: list[tuple[str, str, bool]] = []
    current_heading = ""
    current_parts: list[str] = []

    body = soup.body or soup
    for element in body.children:
        if not hasattr(element, "name") or element.name is None:
            text = element.string
            if text and text.strip():
                current_parts.append(text.strip())
            continue

        if element.name in HEADING_TAGS:
            if current_parts:
                sections.append((
                    current_heading,
                    "\n\n".join(current_parts),
                    False,
                ))
                current_parts = []
            current_heading = element.get_text(strip=True)

        elif element.name in ("pre", "code"):
            # Code blocks as separate chunks
            if current_parts:
                sections.append((current_heading, "\n\n".join(current_parts), False))
                current_parts = []
            code_text = element.get_text()
            sections.append((current_heading, f"```\n{code_text}\n```", True))

        else:
            text = element.get_text(separator="\n", strip=True)
            if text:
                current_parts.append(text)

    if current_parts:
        sections.append((current_heading, "\n\n".join(current_parts), False))

    return sections


def _detect_type(text: str) -> str:
    lower = text.lower()
    if "definition" in lower:
        return "definition"
    if "beispiel" in lower or "example" in lower:
        return "example"
    return "theory"


def _split_text(text: str, target: int, maximum: int) -> list[str]:
    paragraphs = text.split("\n\n")
    chunks: list[str] = []
    current: list[str] = []
    current_tokens = 0

    for para in paragraphs:
        pt = count_tokens(para)
        if current_tokens + pt > maximum and current:
            chunks.append("\n\n".join(current))
            current = [para]
            current_tokens = pt
        else:
            current.append(para)
            current_tokens += pt

    if current:
        chunks.append("\n\n".join(current))
    return chunks

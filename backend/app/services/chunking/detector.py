import re
from dataclasses import dataclass


@dataclass
class PageAnalysis:
    """Analysis results for a single page."""

    text: str
    word_count: int
    line_count: int
    bullet_ratio: float
    has_headings: bool
    avg_line_length: float


def analyze_page(text: str) -> PageAnalysis:
    """Compute structural metrics for a page of text."""
    lines = [l for l in text.split("\n") if l.strip()]
    word_count = len(text.split())
    line_count = len(lines)
    avg_line_length = sum(len(l) for l in lines) / max(line_count, 1)

    bullet_lines = sum(1 for l in lines if re.match(r"^\s*[•\-\*\d+\.]\s", l))
    bullet_ratio = bullet_lines / max(line_count, 1)

    has_headings = any(
        re.match(r"^(#{1,3}\s|[A-ZÄÖÜ][A-ZÄÖÜ\s]{3,}$)", l.strip())
        for l in lines
    )

    return PageAnalysis(
        text=text,
        word_count=word_count,
        line_count=line_count,
        bullet_ratio=bullet_ratio,
        has_headings=has_headings,
        avg_line_length=avg_line_length,
    )


def detect_pdf_category(pages: list[str]) -> str:
    """Classify a PDF based on the first few pages.

    Args:
        pages: Text content of the first 3-5 pages.

    Returns:
        One of: slides_export, textbook, exercise_sheet, paper, scan.
    """
    if not pages:
        return "unknown"

    sample = pages[:min(5, len(pages))]
    analyses = [analyze_page(p) for p in sample]

    avg_words = sum(a.word_count for a in analyses) / len(analyses)
    avg_bullets = sum(a.bullet_ratio for a in analyses) / len(analyses)
    avg_line_len = sum(a.avg_line_length for a in analyses) / len(analyses)

    # Scan detection: very little text
    if avg_words < 20:
        return "scan"

    # Exercise sheet: numbered tasks pattern
    exercise_pattern = re.compile(r"(Aufgabe|Übung|Exercise|Task)\s*\d", re.IGNORECASE)
    exercise_hits = sum(
        1 for a in analyses if exercise_pattern.search(a.text)
    )
    if exercise_hits >= len(analyses) * 0.4:
        return "exercise_sheet"

    # Slides export: short text, many bullets, short lines
    if avg_words < 150 and avg_bullets > 0.3 and avg_line_len < 60:
        return "slides_export"

    # Textbook: long text, headings, paragraphs
    if avg_words > 200 and any(a.has_headings for a in analyses):
        return "textbook"

    # Paper: moderate to long text, structured
    if avg_words > 150:
        return "paper"

    return "slides_export"


def detect_filetype_category(filetype: str) -> str:
    """Map file extension to a default document category."""
    mapping = {
        "pptx": "slides_export",
        "ppt": "slides_export",
        "docx": "textbook",
        "doc": "textbook",
        "html": "textbook",
        "htm": "textbook",
        "txt": "textbook",
    }
    return mapping.get(filetype.lower().lstrip("."), "unknown")

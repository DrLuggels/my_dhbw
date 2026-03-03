import re


def clean_text(text: str) -> str:
    """Remove excessive whitespace and normalize line endings."""
    text = re.sub(r"\r\n", "\n", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    text = re.sub(r"[ \t]+", " ", text)
    return text.strip()


def count_tokens(text: str, model: str = "cl100k_base") -> int:
    """Count tokens using tiktoken."""
    import tiktoken

    encoding = tiktoken.get_encoding(model)
    return len(encoding.encode(text))


def truncate_text(text: str, max_tokens: int = 800, model: str = "cl100k_base") -> str:
    """Truncate text to fit within a token limit."""
    import tiktoken

    encoding = tiktoken.get_encoding(model)
    tokens = encoding.encode(text)
    if len(tokens) <= max_tokens:
        return text
    return encoding.decode(tokens[:max_tokens])

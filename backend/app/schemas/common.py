from typing import Generic, TypeVar

from pydantic import BaseModel

T = TypeVar("T")


class ApiResponse(BaseModel, Generic[T]):
    """Unified API response wrapper used by all endpoints."""

    success: bool = True
    data: T | None = None
    message: str = ""
    errors: list[str] = []

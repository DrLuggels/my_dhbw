"""Pydantic schemas for email API."""

from datetime import datetime

from pydantic import BaseModel


class EmailSummary(BaseModel):
    """Email list item."""

    item_id: str
    subject: str
    sender_name: str
    sender_email: str
    received: datetime
    is_read: bool
    has_attachments: bool


class EmailDetail(BaseModel):
    """Full email with body."""

    item_id: str
    subject: str
    sender_name: str
    sender_email: str
    received: datetime
    is_read: bool
    has_attachments: bool
    body: str
    attachments: list[str]


class EmailTestResult(BaseModel):
    """Result of email connection test."""

    success: bool
    email: str
    inbox_count: int
    unread_count: int


class EmailInboxResponse(BaseModel):
    """Paginated inbox response."""

    emails: list[EmailSummary]
    total: int
    unread: int

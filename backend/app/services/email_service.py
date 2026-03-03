"""Email service using Exchange Web Services (EWS)."""

import asyncio
import logging
from functools import partial

from exchangelib import Account, Configuration, Credentials, DELEGATE

from app.schemas.email import (
    EmailDetail,
    EmailInboxResponse,
    EmailSummary,
    EmailTestResult,
)

logger = logging.getLogger(__name__)


def _connect(server: str, username: str, password: str, email: str) -> Account:
    """Create an EWS account connection (blocking)."""
    creds = Credentials(username=username, password=password)
    config = Configuration(server=server, credentials=creds)
    return Account(
        primary_smtp_address=email,
        config=config,
        autodiscover=False,
        access_type=DELEGATE,
    )


async def _run_sync(func, *args, **kwargs):
    """Run a blocking function in a thread executor."""
    loop = asyncio.get_event_loop()
    return await loop.run_in_executor(None, partial(func, *args, **kwargs))


def _test_connection(server: str, username: str, password: str, email: str) -> EmailTestResult:
    """Test EWS connection (blocking)."""
    account = _connect(server, username, password, email)
    return EmailTestResult(
        success=True,
        email=email,
        inbox_count=account.inbox.total_count,
        unread_count=account.inbox.unread_count,
    )


def _get_inbox(
    server: str, username: str, password: str, email: str,
    limit: int = 30, offset: int = 0,
) -> EmailInboxResponse:
    """Fetch inbox emails (blocking)."""
    account = _connect(server, username, password, email)
    qs = account.inbox.all().order_by("-datetime_received")

    emails = []
    for item in qs[offset:offset + limit]:
        sender = item.sender
        emails.append(EmailSummary(
            item_id=item.id,
            subject=item.subject or "(Kein Betreff)",
            sender_name=sender.name if sender else "",
            sender_email=sender.email_address if sender else "",
            received=item.datetime_received,
            is_read=item.is_read,
            has_attachments=bool(item.attachments),
        ))

    return EmailInboxResponse(
        emails=emails,
        total=account.inbox.total_count,
        unread=account.inbox.unread_count,
    )


def _get_email(
    server: str, username: str, password: str, email: str, item_id: str,
) -> EmailDetail:
    """Fetch a single email by ID (blocking)."""
    account = _connect(server, username, password, email)
    qs = account.inbox.filter(id=item_id)
    item = qs.get()

    sender = item.sender
    attachment_names = [a.name for a in (item.attachments or []) if hasattr(a, "name")]

    body_text = ""
    if item.text_body:
        body_text = item.text_body
    elif item.body:
        body_text = str(item.body)

    return EmailDetail(
        item_id=item.id,
        subject=item.subject or "(Kein Betreff)",
        sender_name=sender.name if sender else "",
        sender_email=sender.email_address if sender else "",
        received=item.datetime_received,
        is_read=item.is_read,
        has_attachments=bool(item.attachments),
        body=body_text,
        attachments=attachment_names,
    )


class EmailService:
    """Async wrapper around EWS operations."""

    async def test_connection(
        self, server: str, username: str, password: str, email: str,
    ) -> EmailTestResult:
        return await _run_sync(_test_connection, server, username, password, email)

    async def get_inbox(
        self, server: str, username: str, password: str, email: str,
        limit: int = 30, offset: int = 0,
    ) -> EmailInboxResponse:
        return await _run_sync(_get_inbox, server, username, password, email, limit, offset)

    async def get_email(
        self, server: str, username: str, password: str, email: str, item_id: str,
    ) -> EmailDetail:
        return await _run_sync(_get_email, server, username, password, email, item_id)


email_service = EmailService()

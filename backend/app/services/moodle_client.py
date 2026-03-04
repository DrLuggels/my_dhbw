"""Low-level Moodle Web Service API client.

Handles HTTP calls, token auth, and response parsing.
"""

import logging

import httpx
from sqlalchemy import select

from app.config import settings

logger = logging.getLogger(__name__)

TIMEOUT = 30.0


class MoodleClient:
    """Async HTTP client for Moodle Web Services REST API."""

    def __init__(self, base_url: str = "", token: str = "") -> None:
        self._explicit_base_url = base_url
        self._explicit_token = token
        self.base_url = ""
        self.token = ""
        self._client: httpx.AsyncClient | None = None
        self._initialized = False

    async def _ensure_init(self) -> None:
        """Load base_url/token from DB settings, falling back to env."""
        if self._initialized:
            return
        self._initialized = True

        if self._explicit_base_url and self._explicit_token:
            self.base_url = self._explicit_base_url.rstrip("/")
            self.token = self._explicit_token
            return

        # Try DB settings first
        try:
            from app.models.base import async_session
            from app.models.settings import AppSettings

            async with async_session() as db:
                result = await db.execute(select(AppSettings).where(AppSettings.id == 1))
                s = result.scalar_one_or_none()
                if s:
                    if not self._explicit_base_url and s.moodle_base_url:
                        self.base_url = s.moodle_base_url.rstrip("/")
                    if not self._explicit_token and s.moodle_token:
                        self.token = s.moodle_token
        except Exception:
            logger.debug("Could not load Moodle settings from DB")

        # Fallback to env
        if not self.base_url:
            self.base_url = (self._explicit_base_url or settings.moodle_base_url).rstrip("/")
        if not self.token:
            self.token = self._explicit_token or settings.moodle_token

    @property
    def client(self) -> httpx.AsyncClient:
        if self._client is None:
            self._client = httpx.AsyncClient(timeout=TIMEOUT)
        return self._client

    @property
    def ws_url(self) -> str:
        return f"{self.base_url}/webservice/rest/server.php"

    async def call(self, function: str, **params) -> dict | list:
        """Call a Moodle Web Service function.

        Args:
            function: WS function name (e.g. core_course_get_courses).
            **params: Additional parameters for the function.

        Returns:
            Parsed JSON response.
        """
        await self._ensure_init()
        data = {
            "wstoken": self.token,
            "wsfunction": function,
            "moodlewsrestformat": "json",
            **params,
        }
        response = await self.client.post(self.ws_url, data=data)
        response.raise_for_status()
        result = response.json()

        if isinstance(result, dict) and "exception" in result:
            raise MoodleApiError(result.get("message", "Unknown Moodle error"))

        return result

    async def download_file(self, url: str) -> bytes:
        """Download a file from Moodle (appends token)."""
        await self._ensure_init()
        sep = "&" if "?" in url else "?"
        full_url = f"{url}{sep}token={self.token}"
        response = await self.client.get(full_url)
        response.raise_for_status()
        return response.content

    async def test_connection(self) -> dict:
        """Test connection by fetching site info."""
        return await self.call("core_webservice_get_site_info")

    async def get_courses(self) -> list[dict]:
        info = await self.call("core_webservice_get_site_info")
        user_id = info.get("userid")
        return await self.call("core_enrol_get_users_courses", userid=user_id)

    async def get_assignments(self, course_ids: list[int] | None = None) -> list[dict]:
        result = await self.call("mod_assign_get_assignments")
        courses = result.get("courses", [])
        if course_ids:
            courses = [c for c in courses if c.get("id") in course_ids]
        return courses

    async def get_calendar_events(self) -> list[dict]:
        result = await self.call(
            "core_calendar_get_calendar_events",
            **{"events[timestart]": 0, "events[timeend]": 9999999999},
        )
        return result.get("events", [])

    async def get_course_contents(self, course_id: int) -> list[dict]:
        return await self.call("core_course_get_contents", courseid=course_id)

    async def close(self) -> None:
        if self._client:
            await self._client.aclose()
            self._client = None


class MoodleApiError(Exception):
    pass

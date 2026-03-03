from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api.documents import router as documents_router
from app.api.health import router as health_router
from app.api.knowledge import router as knowledge_router
from app.api.learning import router as learning_router
from app.api.moodle import router as moodle_router
from app.api.calendar import router as calendar_router
from app.config import settings
from app.models.base import engine


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Startup and shutdown events."""
    yield
    await engine.dispose()


app = FastAPI(
    title="DHBW Study Automation",
    version="2.0.0",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=[settings.frontend_url],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(health_router)
app.include_router(documents_router)
app.include_router(knowledge_router)
app.include_router(learning_router)
app.include_router(moodle_router)
app.include_router(calendar_router)

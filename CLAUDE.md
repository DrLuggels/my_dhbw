# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DHBW Study Automation v2 - AI-powered adaptive learning platform for a single DHBW student. Complete rewrite with clean architecture. Three clients: Vue.js web app, Kotlin Multiplatform Android app, and the FastAPI backend.

## Tech Stack

- **Backend:** Python 3.12 + FastAPI (`backend/`)
- **Frontend:** Vue.js 3 + Vuetify 3 + TypeScript (`frontend/`)
- **Mobile:** Kotlin Multiplatform + Compose + Voyager + Koin + SQLDelight (`dhbw-app/`)
- **Database:** PostgreSQL 16 + pgvector (unified relational + vector)
- **AI:** OpenAI (embeddings), Anthropic Claude (entity extraction, exercises), Google Gemini (vision/OCR)

## Key Design Decisions

- **Single-user app** - no authentication, no multi-tenancy
- **Chunking is the foundation** - specialized strategies per document type
- **One learning system** - unified FSRS + Bloom + Priority engine
- **Extensible** - Strategy Pattern for chunkers, consistent patterns everywhere

## Build & Run

```bash
# Start database (dev: DB only)
docker compose up -d

# Backend
cd backend
pip install -e .
uvicorn app.main:app --reload --port 8000

# Frontend
cd frontend
npm install
npm run dev    # Port 5173, proxies /api → localhost:8000

# Lint backend
cd backend && ruff check app/       # line-length 100, rules: E,F,I,N,UP,B
cd backend && ruff format app/

# Frontend build (type-check + build)
cd frontend && npm run build

# Database migrations
cd backend && alembic upgrade head       # apply all
cd backend && alembic revision --autogenerate -m "description"  # create new

# Production deploy
./deploy.sh                              # or: git push server main
```

## Code Codex

### File Limits
- Max ~200 lines per file (soft limit)
- One responsibility per file

### Backend (Python)
- **Naming:** snake_case files/functions, PascalCase classes, SCREAMING_SNAKE_CASE constants
- **API Response:** Always `ApiResponse[T]` from `schemas/common.py` with `{ success, data, message, errors }`
- **Imports:** stdlib → third-party → local
- **Docstrings:** Google-style, only for non-obvious functions
- **DB sessions:** `get_db()` async dependency yields `AsyncSession` (SQLAlchemy 2.x async)
- **AI config:** API keys/models stored in DB `app_settings` table, falls back to `.env`

### Frontend (TypeScript/Vue)
- **No `any`** - use `unknown` if needed
- **Composition API:** `<script setup lang="ts">` always
- **API calls:** Only through `api/*.ts` files, never in components
- **State:** Only through Pinia stores (composition API style with `ref`/`computed`)
- **Every View:** Header → Loading → Error → Empty → Data

### Vuetify Design System
- **Colors:** primary=#1565C0, accent=#00897B, bg=#FAFAFA, surface=#FFFFFF
- **Cards:** elevation="1" rounded="lg"
- **Inputs:** variant="outlined"
- **Spacing:** multiples of 4 (mt-4, pa-6)
- **Mastery:** red(<40%), orange(40-70%), green(>70%)
- **Bloom:** light blue(1) → dark blue(6) gradient

## Architecture

### Backend

Routers registered in `backend/app/main.py` → all prefixed `/api/{feature}`. Each API endpoint delegates to a service. Services contain all business logic.

**Document processing pipeline** (`document_service.py`):
1. Upload → `detect_pdf_category()` classifies as slides_export|textbook|exercise_sheet|paper|scan
2. Dispatch to concrete `ChunkingStrategy` (ABC in `chunking/base.py`)
3. `semantic_split_chunks()` - OpenAI-powered sub-chunk splitting for large chunks
4. `embed_chunks()` - OpenAI embeddings in batches of 50
5. `extract_entities_from_document()` - Claude extracts entities/relationships as JSON

**Learning engine** (`services/fsrs.py` + `priority_engine.py`):
- FSRS states: NEW → LEARNING → REVIEW → RELEARNING (W0-W16 params)
- Priority = 30% deadline + 20% relevance + 25% mastery_gap + 15% decay + 10% bloom_gap
- Ebbinghaus decay: 5%/day rate, 40% minimum floor
- Bloom advancement: ≥3 attempts AND ≥70% success → next level
- Vygotsky ZPD: 20/40/40 easy/medium/hard distribution

```
backend/app/
├── api/          → REST endpoints (max 150 lines each)
├── models/       → SQLAlchemy ORM + base.py (engine, session, get_db)
├── schemas/      → Pydantic models + common.py (ApiResponse[T])
├── services/     → Business logic (max 200 lines each)
│   └── chunking/ → Strategy Pattern: base.py (ABC), detector.py, *_chunker.py
└── utils/        → Helpers
```

### Frontend

Single Axios instance in `api/client.ts` (Vite dev proxy handles `/api` → backend). Vue Router with lazy-loaded routes at `/dashboard`, `/documents`, `/learning`, `/knowledge`, `/email`, `/calendar`, `/settings`.

```
frontend/src/
├── api/          → Axios API clients (per feature)
├── stores/       → Pinia stores (per feature)
├── views/        → Page components
├── components/   → Feature components (per feature folder)
│   ├── common/     layout/     documents/     learning/
└── types/        → TypeScript interfaces
```

### Mobile App (`dhbw-app/`)

Kotlin Multiplatform targeting Android. Communicates with the same FastAPI backend via Ktor HTTP client. Uses Voyager for navigation, Koin for DI, SQLDelight for local DB.

## Key Reference Files

- `PLAN.md` - Full implementation plan with all phases
- `NEUANFANG_ANALYSE.md` - All algorithms, schemas, business rules from old system
- `.claude/plugins/` - Agent/skill reference files
- `.env.example` - All environment variables (API keys, DB URL, Moodle token, etc.)

## Deployment

- **Server:** 192.168.178.198 (SSH root@, key auth)
- **Deploy:** `git push server main` (auto-deploy via `deploy/post-receive` hook)
- **Project path:** /root/dhbw-automation-deploy/
- **Prod stack:** docker-compose.prod.yml → db + backend + frontend + Caddy reverse proxy
- **Caddy** routes `/api/*` and `/health` to backend, everything else to frontend

## External Integrations

- **Moodle:** https://moodle.dhbw-ravensburg.de (token auth)
- **Rapla:** https://rapla-ravensburg.dhbw.de/rapla (iCal)
- **OpenAI:** text-embedding-3-small (1536D), gpt-5-mini
- **Anthropic:** claude-sonnet-4-6 (entity extraction, exercise generation)
- **Google:** gemini-3-flash-preview (vision/OCR for slide images)

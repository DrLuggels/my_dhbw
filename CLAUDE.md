# CLAUDE.md

## Project Overview

DHBW Study Automation v2 - AI-powered adaptive learning platform for a single DHBW student. Complete rewrite with clean architecture.

## Tech Stack

- **Backend:** Python 3.12 + FastAPI (`backend/`)
- **Frontend:** Vue.js 3 + Vuetify 3 + TypeScript (`frontend/`)
- **Database:** PostgreSQL 16 + pgvector (unified relational + vector)
- **AI:** OpenAI (embeddings), Anthropic Claude (entity extraction, exercises), Google Gemini (vision/OCR)

## Key Design Decisions

- **Single-user app** - no authentication, no multi-tenancy
- **Chunking is the foundation** - specialized strategies per document type
- **One learning system** - unified FSRS + Bloom + Priority engine
- **Extensible** - Strategy Pattern for chunkers, consistent patterns everywhere

## Build & Run

```bash
# Start database
docker compose up -d

# Backend
cd backend
pip install -e .
uvicorn app.main:app --reload --port 8000

# Frontend
cd frontend
npm install
npm run dev    # Port 5173
```

## Code Codex

### File Limits
- Max ~200 lines per file (soft limit)
- One responsibility per file

### Backend (Python)
- **Naming:** snake_case files/functions, PascalCase classes, SCREAMING_SNAKE_CASE constants
- **API Response:** Always `ApiResponse[T]` with `{ success, data, message, errors }`
- **Imports:** stdlib → third-party → local
- **Docstrings:** Google-style, only for non-obvious functions

### Frontend (TypeScript/Vue)
- **No `any`** - use `unknown` if needed
- **Composition API:** `<script setup lang="ts">` always
- **API calls:** Only through `api/*.ts` files, never in components
- **State:** Only through Pinia stores
- **Every View:** Header → Loading → Error → Empty → Data

### Vuetify Design System
- **Colors:** primary=#1565C0, accent=#00897B, bg=#FAFAFA, surface=#FFFFFF
- **Cards:** elevation="1" rounded="lg"
- **Inputs:** variant="outlined"
- **Spacing:** multiples of 4 (mt-4, pa-6)
- **Mastery:** red(<40%), orange(40-70%), green(>70%)
- **Bloom:** light blue(1) → dark blue(6) gradient

## Architecture

```
backend/app/
├── api/          → REST endpoints (max 150 lines each)
├── models/       → SQLAlchemy ORM (max 100 lines each)
├── schemas/      → Pydantic models (max 80 lines each)
├── services/     → Business logic (max 200 lines each)
│   └── chunking/ → Strategy Pattern per doc type
└── utils/        → Helpers

frontend/src/
├── api/          → Axios API clients (per feature)
├── stores/       → Pinia stores (per feature)
├── views/        → Page components
├── components/   → Feature components (per feature folder)
└── types/        → TypeScript interfaces
```

## Key Reference Files

- `PLAN.md` - Full implementation plan with all phases
- `NEUANFANG_ANALYSE.md` - All algorithms, schemas, business rules from old system
- `.claude/plugins/` - Agent/skill reference files

## Deployment

- **Server:** 192.168.178.198 (SSH root@, key auth)
- **Deploy:** `git push server main` (auto-deploy via post-receive hook)
- **Project path:** /root/dhbw-automation-deploy/

## External Integrations

- **Moodle:** https://moodle.dhbw-ravensburg.de (token auth)
- **Rapla:** https://rapla-ravensburg.dhbw.de/rapla (iCal)
- **OpenAI:** text-embedding-3-small (1536D), gpt-5-mini
- **Anthropic:** claude-sonnet-4-5 (entity extraction, exercise generation)
- **Google:** gemini-3-flash-preview (vision/OCR for slide images)

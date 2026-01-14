# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DHBW Study Automation System - an AI-powered study management platform for DHBW students with document analysis, live lecture transcription, Moodle integration, and adaptive learning features.

## Tech Stack

- **Backend:** .NET 8 Web API (`dhbw-automation/src/Backend/`)
- **Frontend:** Vue.js 3 + TypeScript + Vuetify (`dhbw-automation/src/Frontend/`)
- **Database:** MariaDB 11.2 (EF Core with Pomelo)
- **Vector DB:** Qdrant (semantic search, embeddings)
- **Cache:** Redis
- **Storage:** MinIO (S3-compatible)
- **Message Queue:** RabbitMQ
- **AI Services:** OpenAI, Anthropic Claude, Google Gemini, Deepgram (STT)

## Build & Run Commands

### Start Docker Services
```bash
cd dhbw-automation
docker-compose up -d
```

### Backend (.NET 8)
```bash
cd dhbw-automation/src/Backend
dotnet restore
dotnet ef database update    # Apply EF Core migrations
dotnet run                   # Starts on http://localhost:5000
```

### Frontend (Vue.js)
```bash
cd dhbw-automation/src/Frontend
npm install
npm run dev                  # Starts on http://localhost:5173
npm run build                # Production build
npm run lint                 # ESLint with auto-fix
npm run format               # Prettier
```

### Testing
```bash
# Backend
cd dhbw-automation/src/Backend
dotnet test

# Frontend
cd dhbw-automation/src/Frontend
npm run test:unit            # Vitest
npm run test:e2e             # Playwright
```

### Database Migrations
```bash
cd dhbw-automation/src/Backend
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## Architecture

```
┌─────────────────────────────────────────────────┐
│         Frontend (Vue.js 3 + Vuetify)           │
└────────────────────┬────────────────────────────┘
                     │ REST API / WebSocket
┌────────────────────▼────────────────────────────┐
│           Backend (.NET 8 Web API)              │
│  API/Controllers → Core/Services → Infrastructure│
└────────────────────┬────────────────────────────┘
                     │
    ┌────────────────┼────────────────┐
    ↓                ↓                ↓
┌─────────┐   ┌──────────┐   ┌──────────┐
│ MariaDB │   │  Qdrant  │   │  MinIO   │
│  Redis  │   │ (Vector) │   │ RabbitMQ │
└─────────┘   └──────────┘   └──────────┘
```

### Backend Structure (`dhbw-automation/src/Backend/`)

- **API/Controllers/** - REST endpoints (28+ controllers)
- **Core/Services/** - Business logic (modular services, often split into partial classes)
- **Core/Models/** - Domain entities
- **Core/Interfaces/** - Service contracts
- **Infrastructure/Database/** - `AppDbContext.cs`, EF configurations, migrations
- **Infrastructure/ExternalAPIs/** - Anthropic, OpenAI, Google, Moodle, Deepgram integrations
- **Infrastructure/VectorDb/** - Qdrant operations
- **Infrastructure/Storage/** - MinIO file storage

### Frontend Structure (`dhbw-automation/src/Frontend/src/`)

- **views/** - Page components (routed)
- **components/** - Reusable Vue components
- **stores/** - Pinia state management
- **services/** - API client services
- **composables/** - Vue composition functions
- **types/** - TypeScript definitions

### Key Services

| Service | Purpose |
|---------|---------|
| `EmbeddingService` | Vector embeddings, semantic search (6 partial class files) |
| `LearningEngineService` | Adaptive questions, knowledge graphs (5 partial class files) |
| `InteractiveExerciseService` | Exercise generation & evaluation |
| `KnowledgeNetworkService` | Knowledge graph visualization |
| `MoodleSyncService` | Moodle course/resource sync |
| `FileService` | Document upload, parsing, AI analysis |
| `AIService` / `AiGatewayService` | Multi-model AI gateway |

## Code Patterns

### Large Services are Split into Partial Classes
When services grow large, they are refactored into multiple files:
```
Core/Services/Embedding/
├── EmbeddingService.cs              # Core logic
├── EmbeddingService.Processing.cs   # Processing methods
├── EmbeddingService.Search.cs       # Search operations
└── ...
```

### Adding New Features
- **New API endpoint:** Add controller in `API/Controllers/`, inject services
- **New service:** Add to `Core/Services/`, register in `Program.cs`
- **New entity:** Add to `Core/Models/`, add DbSet to `AppDbContext`, create migration
- **Background task:** Add to `Core/BackgroundServices/`

## Configuration

Environment variables in `.env` (see `.env.example`):
- Database connection, Redis, MinIO, RabbitMQ settings
- API keys: `OPENAI_API_KEY`, `ANTHROPIC_API_KEY`, `GEMINI_API_KEY`, `DEEPGRAM_API_KEY`
- Google OAuth: `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`
- Moodle: `MOODLE_BASE_URL`, `MOODLE_TOKEN`
- JWT settings: `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`

## Docker Services (docker-compose.yml)

| Service | Port | Purpose |
|---------|------|---------|
| mariadb | 3306 | Primary database |
| redis | 6379 | Caching |
| minio | 9000/9001 | File storage |
| rabbitmq | 5672/15672 | Message queue |
| qdrant | 6333/6334 | Vector database |
| phpmyadmin | 8080 | DB admin UI |

## Deployment

### Server
- **Host:** `192.168.178.198`
- **SSH:** `root@192.168.178.198` (SSH-Key Auth)
- **Project Path:** `/root/dhbw-automation-deploy/dhbw-automation`
- **Git Remote:** `server` (bare repo at `/root/git-repos/dhbw-automation.git`)

### Deploy via Git Push (Auto-Deployment)
```bash
git push server main
```
Post-receive hook automatically rebuilds and restarts containers.

### Manual Deployment Commands
```bash
# Build and deploy backend
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml build backend && docker compose -f docker-compose.prod.yml up -d backend"

# View logs
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs --tail=100 backend"

# Follow logs (real-time)
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f backend"

# Full rebuild
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml down && docker compose -f docker-compose.prod.yml build && docker compose -f docker-compose.prod.yml up -d"
```

### Production URLs
| Service | URL |
|---------|-----|
| Frontend | http://192.168.178.198 |
| Backend API | http://192.168.178.198:5000 |
| phpMyAdmin | http://192.168.178.198:8080 |
| MinIO Console | http://192.168.178.198:9001 |
| RabbitMQ Management | http://192.168.178.198:15672 |
| Qdrant Dashboard | http://192.168.178.198:6333/dashboard |

### EF Core Note
Use `.AsNoTracking()` in EF queries to prevent circular reference issues in JSON serialization.

## Mobile App (Flutter)

- **Development Location:** `C:\Projects\dhbw_mobile` (outside this repo)
- **In-Repo Version:** `dhbw-automation/mobile/` (veraltet, nicht aktuell)
- **Hinweis:** Flutter-Entwicklung erfolgt in `C:\Projects\dhbw_mobile`, da `flutter run` aus dem OneDrive-Pfad nicht funktioniert. Die Repo-Version ist nur eine Kopie und nicht synchron.

## Current Development Focus

See `.claude/LEARNING_ENGINE.md` for active task details:
- DeepTutor-style adaptive learning engine
- Knowledge graph extraction from documents
- Adaptive question generation (Bloom's taxonomy)
- User performance tracking with spaced repetition

## API Documentation

Swagger UI available at `http://localhost:5000/swagger` when backend is running.

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

## Current Development Focus

See `.claude/LEARNING_ENGINE.md` for active task details:
- DeepTutor-style adaptive learning engine
- Knowledge graph extraction from documents
- Adaptive question generation (Bloom's taxonomy)
- User performance tracking with spaced repetition

## API Documentation

Swagger UI available at `http://localhost:5000/swagger` when backend is running.

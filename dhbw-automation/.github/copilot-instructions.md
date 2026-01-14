# GitHub Copilot Instructions - DHBW Study Automation System

## System Architecture

**Stack**: .NET 8 Web API backend + Vue 3 (TypeScript/Vuetify) frontend + Docker infrastructure

**Backend Namespace**: All C# code uses `DHBWAutomation.Backend.<Layer>` namespace pattern:
- `API` - Controllers and middleware
- `Core` - Business logic (Services, Interfaces, Models)
- `Infrastructure` - Database (AppDbContext), VectorDb (Qdrant), ExternalAPIs
- `Shared` - Helpers and utilities

**Project Structure**:
```
src/Backend/
  ├── API/          # REST endpoints (Program.cs, Controllers/)
  ├── Core/         # Business logic (Services/, Interfaces/, Models/)
  ├── Infrastructure/  # Data access, Qdrant, external APIs
  └── Shared/       # Helpers (AnthropicClient, EncryptionHelper, RateLimiter)
  
src/Frontend/src/
  ├── components/   # Vue SFC components
  ├── views/        # Page components
  ├── stores/       # Pinia stores (auth.ts, mail.ts, validation.ts, etc.)
  ├── services/     # API client services
  └── types/        # TypeScript type definitions
```

## Critical Architectural Patterns

### AI Staging System
**All AI-extracted data goes through validation before production DB**. See [docs/AI_STAGING_SYSTEM.md](../docs/AI_STAGING_SYSTEM.md).

When AI extracts entities from documents:
1. Write to `StagedEntities` table (with confidence score)
2. If confidence < 90%, generate questions in `AIQuestions` table
3. User reviews/answers questions
4. Promote to production tables after validation

**Never** directly create production entities from AI results - always stage first.

### Multi-AI Support with Resilience
AI operations use `AnthropicClient` (in `Shared/Helpers/`) with built-in:
- Rate limiting (50 req/min for Tier 1)
- Retry with exponential backoff (3 attempts)
- Circuit breaker (opens after 5 failures for 1 min)

```csharp
// Get user-specific or fallback API key
var apiKey = await GetAnthropicApiKeyAsync(userId);
var response = await _anthropicClient.GenerateAsync(prompt, apiKey);
```

User API keys are stored **encrypted** in DB. Use `EncryptionHelper.Decrypt()` before use.

### Knowledge Graph & Learning Engine
Documents → Chunks → Embeddings → Qdrant vector DB → Entities/Relationships

The `LearningEngineService` orchestrates:
1. Document chunking (`ChunkingService`)
2. Embedding generation (`EmbeddingService`)
3. Vector storage (`QdrantService` → `dhbw_kg_entities` collection)
4. Entity extraction and relationship mapping
5. Adaptive question generation based on knowledge graph

Collections: `dhbw_kg_entities` (1536-dim vectors), others defined per feature.

### Database: MariaDB with Timestamped Migrations
Connection built dynamically from env vars: `DB_HOST`, `DB_PORT`, `DB_DATABASE`, `DB_USERNAME`, `DB_PASSWORD`

Migrations in `database/migrations/` follow `YYYYMMDD_<description>.sql` naming (e.g., `20260110_ai_staging_system.sql`).

Core tables: `users`, `Documents`, `CalendarEvents`, `todos`, `StagedEntities`, `AIQuestions`, `knowledge_base_items`, `moodle_*`.

## Development Workflows

### Local Development
```powershell
# Start infrastructure (MariaDB, Redis, MinIO, Qdrant, RabbitMQ)
docker-compose up -d

# Backend (.NET 8)
cd src/Backend
dotnet restore
dotnet ef database update    # Apply migrations (if using EF)
dotnet run                   # API runs on http://localhost:5000

# Frontend (Vue 3 + Vite)
cd src/Frontend
npm install
npm run dev                  # Dev server on http://localhost:5173
```

### Deployment Scripts (PowerShell)
Located in `scripts/`:
- `restart.ps1` - Quick restart (no rebuild)
- `rebuild.ps1` - Full rebuild with `--no-cache`
- `deploy.ps1 "commit message"` - Commit, push, and deploy to server
- `status.ps1` / `logs.ps1` - Check containers

### Commit Convention
Use [Conventional Commits](https://www.conventionalcommits.org/):
- `feat(scope): add feature`
- `fix(scope): fix bug`
- `docs(scope): update documentation`
- `refactor(scope): refactor code`

Scopes: `backend`, `frontend`, `ai`, `db`, `deploy`, `docker`

## Code Conventions

### C# Backend
- **File-scoped namespaces**: `namespace DHBWAutomation.Backend.Core.Models;` (not block syntax)
- **Interfaces**: All services have interfaces in `Core/Interfaces/I<ServiceName>.cs`
- **DI Registration**: All services registered in `API/Program.cs` (see lines 1-200)
- **Error Handling**: Use structured logging (`ILogger<T>`) and throw specific exceptions
- **Async/Await**: All I/O operations must be async

### Vue 3 Frontend
- **Composition API**: Use `<script setup lang="ts">` syntax
- **State Management**: Pinia stores in `stores/`, use `defineStore('name', () => {...})`
- **API Calls**: Import from `services/` (e.g., `import api from '@/services/api'`)
- **Types**: Define in `types/`, use explicit TypeScript types
- **Components**: Vuetify 3 components preferred

### Environment Configuration
Never hardcode secrets. Use `.env` file (root and `src/Frontend/.env`):
- Backend: Read via `Environment.GetEnvironmentVariable("KEY")`
- Frontend: Vite env vars prefixed with `VITE_` (e.g., `VITE_API_URL`)

`.env` is gitignored. Use `.env.example` as template.

## Key Integration Points

### Moodle Sync
Background service polls Moodle API. See `Core/Services/MoodleSync/`.
Tables: `moodle_courses`, `moodle_assignments`, `moodle_resources`, `moodle_calendar_events`.

### Email Processing
`EmailSyncBackgroundService` polls 3 mail accounts (DHBW, private, work).
Stores in `Emails` table, extracts attachments to `EmailAttachments`.

### Calendar Integration
Google Calendar sync via `GoogleCalendarService`. Tokens stored in `GoogleCalendarTokens`.
Events in `CalendarEvents` table with source tracking.

### External APIs
- Rapla: Course schedules (`Infrastructure/ExternalAPIs/Rapla/`)
- Nextcloud: File sync (`Infrastructure/ExternalAPIs/Nextcloud/`)
- HAFAS: Train schedules (`Core/Services/HafasService.cs`)

## Testing & Debugging

- Backend: Use Swagger UI at `http://localhost:5000/swagger`
- Frontend: Dev tools + Vue DevTools browser extension
- Logs: `docker logs <container>` or `.\scripts\logs.ps1`
- Database: Connect to MariaDB via `localhost:3306` with creds from `.env`

## Common Pitfalls

1. **CORS**: Frontend allowed origins configured in `Program.cs` (lines 40-65). Add new origins to `allowedOrigins` list.
2. **File Uploads**: Max size is 100MB (configured in `Program.cs`, FormOptions).
3. **Rate Limits**: AnthropicClient enforces 50 req/min. Don't bypass `_rateLimiter`.
4. **Encryption**: Always decrypt user API keys before use (`EncryptionHelper.Decrypt()`).
5. **Vector DB**: Ensure Qdrant collection exists before queries (`EnsureCollectionExistsAsync`).

## Documentation
- [README.md](../README.md) - Project overview
- [SETUP_GUIDE.md](../SETUP_GUIDE.md) - Detailed setup instructions
- [PROJECT_STRUCTURE.md](../PROJECT_STRUCTURE.md) - Full directory structure
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Contribution guidelines
- [docs/AI_STAGING_SYSTEM.md](../docs/AI_STAGING_SYSTEM.md) - AI validation workflow
- [docs/DHBW_AUTH_SYSTEM.md](../docs/DHBW_AUTH_SYSTEM.md) - Authentication details

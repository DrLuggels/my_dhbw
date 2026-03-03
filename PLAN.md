# DHBW Study Automation v2 - Implementierungsplan

## Context

Das alte DHBW-Automationssystem wurde analysiert und gelöscht. Es hatte fundamentale Architekturprobleme (3 parallele Lernsysteme, 35+ Services, inkonsistente APIs, kaputte Auth). Alle bewährten Algorithmen und Business-Regeln wurden in `NEUANFANG_ANALYSE.md` extrahiert.

Ziel: Ein sauberes, erweiterbares System mit klarer Architektur, das alle Features des alten Systems übernimmt - aber diesmal richtig.

**Kernentscheidungen:**
- Single-User App (kein Auth nötig)
- Python + FastAPI Backend
- PostgreSQL + pgvector (alles in einer DB)
- Vue.js 3 + Vuetify Frontend
- Chunking ist das Fundament - verschiedene Strategien pro Dokumenttyp
- Extensible by design

---

## Projektstruktur

```
my_dhbw/
├── backend/
│   ├── app/
│   │   ├── __init__.py
│   │   ├── main.py                          # FastAPI App + Lifespan
│   │   ├── config.py                        # Pydantic Settings
│   │   │
│   │   ├── api/                             # REST Endpoints
│   │   │   ├── __init__.py
│   │   │   ├── documents.py                 # Upload, List, Delete
│   │   │   ├── learning.py                  # Übungen, FSRS, Prioritäten
│   │   │   ├── knowledge.py                 # Entities, Beziehungen, Graph
│   │   │   ├── moodle.py                    # Moodle Sync
│   │   │   ├── calendar.py                  # Kalender Events
│   │   │   └── health.py                    # Health Check
│   │   │
│   │   ├── models/                          # SQLAlchemy ORM Models
│   │   │   ├── __init__.py
│   │   │   ├── base.py                      # Base Model + DB Setup
│   │   │   ├── document.py                  # Document + Chunk
│   │   │   ├── knowledge.py                 # Entity + Relationship
│   │   │   ├── learning.py                  # Exercise + Performance + Streak
│   │   │   ├── calendar.py                  # CalendarEvent
│   │   │   └── moodle.py                    # MoodleCourse + Assignment + Resource
│   │   │
│   │   ├── schemas/                         # Pydantic Request/Response
│   │   │   ├── __init__.py
│   │   │   ├── documents.py
│   │   │   ├── learning.py
│   │   │   ├── knowledge.py
│   │   │   └── common.py                    # Shared schemas (ApiResponse, etc.)
│   │   │
│   │   ├── services/                        # Business Logic
│   │   │   ├── __init__.py
│   │   │   ├── document_service.py          # Upload + Pipeline orchestration
│   │   │   ├── chunking/                    # Chunking-Strategien
│   │   │   │   ├── __init__.py
│   │   │   │   ├── base.py                  # Abstract ChunkingStrategy
│   │   │   │   ├── detector.py              # DocumentType-Erkennung
│   │   │   │   ├── pdf_chunker.py           # PDF (Skript vs. Folien-Export)
│   │   │   │   ├── pptx_chunker.py          # PowerPoint
│   │   │   │   ├── docx_chunker.py          # Word
│   │   │   │   └── html_chunker.py          # HTML/Moodle
│   │   │   ├── embedding_service.py         # Embedding-Generierung
│   │   │   ├── knowledge_service.py         # Entity-Extraktion + Graph
│   │   │   ├── learning_service.py          # FSRS + Bloom + Übungen
│   │   │   ├── ai_service.py                # Multi-Model AI Gateway
│   │   │   ├── moodle_service.py            # Moodle API Client + Sync
│   │   │   └── calendar_service.py          # Kalender-Aggregation
│   │   │
│   │   └── utils/                           # Hilfsfunktionen
│   │       ├── __init__.py
│   │       └── text.py                      # Text-Bereinigung, Token-Zählung
│   │
│   ├── migrations/                          # Alembic Migrations
│   │   ├── env.py
│   │   └── versions/
│   ├── uploads/                             # Hochgeladene Dateien
│   ├── pyproject.toml                       # Dependencies
│   ├── .env.example
│   └── Dockerfile
│
├── frontend/
│   ├── src/
│   │   ├── App.vue
│   │   ├── main.ts
│   │   ├── router.ts                        # Vue Router
│   │   │
│   │   ├── api/                             # API Client (nach Feature getrennt)
│   │   │   ├── client.ts                    # Axios Instance
│   │   │   ├── documents.ts
│   │   │   ├── learning.ts
│   │   │   ├── knowledge.ts
│   │   │   ├── moodle.ts
│   │   │   └── calendar.ts
│   │   │
│   │   ├── stores/                          # Pinia Stores
│   │   │   ├── documents.ts
│   │   │   ├── learning.ts
│   │   │   └── app.ts                       # Global App State
│   │   │
│   │   ├── views/                           # Seiten
│   │   │   ├── DashboardView.vue
│   │   │   ├── DocumentsView.vue
│   │   │   ├── LearningView.vue
│   │   │   ├── KnowledgeGraphView.vue
│   │   │   ├── CalendarView.vue
│   │   │   └── SettingsView.vue
│   │   │
│   │   ├── components/                      # Komponenten
│   │   │   ├── layout/
│   │   │   │   ├── AppLayout.vue
│   │   │   │   ├── AppNav.vue
│   │   │   │   └── AppBar.vue
│   │   │   ├── documents/
│   │   │   │   ├── DocumentUpload.vue
│   │   │   │   ├── DocumentList.vue
│   │   │   │   └── DocumentDetail.vue
│   │   │   ├── learning/
│   │   │   │   ├── ExercisePlayer.vue
│   │   │   │   ├── MultipleChoice.vue
│   │   │   │   ├── FillInBlank.vue
│   │   │   │   ├── FreeText.vue
│   │   │   │   ├── StreakWidget.vue
│   │   │   │   └── StatsCards.vue
│   │   │   ├── knowledge/
│   │   │   │   ├── KnowledgeGraph.vue
│   │   │   │   └── EntityDetail.vue
│   │   │   └── common/
│   │   │       └── LoadingState.vue
│   │   │
│   │   └── types/                           # TypeScript Types
│   │       ├── documents.ts
│   │       ├── learning.ts
│   │       ├── knowledge.ts
│   │       └── api.ts
│   │
│   ├── package.json
│   ├── tsconfig.json
│   ├── vite.config.ts
│   └── Dockerfile
│
├── docker-compose.yml                       # PostgreSQL + pgvector
├── .env.example
├── CLAUDE.md                                # Neue Projektanleitung
├── NEUANFANG_ANALYSE.md                     # Referenz altes System
└── .claude/                                 # Claude Code Config + Plugins
```

---

## Design & Code Codex

### Goldene Regeln

1. **Max ~200 Zeilen pro Datei** (weiche Grenze) - wenn eine Datei wächst, wird sie aufgeteilt
2. **Jede Datei hat EINE Verantwortung** - kein Mischen von Concerns
3. **Konsistenz > Kreativität** - gleiche Patterns überall wiederverwenden
4. **Kein `any` in TypeScript** - `unknown` verwenden wenn nötig
5. **Keine Magic Numbers** - alles in Konstanten/Config
6. **Explizite Fehlerbehandlung** - keine stummen `catch {}`

---

### Backend Code Codex (Python)

**Datei-Organisation:**
```
api/documents.py      → NUR Route-Handler, max 150 Zeilen
services/document.py  → NUR Business-Logik, max 200 Zeilen
models/document.py    → NUR ORM-Models, max 100 Zeilen
schemas/documents.py  → NUR Pydantic-Schemas, max 100 Zeilen
```

**Naming:**
```python
# Dateien:        snake_case.py
# Klassen:        PascalCase
# Funktionen:     snake_case
# Konstanten:     SCREAMING_SNAKE_CASE
# Private:        _prefixed

# RICHTIG:
class DocumentService:
    async def process_document(self, doc_id: int) -> ProcessingResult: ...

# FALSCH:
class docService:
    async def processDoc(self, id): ...
```

**API-Endpoint-Muster (jeder Endpoint sieht gleich aus):**
```python
@router.post("/upload", response_model=ApiResponse[DocumentOut])
async def upload_document(
    file: UploadFile,
    db: AsyncSession = Depends(get_db),
) -> ApiResponse[DocumentOut]:
    """Upload und verarbeite ein Dokument."""
    result = await document_service.upload(db, file)
    return ApiResponse(data=result, message="Dokument hochgeladen")
```

**Einheitliche API-Response (IMMER):**
```python
class ApiResponse(BaseModel, Generic[T]):
    success: bool = True
    data: T | None = None
    message: str = ""
    errors: list[str] = []
```

**Service-Pattern:**
```python
# Services sind stateless Klassen oder Funktionen
# Dependency Injection über Funktionsparameter
# Keine globalen Variablen

async def get_document(db: AsyncSession, doc_id: int) -> Document | None:
    result = await db.execute(select(Document).where(Document.id == doc_id))
    return result.scalar_one_or_none()
```

**Docstrings (Google-Style, nur bei nicht-offensichtlichen Funktionen):**
```python
async def detect_document_type(pages: list[PageContent]) -> DocCategory:
    """Analysiert Seitenstruktur um den Dokumenttyp zu erkennen.

    Args:
        pages: Erste 3 Seiten des Dokuments mit Text und Metadaten.

    Returns:
        Erkannter Dokumenttyp (slides_export, textbook, exercise_sheet).
    """
```

**Import-Reihenfolge:**
```python
# 1. Standard Library
from datetime import datetime
from typing import Any

# 2. Third Party
from fastapi import APIRouter, Depends
from sqlalchemy import select

# 3. Local
from app.models.document import Document
from app.schemas.documents import DocumentOut
```

---

### Frontend Code Codex (Vue.js + TypeScript)

**Datei-Organisation:**
```
views/DocumentsView.vue      → Seiten-Layout, ruft Komponenten auf, max 150 Zeilen
components/documents/         → Feature-Komponenten, je max 200 Zeilen
api/documents.ts              → API-Calls, max 80 Zeilen
stores/documents.ts           → State, max 120 Zeilen
types/documents.ts            → Interfaces & Types, max 80 Zeilen
```

**Vue-Komponenten-Aufbau (immer gleiche Reihenfolge):**
```vue
<script setup lang="ts">
// 1. Imports
// 2. Props & Emits
// 3. Stores & Composables
// 4. Reactive State
// 5. Computed
// 6. Methods
// 7. Lifecycle Hooks
</script>

<template>
  <!-- Maximal 2 Ebenen Verschachtelung im Template -->
</template>

<style scoped>
/* Nur wenn Vuetify-Overrides nötig */
</style>
```

**Komponenten-Regeln:**
```
- Props definieren mit defineProps<{ }>() (TypeScript generics)
- Events definieren mit defineEmits<{ }>()
- KEINE direkten API-Calls in Komponenten → immer über Store oder Composable
- KEINE Business-Logik in Templates → in Computed oder Methods
- Jede Komponente hat EINEN klaren Zweck
```

**API-Client-Muster (jeder API-Call sieht gleich aus):**
```typescript
// api/documents.ts
import { client } from './client'
import type { ApiResponse, Document, DocumentUpload } from '@/types'

export const documentsApi = {
  list: () =>
    client.get<ApiResponse<Document[]>>('/documents'),

  get: (id: number) =>
    client.get<ApiResponse<Document>>(`/documents/${id}`),

  upload: (data: FormData) =>
    client.post<ApiResponse<Document>>('/documents/upload', data),

  delete: (id: number) =>
    client.delete<ApiResponse<void>>(`/documents/${id}`),
}
```

**Store-Muster (Pinia, immer gleiche Struktur):**
```typescript
// stores/documents.ts
export const useDocumentStore = defineStore('documents', () => {
  // State
  const documents = ref<Document[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  // Getters (computed)
  const processedDocs = computed(() =>
    documents.value.filter(d => d.processing_status === 'done')
  )

  // Actions
  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      const { data } = await documentsApi.list()
      documents.value = data.data ?? []
    } catch (e) {
      error.value = 'Fehler beim Laden'
    } finally {
      loading.value = false
    }
  }

  return { documents, loading, error, processedDocs, fetchAll }
})
```

**TypeScript-Regeln:**
```typescript
// IMMER interfaces für Objekte
interface Document {
  id: number
  title: string
  processing_status: ProcessingStatus
}

// IMMER enums/unions für feste Werte
type ProcessingStatus = 'pending' | 'processing' | 'done' | 'error'
type DocCategory = 'slides_export' | 'textbook' | 'exercise_sheet'

// NIEMALS any - unknown verwenden
function parseResponse(data: unknown): Document { ... }

// IMMER generische ApiResponse
interface ApiResponse<T> {
  success: boolean
  data: T | null
  message: string
  errors: string[]
}
```

---

### Design System (Vuetify Theming)

**Farbpalette (konsistent überall):**
```typescript
// Ein Theme, überall verwendet
const dhbwTheme = {
  dark: false,
  colors: {
    primary:    '#1565C0',   // Blau - Hauptaktionen, Navigation
    secondary:  '#37474F',   // Dunkelgrau - Sekundäre Elemente
    accent:     '#00897B',   // Teal - Highlights, Erfolg
    background: '#FAFAFA',   // Hellgrau - Seitenhintergrund
    surface:    '#FFFFFF',   // Weiß - Karten, Dialoge
    error:      '#D32F2F',   // Rot - Fehler
    warning:    '#F57C00',   // Orange - Warnungen
    info:       '#1976D2',   // Blau - Info
    success:    '#388E3C',   // Grün - Erfolg

    // Semantische Lern-Farben
    'mastery-low':    '#EF5350',  // Rot - <40% Mastery
    'mastery-mid':    '#FFA726',  // Orange - 40-70%
    'mastery-high':   '#66BB6A',  // Grün - >70%
    'bloom-1':        '#90CAF9',  // Hellblau - Erinnern
    'bloom-2':        '#64B5F6',  // Blau - Verstehen
    'bloom-3':        '#42A5F5',  // Kräftiger - Anwenden
    'bloom-4':        '#1E88E5',  // Tiefblau - Analysieren
    'bloom-5':        '#1565C0',  // Dunkelblau - Bewerten
    'bloom-6':        '#0D47A1',  // Sehr dunkel - Erschaffen
  }
}
```

**Layout-Regeln:**
```
- Sidebar: 260px breit, fixiert, primary-Farbe Header
- Content: max-width 1200px, zentriert, 24px Padding
- Karten: v-card mit elevation="1", rounded="lg"
- Spacing: Immer Vielfache von 4 (mt-4, pa-6, gap-4)
- Schrift: Roboto (Vuetify Default), max 3 Größen pro Seite
```

**Komponenten-Konsistenz:**
```
Seiten-Header:     <v-toolbar flat color="transparent">
                     <v-toolbar-title>Seitenname</v-toolbar-title>
                     <v-spacer />
                     <v-btn>Hauptaktion</v-btn>
                   </v-toolbar>

Daten-Karte:       <v-card elevation="1" rounded="lg" class="pa-4">
                     <div class="text-subtitle-2 text-medium-emphasis">Label</div>
                     <div class="text-h5 font-weight-bold">Wert</div>
                   </v-card>

Lade-Zustand:      <v-skeleton-loader> (nie leere Seiten)

Leerer Zustand:    <v-empty-state> mit Icon + Text + Action-Button

Fehler-Zustand:    <v-alert type="error" variant="tonal">

Erfolg-Feedback:   v-snackbar (unten rechts, 3 Sekunden)

Listen:            <v-list> mit <v-list-item> (nie eigene Listen)

Formulare:         <v-form> mit <v-text-field> (immer variant="outlined")

Buttons:           Primär:   <v-btn color="primary" variant="elevated">
                   Sekundär: <v-btn color="primary" variant="outlined">
                   Danger:   <v-btn color="error" variant="tonal">
                   Icon:     <v-btn icon variant="text">
```

**View-Aufbau (jede Seite hat die gleiche Struktur):**
```vue
<template>
  <div>
    <!-- 1. Page Header -->
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Seitenname</v-toolbar-title>
      <v-spacer />
      <v-btn color="primary">Aktion</v-btn>
    </v-toolbar>

    <!-- 2. Content -->
    <v-container fluid class="pa-6">
      <!-- Loading -->
      <template v-if="loading">
        <v-skeleton-loader type="card" />
      </template>

      <!-- Error -->
      <v-alert v-else-if="error" type="error" variant="tonal">
        {{ error }}
      </v-alert>

      <!-- Empty -->
      <v-empty-state v-else-if="!items.length"
        icon="mdi-file-outline"
        title="Noch keine Einträge"
        text="Erstelle deinen ersten Eintrag"
      />

      <!-- Data -->
      <template v-else>
        <!-- Feature-spezifischer Content -->
      </template>
    </v-container>
  </div>
</template>
```

---

### Erweiterbarkeit

**Neues Feature hinzufügen (Checkliste):**
```
Backend:
□ models/feature.py        → ORM Model (max 100 Zeilen)
□ schemas/feature.py       → Pydantic Schemas (max 80 Zeilen)
□ services/feature_service.py → Business Logic (max 200 Zeilen)
□ api/feature.py           → Endpoints (max 150 Zeilen)
□ Router in main.py registrieren

Frontend:
□ types/feature.ts         → TypeScript Interfaces
□ api/feature.ts           → API Client Funktionen
□ stores/feature.ts        → Pinia Store (optional)
□ views/FeatureView.vue    → Seite
□ components/feature/      → Komponenten
□ Route in router.ts registrieren
```

**Neuen Chunker hinzufügen:**
```
□ services/chunking/xyz_chunker.py  → implements ChunkingStrategy
□ detector.py aktualisieren         → neuen Typ erkennen
□ Fertig - alles andere passiert automatisch
```

---

## Phase 1: Foundation (Backend-Kern)

### Schritt 1.1: Projekt-Scaffolding
- `pyproject.toml` mit allen Dependencies
- `.env.example` mit allen Konfigurationen
- `docker-compose.yml` (PostgreSQL 16 + pgvector)
- `backend/app/config.py` (Pydantic Settings)
- `CLAUDE.md` (neue Projektanleitung)

**Dependencies (pyproject.toml):**
```
fastapi, uvicorn[standard], pydantic-settings
sqlalchemy[asyncio], asyncpg, alembic
pgvector  (SQLAlchemy pgvector support)
python-multipart (File Upload)
pymupdf (PDF - besser als PyPDF2)
python-pptx (PowerPoint)
python-docx (Word)
beautifulsoup4, lxml (HTML)
openai (Embeddings + GPT)
anthropic (Claude für Entity-Extraktion)
google-generativeai (Gemini Vision)
httpx (async HTTP für Moodle API)
tiktoken (Token-Zählung)
```

### Schritt 1.2: Datenbank-Setup
- `backend/app/models/base.py` - SQLAlchemy async Engine + Base
- pgvector Extension aktivieren
- Alembic init + erste Migration

**Kern-Tabellen:**
```sql
-- Dokumente
documents (id, title, filename, filepath, filetype, filesize,
           doc_category, processing_status, metadata_json,
           created_at, updated_at)

-- Chunks (mit pgvector Embedding)
chunks (id, document_id FK, content, chunk_index,
        chunk_type, topic_label, section_heading,
        page_number, metadata_json,
        embedding vector(1536),
        created_at)

-- Knowledge Entities
entities (id, name, description, entity_type,
          subject, topic, subtopic,
          importance, confidence,
          source_document_id FK, source_chunk_id FK,
          mastery_score, bloom_level, next_review,
          fsrs_state, fsrs_stability, fsrs_difficulty,
          total_attempts, correct_attempts,
          easy_total, easy_correct,
          medium_total, medium_correct,
          hard_total, hard_correct,
          last_interaction, decay_rate,
          created_at, updated_at)

-- Knowledge Relationships
relationships (id, source_entity_id FK, target_entity_id FK,
               relationship_type, strength, evidence,
               confidence, is_prerequisite,
               prerequisite_strictness,
               created_at)

-- Exercises
exercises (id, entity_id FK, question, correct_answer,
           explanation, exercise_type, difficulty,
           bloom_level, options_json,
           is_answered, is_correct, user_answer, score,
           next_review, fsrs_state,
           source_chunk_id FK,
           created_at, answered_at)

-- Calendar Events
calendar_events (id, title, description,
                 start_time, end_time, all_day,
                 event_type, source, external_id,
                 subject, location,
                 created_at, updated_at)

-- Moodle
moodle_courses (id, moodle_id, shortname, fullname,
                summary, start_date, end_date, last_synced)

moodle_assignments (id, course_id FK, moodle_id,
                    name, description, due_date, status)

moodle_resources (id, course_id FK, moodle_id,
                  name, resource_type, url, file_size,
                  is_downloaded, document_id FK,
                  last_modified)

-- Learning Streak
learning_streak (id, current_streak, longest_streak,
                 last_activity_date, total_active_days)

-- Learning Priorities (berechnet, kein FK)
learning_priorities (id, entity_id FK, composite_score,
                     deadline_urgency, topic_relevance,
                     mastery_gap, decay_amount, bloom_gap,
                     is_blocked, block_reason,
                     calculated_at)
```

### Schritt 1.3: FastAPI App + Health
- `backend/app/main.py` - App mit Lifespan (DB init/close)
- `backend/app/api/health.py` - Health Endpoint
- CORS konfiguriert für Frontend (localhost:5173)
- Einheitliche API-Response Struktur: `{ data, message, success }`

---

## Phase 2: Chunking-Pipeline (das Herzstück)

### Schritt 2.1: Document Type Detector
- `backend/app/services/chunking/detector.py`
- Analysiert erste Seiten eines PDF: Wort-Dichte, Bullet-Points, Struktur
- Klassifiziert: `slides_export` | `textbook` | `exercise_sheet` | `paper` | `scan`
- PPTX/DOCX/HTML haben feste Typen

### Schritt 2.2: Chunking-Strategien (Strategy Pattern)
- `backend/app/services/chunking/base.py` - Abstract Base `ChunkingStrategy`

```python
class ChunkingStrategy(ABC):
    @abstractmethod
    async def chunk(self, content: Any, metadata: dict) -> list[ChunkResult]:
        """Returns list of chunks with metadata."""
        pass
```

- **`pdf_chunker.py`**:
  - `SlidesExportChunker` - 1 Seite = 1 Slide, AI-Gruppierung verwandter Slides
  - `TextbookChunker` - Heading-basiert, 500-800 Token pro Chunk
  - `ExerciseChunker` - Aufgaben-Nummern erkennen, 1 Aufgabe = 1 Chunk
  - Context-Overlap: Letzter vollständiger Absatz + Topic-Label des vorherigen Chunks

- **`pptx_chunker.py`**:
  - Slide-Extraktion mit python-pptx (Titel, Bullets, Notes)
  - Bilder → Gemini Vision für Beschreibung
  - AI-basierte Themen-Gruppierung (3-5 Folien pro Chunk)
  - Context: Foliennummer-Range + Vorlesungstitel

- **`docx_chunker.py`**:
  - Heading-Hierarchie auswerten (H1, H2, H3)
  - Split an Headings, max 800 Token
  - Tabellen als strukturierter Text

- **`html_chunker.py`**:
  - BeautifulSoup DOM-Parsing
  - Heading-basiertes Splitting
  - Code-Blöcke als eigene Chunks

### Schritt 2.3: Chunk-Anreicherung
- Jeder Chunk bekommt:
  - `chunk_type`: definition, example, exercise, theory, overview, formula
  - `topic_label`: AI-generiertes Thema (Claude, günstig mit Haiku)
  - `section_heading`: Aus Dokument-Struktur
  - `page_number`: Seitenzahl/Foliennummer
  - `context_prefix`: Zusammenfassung des vorherigen Chunks (für Overlap)

### Schritt 2.4: Embedding-Service
- `backend/app/services/embedding_service.py`
- OpenAI text-embedding-3-small (1536 Dimensionen)
- Batch-Embedding mit Rate-Limiting
- Speicherung direkt in `chunks.embedding` (pgvector column)

### Schritt 2.5: Document-Upload API
- `backend/app/api/documents.py`
  - `POST /api/documents/upload` - Upload + async Processing
  - `GET /api/documents` - Liste aller Dokumente
  - `GET /api/documents/{id}` - Dokument mit Chunks
  - `DELETE /api/documents/{id}` - Löschen (mit Chunks + Embeddings)
  - `GET /api/documents/{id}/chunks` - Chunks eines Dokuments
  - `POST /api/documents/{id}/reprocess` - Neu verarbeiten

---

## Phase 3: Knowledge Engine

### Schritt 3.1: Entity-Extraktion
- `backend/app/services/knowledge_service.py`
- Claude Sonnet extrahiert Entities aus Chunks
- 12 Entity-Typen, 13 Beziehungstypen (aus NEUANFANG_ANALYSE.md)
- Batch-Verarbeitung: Alle Chunks eines Dokuments → Entities

### Schritt 3.2: Auto-Linking
- Cosine-Similarity zwischen Chunk-Embeddings (pgvector)
- Threshold 0.8 für automatische Verknüpfung
- Beziehungsstärke aus Ähnlichkeit ableiten

### Schritt 3.3: Knowledge API
- `backend/app/api/knowledge.py`
  - `GET /api/knowledge/entities` - Alle Entities (Filter: subject, type)
  - `GET /api/knowledge/entities/{id}` - Entity mit Beziehungen
  - `GET /api/knowledge/graph` - Graph-Daten für Visualisierung
  - `POST /api/knowledge/search` - Semantische Suche
  - `GET /api/knowledge/weak-areas` - Schwachstellen

---

## Phase 4: Adaptives Lernsystem

### Schritt 4.1: FSRS Engine
- `backend/app/services/learning_service.py`
- FSRS mit W0-W16 Parametern (aus NEUANFANG_ANALYSE.md)
- State Machine: New → Learning → Review → Relearning
- Exponentieller Verfall (5%/Tag, Min 40%)

### Schritt 4.2: Übungsgenerierung
- Claude generiert Übungen aus Entities + zugehörigen Chunks
- 3 Übungstypen initial: Multiple Choice, Lückentext, Freitext
- Bloom's Taxonomy Level 1-6 Mapping
- 20/40/40 Schwierigkeitsverteilung

### Schritt 4.3: Prioritäts-Engine
- Multi-Faktor Score: Deadline + Relevanz + Mastery-Gap + Verfall + Bloom
- Voraussetzungsketten (soft/hard blocking)
- Streak-Multiplikator

### Schritt 4.4: Learning API
- `backend/app/api/learning.py`
  - `GET /api/learning/next` - Nächste empfohlene Übung
  - `POST /api/learning/exercise` - Übung generieren
  - `POST /api/learning/exercise/{id}/answer` - Antwort abgeben
  - `GET /api/learning/session` - Lernsession (N Übungen)
  - `GET /api/learning/stats` - Mastery-Statistiken
  - `GET /api/learning/streak` - Streak-Info
  - `GET /api/learning/priorities` - Lern-Prioritäten
  - `GET /api/learning/due` - Fällige Übungen

---

## Phase 5: Moodle-Integration

### Schritt 5.1: Moodle API Client
- `backend/app/services/moodle_service.py`
- Token-basierte Auth
- Kurse, Aufgaben, Ressourcen, Kalender synchronisieren
- Auto-Download von Ressourcen → Document-Pipeline

### Schritt 5.2: Moodle API
- `backend/app/api/moodle.py`
  - `POST /api/moodle/connect` - Token setzen
  - `POST /api/moodle/sync` - Sync auslösen
  - `GET /api/moodle/courses` - Kurse
  - `GET /api/moodle/assignments` - Aufgaben mit Deadlines

---

## Phase 6: Frontend

### Schritt 6.1: Projekt-Setup
- Vue.js 3 + TypeScript + Vite
- Vuetify 3 (Material Design)
- Pinia (State Management)
- Vue Router

### Schritt 6.2: Layout & Navigation
- `AppLayout.vue` - Sidebar + Content Area
- Navigation: Dashboard, Dokumente, Lernen, Wissensgraph, Kalender, Einstellungen

### Schritt 6.3: Dashboard
- Übersicht: Streak, fällige Übungen, Mastery-Stats
- Quick Actions: Upload, Lernstart, Moodle Sync

### Schritt 6.4: Dokument-Management
- Upload (Drag & Drop, Multi-File)
- Liste mit Kategorie-Filter
- Detail-Ansicht mit Chunks

### Schritt 6.5: Lern-Interface
- ExercisePlayer mit dynamischen Komponenten
- MC, Lückentext, Freitext
- Sofortiges Feedback + Erklärung
- Streak-Widget, Difficulty-Chart

### Schritt 6.6: Knowledge Graph
- vis-network für interaktiven Graph
- Entity-Detail Panel
- Semantische Suche

---

## Phase 7: Docker & Deployment

### Schritt 7.1: Docker Setup
- `docker-compose.yml`: PostgreSQL 16 + pgvector Extension
- Backend Dockerfile (Python 3.12)
- Frontend Dockerfile (Node 20 + Nginx)

### Schritt 7.2: Produktions-Deployment
- `docker-compose.prod.yml` für Server (192.168.178.198)
- Nginx Reverse Proxy
- Auto-Deploy via git push

---

## Implementierungsreihenfolge

```
Phase 1: Foundation     → Backend-Gerüst, DB, Config
Phase 2: Chunking       → Das Herzstück, alle 4 Dokumenttypen
Phase 3: Knowledge      → Entity-Extraktion, Graph
Phase 4: Learning       → FSRS, Bloom, Übungen
Phase 5: Moodle         → Sync, Auto-Download
Phase 6: Frontend       → Vue.js UI
Phase 7: Deployment     → Docker, Server
```

Jede Phase ist eigenständig testbar. Phase 2 (Chunking) bekommt die meiste Aufmerksamkeit.

---

## Verifizierung

- **Phase 1:** `curl http://localhost:8000/health` → `{ "status": "ok" }`
- **Phase 2:** Upload PDF/PPTX → Chunks in DB prüfen, Embeddings vorhanden
- **Phase 3:** Entities aus Chunks extrahiert, Graph-API liefert Daten
- **Phase 4:** Übung generieren → Antwort abgeben → FSRS-Update prüfen
- **Phase 5:** Moodle-Sync → Kurse + Ressourcen in DB
- **Phase 6:** Frontend zeigt alle Daten, Upload funktioniert
- **Phase 7:** `git push server main` → App läuft auf 192.168.178.198

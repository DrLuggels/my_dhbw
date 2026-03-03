# Implementierungs-Prompt

Kopiere alles unterhalb dieser Linie nach einem `/clear`:

---

Wir bauen das DHBW Study Automation System v2 komplett neu auf. Alle Entscheidungen sind getroffen, der Plan steht.

**Lies zuerst diese 2 Dateien:**
1. `PLAN.md` - Vollständiger Implementierungsplan mit Design & Code Codex
2. `NEUANFANG_ANALYSE.md` - Alle Algorithmen, Schemas und Business-Regeln aus dem alten System

**Kontext (damit du nicht suchen musst):**
- Single-User App (kein Auth/JWT nötig)
- Backend: Python + FastAPI
- Frontend: Vue.js 3 + Vuetify + TypeScript
- DB: PostgreSQL + pgvector (alles in einer DB)
- AI: OpenAI (Embeddings), Anthropic Claude (Entity-Extraktion, Übungen), Google Gemini (Vision/OCR)
- Chunking ist das Herzstück - verschiedene Strategien pro Dokumenttyp (PDF, PPTX, DOCX, HTML)
- PDF kann Folien-Export ODER Skript/Lehrbuch sein - muss erkannt werden
- Semantische Gruppierung: AI gruppiert zusammengehörige Folien/Abschnitte
- Intelligenter Overlap: Context-Header statt roher Text-Kopie
- Algorithmen aus dem alten System beibehalten: FSRS (W0-W16), Bloom 1-6, 20/40/40 Verteilung, Exponentieller Verfall (5%/Tag)
- Deployment: Server 192.168.178.198 via `git push server main`
- Plugins/Agenten liegen in `.claude/plugins/` als Referenz

**Code Codex Kurzfassung:**
- Max ~200 Zeilen pro Datei
- Backend: snake_case, Google-Style Docstrings, einheitliche `ApiResponse<T>`
- Frontend: Composition API (`<script setup lang="ts">`), Props via `defineProps<{}>()`, API-Calls NUR über `api/*.ts`, State NUR über Pinia Stores
- Jede View hat die gleiche Struktur: Header → Loading → Error → Empty → Data
- Vuetify: elevation="1", rounded="lg", variant="outlined" bei Inputs, Spacing in 4er-Schritten
- Farbpalette: primary=#1565C0, accent=#00897B, background=#FAFAFA, surface=#FFFFFF
- Mastery-Farben: rot(<40%), orange(40-70%), grün(>70%)
- Bloom-Farben: Hellblau(1) → Dunkelblau(6) Gradient

**Starte mit Phase 1 (Foundation):** Backend-Gerüst, Docker, DB, Config. Dann frage mich ob du weitermachen sollst mit Phase 2 (Chunking).

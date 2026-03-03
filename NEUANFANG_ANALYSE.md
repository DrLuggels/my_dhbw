# DHBW Automation - Vollständige Wissenschaftliche Analyse für Neuanfang

> **Datum:** 2026-03-03
> **Zweck:** Extraktion aller relevanten Informationen aus dem bestehenden System als Grundlage für einen vollständigen Neuanfang

---

## Inhaltsverzeichnis

1. [Executive Summary](#1-executive-summary)
2. [Kritische Probleme des aktuellen Systems](#2-kritische-probleme)
3. [Domain-Modell: Was das System tut](#3-domain-modell)
4. [Alle Features & Funktionen](#4-features)
5. [Algorithmen & Business-Regeln](#5-algorithmen)
6. [API-Endpoints (vollständig)](#6-api-endpoints)
7. [Datenbank-Schema](#7-datenbank-schema)
8. [Externe Integrationen](#8-integrationen)
9. [Frontend-Architektur](#9-frontend)
10. [Infrastruktur & Deployment](#10-infrastruktur)
11. [Dependency-Inventar](#11-dependencies)
12. [Empfehlungen für den Neuanfang](#12-empfehlungen)

---

## 1. Executive Summary

### Was ist DHBW Automation?

Eine **KI-gestützte Lernplattform** für DHBW-Studenten mit:
- Automatischer Dokumentenverarbeitung (PDF → Knowledge Graph → Übungen)
- Adaptivem Lernsystem (Spaced Repetition + Bloom's Taxonomy)
- Moodle-Integration (Kurse, Aufgaben, Ressourcen, Kalender)
- Multi-AI-Gateway (OpenAI, Anthropic, Google Gemini, Deepgram)
- Kalender-Synchronisation (Google, Rapla, Moodle)
- Todo-/Aufgabenverwaltung
- E-Mail-Synchronisation

### Warum Neuanfang?

Das aktuelle System hat **fundamentale architektonische Probleme**:

| Problem | Schwere | Beschreibung |
|---------|---------|--------------|
| **3 parallele Lernsysteme** | Kritisch | AKGLS, LearningEngine, OmniLearning - alle koexistieren ohne klare Zuständigkeit |
| **Hardcoded userId=1** | Kritisch | Kein Multi-User-Support in vielen Controllern |
| **35+ Services ohne klare Grenzen** | Hoch | Massive Service-Proliferation, überlappende Verantwortlichkeiten |
| **Inkonsistente API-Responses** | Hoch | Mix aus anonymen Objekten, ApiResponse<T>, rohen Daten |
| **Partial-Class-Overuse** | Hoch | Services über 5-9 Dateien verteilt statt sauberer Composition |
| **Type-Duplikation (Frontend)** | Mittel | 70% Überlappung zwischen omniLearning.ts und learningEngine.ts |
| **Monolithischer api.ts** | Mittel | 100+ Methoden in einer Datei |
| **Kein Repository-Pattern** | Mittel | Direkte DbContext-Zugriffe überall |
| **Fehlende Tests** | Hoch | Kaum Unit-/Integrationstests vorhanden |
| **N+1 Query-Probleme** | Mittel | Keine optimierten DB-Abfragen |

---

## 2. Kritische Probleme des aktuellen Systems

### 2.1 Architektur-Chaos: 3 Lernsysteme parallel

```
AKGLS (Adaptive Knowledge Graph Learning System)
├── UserKnowledgeNode / UserKnowledgeEdge
├── LearningPriority / UserDecayProfile
├── PrerequisiteChain / LearningStreak
└── PersonalKnowledgeGraphService

LearningEngine (DeepTutor-style)
├── KgEntity / KgRelationship
├── UserEntityPerformance
└── LearningEngineService (5 Partial Classes)

OmniLearning (Consolidation-Versuch)
├── UnifiedKnowledgeEntity / UnifiedKnowledgeRelationship
├── UnifiedLearningPriority
└── OmniLearningEngineService (9+ Partial Classes)
```

**Problem:** Alle drei existieren gleichzeitig. Daten sind nicht synchron. Jedes System hat eigene Entities, eigene Algorithmen, eigene Endpoints.

### 2.2 Backend: Inkonsistente Patterns

- **Controller** mischen direkte DB-Zugriffe mit Service-Aufrufen
- **Responses** sind mal `{ success, data, message }`, mal `Ok(data)`, mal `BadRequest(new { ... })`
- **JWT** nicht vollständig implementiert (`GetCurrentUser()` returniert "noch nicht implementiert")
- **Rate Limiting** ist statisch und thread-unsafe
- **Auto-Migration** beim Start (gefährlich in Produktion)

### 2.3 Frontend: Gewachsene Komplexität

- `api.ts` mit 100+ Methoden in einer Datei
- Manche Views rufen API direkt auf, andere nutzen Stores
- Keine Form-Validierung (kein VeeValidate o.ä.)
- Keine Internationalisierung (alles hardcoded Deutsch)
- Keine Error Boundaries
- Keine Tests vorhanden

### 2.4 Infrastruktur: Overengineered

- 6 Docker-Services für ein Solo-Projekt (MariaDB, Redis, MinIO, RabbitMQ, Qdrant, phpMyAdmin)
- RabbitMQ wird kaum genutzt (Background-Services nutzen eigene Timer)
- Redis-Caching existiert aber wird selten eingesetzt

---

## 3. Domain-Modell: Was das System tut

### 3.1 Core User Journey

```
1. REGISTRIERUNG
   └── Email + Passwort, DHBW-Matrikelnummer

2. KONFIGURATION
   ├── Moodle-Token hinterlegen
   ├── E-Mail-Konten verbinden (IMAP/SMTP)
   ├── Persönliche AI-API-Keys (optional)
   └── Fächer/Kurse auswählen

3. DOKUMENT-UPLOAD & VERARBEITUNG
   ├── PDF/DOCX hochladen (Vorlesungsfolien, Skripte)
   └── Pipeline: Parse → Chunk → Embed → Entity-Extraction

4. ADAPTIVES LERNEN
   ├── Priorisierte Lernwarteschlange
   ├── 6 Übungstypen (MC, Lückentext, Drag&Drop, Slider, Code, Freitext)
   ├── Bloom's Taxonomy Progression (1→6)
   ├── FSRS Spaced Repetition
   └── 20/40/40 Schwierigkeitsverteilung

5. FORTSCHRITTSTRACKING
   ├── Mastery-Dashboard
   ├── Knowledge Graph Visualisierung
   ├── Lernstreak & Gamification
   └── Schwachstellen-Analyse
```

### 3.2 Kern-Entitäten (Domain Model)

```
User
├── Documents[]
│   ├── DocumentChunks[] (semantische Splits)
│   └── DocumentImages[] (extrahierte Bilder)
├── KnowledgeEntities[] (extrahierte Konzepte)
│   ├── Relationships[] (Beziehungen zwischen Konzepten)
│   └── Exercises[] (generierte Übungen)
├── LearningPriorities[] (priorisierte Lernwarteschlange)
├── CalendarEvents[] (Kalender: manuell + sync)
├── MoodleCourses[] (synchronisierte Kurse)
│   ├── MoodleAssignments[]
│   └── MoodleResources[]
├── TodoLists[] → Todos[]
├── LearningDeficits[] (erkannte Wissenslücken)
├── LearningStreak (Motivations-Tracking)
└── Emails[] (synchronisierte E-Mails)
```

### 3.3 Entity-Typen (Knowledge Graph)

12 Entitätstypen:
1. `concept` - Konzept/Begriff
2. `definition` - Definition
3. `formula` - Formel
4. `person` - Person (Wissenschaftler, Autor)
5. `date` - Datum/Zeitpunkt
6. `example` - Beispiel
7. `theorem` - Satz/Theorem
8. `method` - Methode/Verfahren
9. `term` - Fachbegriff
10. `algorithm` - Algorithmus
11. `data_structure` - Datenstruktur
12. `principle` - Prinzip/Grundsatz

### 3.4 Beziehungstypen (Knowledge Graph)

13 Beziehungstypen:
1. `is_a` - Vererbung/Klassifikation
2. `part_of` - Komposition
3. `relates_to` - Allgemeine Assoziation
4. `requires` - Voraussetzung/Abhängigkeit
5. `contradicts` - Widerspruch
6. `example_of` - Beispiel für
7. `defines` - Definition von
8. `uses` - Nutzt/Verwendet
9. `precedes` - Reihenfolge
10. `derives_from` - Abstammung
11. `extends` - Spezialisierung
12. `implements` - Implementierung
13. `similar_to` - Ähnlichkeit

---

## 4. Alle Features & Funktionen

### 4.1 Dokumentenverarbeitung

| Feature | Beschreibung | AI-Modell |
|---------|-------------|-----------|
| **Upload** | PDF, DOCX, DOC, TXT (max 100MB) | - |
| **Text-Extraktion** | iText7 (PDF), OpenXml (DOCX) | - |
| **OCR** | Bild-basierte PDFs | Gemini 3 Flash |
| **Semantisches Chunking** | 600-1500 Zeichen pro Chunk | Claude Sonnet 4.5 |
| **Embedding** | 1536-dimensionale Vektoren | OpenAI text-embedding-3-small |
| **Entity-Extraktion** | NER aus Chunks | Claude Sonnet 4.5 |
| **Relationship-Extraktion** | Beziehungen erkennen | Claude Sonnet 4.5 |
| **Auto-Linking** | Chunks mit >0.8 Kosinus-Ähnlichkeit verknüpfen | Qdrant |

### 4.2 Adaptives Lernsystem

| Feature | Beschreibung |
|---------|-------------|
| **FSRS** | Free Spaced Repetition Scheduler mit W0-W16 Parametern |
| **Bloom's Taxonomy** | 6 kognitive Stufen (Erinnern → Erschaffen) |
| **20/40/40 Verteilung** | 20% leicht, 40% mittel, 40% schwer (Vygotsky ZPD) |
| **Exponentieller Verfall** | 5% pro Tag, Minimum 40% Retention |
| **Prioritäts-Score** | 30% Deadline + 20% Relevanz + 25% Mastery-Gap + 15% Verfall + 10% Bloom-Gap |
| **Voraussetzungsketten** | Hard/Soft Blocking von Themen |
| **Lernstreak** | 1.0-1.5x Multiplikator (2% pro Tag, cap bei 25 Tagen) |

### 4.3 Übungstypen (6 Formate)

| Typ | Beschreibung | Bloom-Level |
|-----|-------------|-------------|
| **Multiple Choice** | Auswahl aus Optionen | 1-2 |
| **Lückentext** | Text mit Lücken ausfüllen | 1-2 |
| **Drag & Drop** | Elemente in Kategorien sortieren | 3 |
| **Slider** | Werte auf Spektrum einstellen | 3 |
| **Code Editor** | Code schreiben und validieren | 3-6 |
| **Freitext** | Kurze/lange Textantworten | 4-6 |

### 4.4 Moodle-Integration

| Feature | Beschreibung |
|---------|-------------|
| **Kurs-Sync** | Alle Kurse mit Metadaten |
| **Aufgaben-Sync** | Deadlines, Abgabestatus |
| **Ressourcen-Sync** | Dateien, Seiten, Wikis, Bücher |
| **Kalender-Sync** | Vorlesungen, Prüfungen, Fristen |
| **Auto-Download** | PDFs automatisch herunterladen und verarbeiten |

### 4.5 Kalender & Planung

| Feature | Beschreibung |
|---------|-------------|
| **Rapla-Sync** | DHBW-Stundenplan importieren |
| **Google Calendar** | OAuth2, bidirektionaler Sync |
| **Moodle-Kalender** | Deadlines und Prüfungen |
| **Manuelle Events** | Eigene Termine erstellen |
| **Wochen-/Listenansicht** | Flexible Kalenderdarstellung |

### 4.6 Todo-Verwaltung

| Feature | Beschreibung |
|---------|-------------|
| **Listen** | Mehrere benannte Listen |
| **Drag & Drop** | Aufgaben umsortieren |
| **Archiv** | Erledigte Aufgaben archivieren |
| **Erinnerungen** | Überfällige Aufgaben-Banner |
| **Quick Add** | Schnelles Hinzufügen |

### 4.7 E-Mail-Integration

| Feature | Beschreibung |
|---------|-------------|
| **3 Konten** | DHBW (Exchange), Privat (Gmail), Arbeit |
| **IMAP-Sync** | Periodische Synchronisation |
| **Zusammenfassung** | AI-basierte E-Mail-Zusammenfassung |
| **Aktionen** | Auf E-Mails reagieren |

### 4.8 Knowledge Graph Visualisierung

| Feature | Beschreibung |
|---------|-------------|
| **Netzwerk-Graph** | Vis.js interaktiver Graph |
| **Cluster-Visualisierung** | PCA-basierte Vektor-Cluster (Plotly) |
| **Semantische Suche** | Wissenssuche über Embeddings |
| **Tag-System** | Inhalte taggen und filtern |
| **Auto-Linking** | Automatische semantische Verknüpfung |

### 4.9 Weitere Features

| Feature | Beschreibung |
|---------|-------------|
| **Validation/Staging** | AI-extrahierte Entities bestätigen/ablehnen |
| **JavaDocs-Scraper** | Java-API-Docs für Übungsgenerierung |
| **Reise-Planer** | HAFAS-Zugverbindungen |
| **Exam-Simulation** | Prüfungssimulation mit Timer |
| **PWA** | Progressive Web App Unterstützung |

---

## 5. Algorithmen & Business-Regeln

### 5.1 FSRS (Free Spaced Repetition Scheduler)

```
Parameter (W0-W16):
W0-W3: Initiale Stabilität für [Wieder, Schwer, Gut, Leicht] = [0.4, 0.6, 2.4, 5.8]
W4: Stabilitäts-Multiplikator = 4.93
W5-W8: Schwierigkeitsfaktoren = [0.94, 0.86, 0.01, 1.49]
W9-W16: Erweiterte Verfall-/Vergessens-Parameter

Konfiguration:
- Ziel-Retention: 90%
- Max-Intervall: 365 Tage
- Min-Intervall: 1 Tag

State Machine:
- New (0): Noch nie gelernt
- Learning (1): < 3 korrekte Versuche, 1-3 Tage Spacing
- Review (2): Stabiles Wissen, Spacing nach Stabilität
- Relearning (3): Fehlgeschlagen bei Review, zurück auf 1 Tag
```

### 5.2 Exponentieller Verfall (Ebbinghaus)

```
DecayFactor = exp(-DecayRate × TageSeitInteraktion)
DefaultDecayRate = 5% pro Tag
MinimumRetention = 40%

EffektivesWissen = FsrsMastery × (0.4 + 0.6 × DecayFactor) × BaseStrength
```

### 5.3 Prioritäts-Berechnung (Multi-Faktor)

```
CompositeScore = 0.30 × DeadlineUrgency
               + 0.20 × TopicRelevance
               + 0.25 × MasteryGap
               + 0.15 × DecayAmount
               + 0.10 × BloomGap

DeadlineUrgency = 100 × (1 - TageZurDeadline / 30)
TopicRelevance  = Semantische Ähnlichkeit zum Fokus (0-100)
MasteryGap      = (1 - EffektivesWissen) × 100
DecayAmount     = (1 - DecayFactor) × 100
BloomGap        = (ZielBloomLevel - AktuellesBloomLevel) × 20

Voraussetzungs-Blocking:
- Strenge Voraussetzung nicht erfüllt → Score × 0.5
```

### 5.4 Schwierigkeitsauswahl (20/40/40)

```
WENN totalÜbungen < 5: → "leicht"
SONST:
    leichtAnteil  = leichtTotal / total
    mittelAnteil  = mittelTotal / total

    WENN leichtAnteil < 0.20: → "leicht"
    WENN mittelAnteil < 0.40: → "mittel"
    SONST:                    → "schwer"

Erfolgsraten-Ziele:
- Leicht: > 80% Erfolg
- Mittel: > 60% Erfolg
- Schwer: > 40% Erfolg
```

### 5.5 Bloom's Taxonomy Progression

```
Level 1 (Erinnern)   → MC, Lückentext
Level 2 (Verstehen)  → Lückentext, Wahr/Falsch
Level 3 (Anwenden)   → Drag&Drop, Code
Level 4 (Analysieren) → Code, Freitext
Level 5 (Bewerten)   → Freitext, Essay
Level 6 (Erschaffen) → Code, Kreativaufgaben

Aufstiegsregel: ≥ 3 Versuche UND ≥ 70% Erfolgsrate auf aktuellem Level
```

### 5.6 Lernstreak-Multiplikator

```
Multiplikator = 1.0 + min(0.5, CurrentStreak × 0.02)

Tag  1: 1.02x
Tag  7: 1.14x
Tag 25: 1.50x (Maximum)

Regeln:
- Inkrementiert beim ersten Exercise pro Tag
- Bricht nach 24h Inaktivität ab
- 1 Streak-Freeze pro Woche verfügbar
```

### 5.7 Auto-Linking Schwellenwert

```
Zwei Chunks werden automatisch verknüpft wenn:
Kosinus-Ähnlichkeit > 0.8

Embedding-Modell: OpenAI text-embedding-3-small (1536 Dimensionen)
```

---

## 6. API-Endpoints (vollständig)

### 6.1 Authentifizierung

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| POST | `/api/auth/login` | Login (Email/Passwort → JWT) |
| POST | `/api/auth/register` | Registrierung |
| GET | `/api/auth/me` | Aktueller User (JWT) |

### 6.2 User-Konfiguration

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| GET | `/api/user/api-keys` | API-Key-Status prüfen |
| PUT | `/api/user/api-keys` | API-Keys aktualisieren |
| DELETE | `/api/user/api-keys` | API-Keys löschen |

### 6.3 Dokumente

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| POST | `/api/files/upload` | Dokument hochladen (max 100MB) |
| GET | `/api/files` | Dokumente auflisten (Pagination) |
| GET | `/api/files/{id}` | Einzelnes Dokument |
| DELETE | `/api/files/{id}` | Dokument löschen |
| POST | `/api/files/bulk-delete` | Mehrere löschen |

### 6.4 OmniLearning (Hauptsystem)

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| POST | `/api/omni/dokumente/{id}/verarbeiten` | Dokument verarbeiten |
| POST | `/api/omni/dokumente/batch-verarbeiten` | Batch-Verarbeitung |
| GET | `/api/omni/entitaeten` | Entities mit Filtern |
| POST | `/api/omni/entitaeten/suche` | Semantische Suche |
| GET | `/api/omni/entitaeten/{id}` | Entity-Details |
| POST | `/api/omni/entitaeten` | Entity erstellen |
| GET | `/api/omni/entitaeten/{id}/verwandt` | Verwandte Entities |
| POST | `/api/omni/entitaeten/zusammenfuehren` | Entities zusammenführen |
| POST | `/api/omni/beziehungen` | Beziehung erstellen |
| POST | `/api/omni/entitaeten/{id}/beziehungen-generieren` | Auto-Beziehungen |
| GET | `/api/omni/entitaeten/{id}/voraussetzungen` | Voraussetzungen prüfen |
| GET | `/api/omni/entitaeten/{id}/voraussetzungs-kette` | Voraussetzungskette |
| POST | `/api/omni/uebungen/generieren` | Übung generieren |
| POST | `/api/omni/uebungen/session` | Lernsession generieren |
| POST | `/api/omni/uebungen/{id}/antwort` | Antwort abgeben |
| GET | `/api/omni/uebungen/faellig` | Fällige Übungen |
| GET | `/api/omni/uebungen/naechste` | Nächste empfohlene Übung |
| POST | `/api/omni/prioritaeten/berechnen` | Prioritäten berechnen |
| GET | `/api/omni/schwachstellen` | Schwachstellen |
| GET | `/api/omni/ueberfaellig` | Überfällige Items |
| GET | `/api/omni/graph` | Knowledge Graph |
| GET | `/api/omni/cluster` | Cluster-Visualisierung |
| GET | `/api/omni/vector-cluster` | PCA-Vektor-Cluster |
| GET | `/api/omni/statistiken` | Mastery-Statistiken |
| GET | `/api/omni/streak` | Lernstreak |
| GET | `/api/omni/schwierigkeitsverteilung` | 20/40/40 Verteilung |
| GET | `/api/omni/bloom-progression` | Bloom's Progression |

### 6.5 Kalender

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| GET | `/api/calendar/events/{userId}` | Events mit Filtern |
| POST | `/api/calendar/sync-rapla/{userId}` | Rapla synchronisieren |
| GET | `/api/calendar/test-rapla` | Rapla-Verbindung testen |
| GET | `/api/calendar/week-schedule` | Wochenplan |
| POST | `/api/calendar/events` | Manuelles Event |
| DELETE | `/api/calendar/events/{id}` | Event löschen |
| PATCH | `/api/calendar/{id}/notes` | Event-Notizen |
| GET | `/api/calendar/google/authorize/{userId}` | Google OAuth starten |
| GET | `/api/calendar/google/callback` | Google OAuth Callback |
| POST | `/api/calendar/google/sync-*` | Google-Sync-Varianten |

### 6.6 Moodle

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| POST | `/api/moodle/login` | Moodle-Login |
| POST | `/api/moodle/test` | Verbindung testen |
| POST | `/api/moodle/sync` | Voll-Sync auslösen |
| GET | `/api/moodle/status` | Sync-Status |
| GET | `/api/moodle/courses` | Kurse auflisten |
| GET | `/api/moodle/courses/{id}/assignments` | Kurs-Aufgaben |
| GET | `/api/moodle/assignments` | Alle Aufgaben |
| GET | `/api/moodle/courses/{id}/resources` | Kurs-Ressourcen |
| GET | `/api/moodle/resources` | Alle Ressourcen |
| GET | `/api/moodle/calendar` | Moodle-Kalender |
| POST | `/api/moodle/disable` | Sync deaktivieren |
| POST | `/api/moodle/enable` | Sync aktivieren |
| POST | `/api/moodle/resources/{id}/download` | Ressource herunterladen |
| POST | `/api/moodle/resources/download-all` | Alle herunterladen |

### 6.7 Todos

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| GET | `/api/todo-lists` | Listen abrufen |
| POST | `/api/todo-lists` | Liste erstellen |
| PUT | `/api/todo-lists/{id}` | Liste aktualisieren |
| DELETE | `/api/todo-lists/{id}` | Liste löschen |
| GET | `/api/todos` | Todos einer Liste |
| POST | `/api/todos` | Todo erstellen |
| PUT | `/api/todos/{id}/status` | Status ändern |
| POST | `/api/todos/{id}/move` | Todo verschieben |
| POST | `/api/todos/{id}/archive` | Archivieren |

### 6.8 Weitere Endpoints

| Methode | Route | Beschreibung |
|---------|-------|-------------|
| GET | `/api/mail/summary` | E-Mail-Zusammenfassung |
| GET | `/api/mail/emails` | E-Mails abrufen |
| POST | `/api/mail/sync` | E-Mail-Sync |
| GET | `/api/validation/pending` | Pending Entities |
| POST | `/api/validation/{id}/confirm` | Entity bestätigen |
| POST | `/api/validation/{id}/reject` | Entity ablehnen |
| GET | `/api/travel/connections` | HAFAS-Verbindungen |
| GET | `/health` | Health Check |

---

## 7. Datenbank-Schema

### 7.1 Core-Tabellen (für Neuanfang relevant)

```sql
-- Benutzer
users (
    id, email, password_hash, first_name, last_name,
    matriculation_number, course,
    openai_api_key_encrypted, anthropic_api_key_encrypted, gemini_api_key_encrypted,
    moodle_token_encrypted, moodle_base_url,
    created_at, updated_at
)

-- Dokumente
documents (
    id, user_id, title, file_name, file_path, mime_type, file_size,
    category, description, source, -- manual_upload | moodle_sync | nextcloud_sync
    is_processed, processing_status, metadata_json,
    created_at, updated_at
)

-- Dokument-Chunks
document_chunks (
    id, document_id, user_id, content, chunk_index,
    topic_label, section_heading, chunk_type,
    -- introduction | definition | example | exercise | conclusion | mixed
    page_number, char_start, char_end,
    embedding_id -- Qdrant Point ID
)

-- Knowledge-Entities (UNIFIED)
unified_knowledge_entities (
    id, user_id, name, description,
    entity_type, -- concept | definition | formula | person | etc.
    subject, topic, subtopic,
    importance_score, confidence_score,
    occurrence_count, source_document_id, source_chunk_id,
    mastery_score, bloom_level, next_review,
    fsrs_state, fsrs_stability, fsrs_difficulty,
    total_attempts, correct_attempts,
    easy_total, easy_correct, medium_total, medium_correct,
    hard_total, hard_correct,
    last_interaction, decay_rate,
    embedding_id -- Qdrant Point ID
)

-- Knowledge-Relationships
unified_knowledge_relationships (
    id, user_id, source_entity_id, target_entity_id,
    relationship_type, -- is_a | part_of | requires | etc.
    strength, evidence, confidence_score,
    is_prerequisite, prerequisite_strictness, -- soft | hard
    decay_rate, last_interaction
)

-- Übungen
unified_exercises (
    id, user_id, entity_id,
    question, correct_answer, explanation,
    exercise_type, -- multiple_choice | fill_in_blank | drag_drop | etc.
    difficulty, -- easy | medium | hard
    bloom_level, -- 1-6
    mode, -- learning | exam_prep | exam_simulation
    options_json, -- MC-Optionen, Drag-Items, etc.
    is_answered, is_correct, user_answer, score,
    time_limit_seconds, answered_at,
    next_review, fsrs_state,
    source_document_id, source_chunk_id,
    created_at
)

-- Lern-Prioritäten
unified_learning_priorities (
    id, user_id, entity_id,
    composite_score, deadline_urgency, topic_relevance,
    mastery_gap, decay_amount, bloom_gap,
    is_blocked, block_reason,
    calculated_at
)

-- Kalender
calendar_events (
    id, user_id, title, description,
    start_time, end_time, all_day,
    event_type, -- lecture | assignment | exam | deadline | project
    source, -- manual | moodle | google | rapla
    external_id, subject, professor,
    notes, location,
    created_at, updated_at
)

-- Moodle
moodle_courses (id, user_id, moodle_id, shortname, fullname, summary, start_date, end_date)
moodle_assignments (id, course_id, user_id, moodle_id, name, description, due_date, status)
moodle_resources (id, course_id, user_id, moodle_id, name, type, url, file_size, last_modified)

-- Todos
todo_lists (id, user_id, name, color, position, is_default, is_archive)
todos (id, list_id, user_id, title, description, is_completed, due_date, priority, position)

-- Lerndefizite
learning_deficits (
    id, user_id, entity_id, error_type, -- concept | calculation | application
    severity, -- low | medium | high | critical
    occurrence_count, description,
    related_document_ids_json, needs_tutoring,
    first_occurred, last_occurred, resolved_at
)

-- Lernstreak
learning_streaks (
    id, user_id, current_streak, longest_streak,
    last_activity_date, total_active_days,
    streak_freezes_available, streak_freezes_used
)
```

### 7.2 Indizes (Performance-relevant)

```sql
-- Häufige Abfragen
CREATE INDEX idx_documents_user_id ON documents(user_id);
CREATE INDEX idx_chunks_document_id ON document_chunks(document_id);
CREATE INDEX idx_entities_user_subject ON unified_knowledge_entities(user_id, subject);
CREATE INDEX idx_entities_user_next_review ON unified_knowledge_entities(user_id, next_review);
CREATE INDEX idx_exercises_user_entity ON unified_exercises(user_id, entity_id);
CREATE INDEX idx_calendar_user_date ON calendar_events(user_id, start_time);
CREATE INDEX idx_priorities_user_score ON unified_learning_priorities(user_id, composite_score DESC);
```

---

## 8. Externe Integrationen

### 8.1 AI-Services

| Provider | Modelle | Kosten (pro 1M Token) | Verwendung |
|----------|---------|----------------------|------------|
| **OpenAI** | gpt-5-mini | $0.25/$2 | Standard-Aufgaben |
| **OpenAI** | gpt-5 | $1.25/$10 | Komplexere Aufgaben |
| **OpenAI** | gpt-5.2 | $1.75/$14 | Flagship |
| **OpenAI** | text-embedding-3-small | minimal | Embeddings (1536D) |
| **OpenAI** | whisper-1 | per minute | Speech-to-Text |
| **Anthropic** | claude-haiku-4.5 | $1/$5 | Schnelle Aufgaben |
| **Anthropic** | claude-sonnet-4.5 | $3/$15 | Entity-Extraktion, Reasoning |
| **Anthropic** | claude-opus-4.5 | $15/$75 | Komplexes Reasoning |
| **Google** | gemini-3-flash-preview | $0.50/$3 | OCR, Bild-Analyse |
| **Deepgram** | nova-2 | per minute | Live-Transkription |

### 8.2 Moodle API

```
Base URL: https://moodle.dhbw-ravensburg.de
Auth: Token-basiert (wstoken)
Sync-Intervall: 1 Minute (konfigurierbar)

Web Service Funktionen:
- core_webservice_get_site_info
- core_course_get_courses
- mod_assign_get_assignments
- core_calendar_get_calendar_events
- mod_resource_get_resources (+ alle Modul-Typen)
```

### 8.3 Google Calendar

```
Auth: OAuth 2.0 (Client Credentials)
Scopes: calendar.readonly, calendar.events
Features: Import, Export, Bidirektionaler Sync
```

### 8.4 DHBW Rapla

```
Base URL: https://rapla-ravensburg.dhbw.de/rapla
Format: iCalendar (.ics)
Sync: Polling alle 60 Minuten
```

### 8.5 Qdrant (Vektor-DB)

```
Port: 6333 (REST), 6334 (gRPC)
Dimension: 1536
Collections:
- dhbw_omni_entities (Knowledge Entities)
- dhbw_omni_exercises (Exercises)
Metriken: Cosine Similarity
Auto-Link Threshold: 0.8
```

### 8.6 E-Mail (IMAP/SMTP)

```
DHBW-Account:
  IMAP: outlook.office365.com:993
  SMTP: smtp.office365.com:587
  Polling: 1 Minute

Privat (Gmail):
  IMAP: imap.gmail.com:993
  SMTP: smtp.gmail.com:587
  Polling: 5 Minuten
```

---

## 9. Frontend-Architektur

### 9.1 Seiten (14 Views)

| View | Route | Funktion |
|------|-------|----------|
| HomeView | `/` | Landing Page |
| LoginView | `/login` | Anmeldung |
| RegisterView | `/register` | Registrierung |
| DashboardView | `/dashboard` | Hauptübersicht |
| FilesView | `/files` | Dokumentenverwaltung |
| CalendarView | `/calendar` | Kalender |
| CalendarSettingsView | `/calendar/settings` | Kalender-Einstellungen |
| ProfileView | `/profile` | Profil & Integrationen |
| LearningView | `/learning` | Legacy-Lernsystem |
| OmniLernenView | `/omni` | Neues Lernsystem (Haupt) |
| TasksView | `/tasks` | Todo-Listen |
| KnowledgeNetworkView | `/knowledge` | Wissensgraph |
| InteractiveTestView | `/test/interactive` | Prüfungssimulation |
| ValidationView | `/validation` | AI-Entity-Bestätigung |
| TravelView | `/travel` | Reiseplanung |

### 9.2 Komponenten (50+)

**Übungs-Komponenten:** InteractiveExercisePlayer, MultipleChoice, FillInBlank, DragDrop, TextInput, ExamTimer

**Lern-Komponenten:** DeficitsTab, ExercisesTab, ResolvedTab, InteractiveTab, LearningStatsCards, PriorityCard, StreakWidget, DifficultyDistribution

**Wissensgraph:** NetworkGraph (vis.js), ClusterVisualization, NodeDetailsPanel, SemanticSearchDialog

**OmniLernen:** OmniExercisePlayer, OmniGraphVisualization, OmniVectorClusterVisualization (Plotly)

**Profil:** AccountInfoCard, PasswordChangeCard, ApiKeysCard, IntegrationsCard, MoodleIntegrationCard

**Tasks:** TaskListSidebar, TaskItem, QuickAddTask, ReminderBanner

### 9.3 State Management (4 Pinia Stores)

| Store | State | Zweck |
|-------|-------|-------|
| **auth** | user, token, isLoading, error | JWT-Authentifizierung |
| **taskList** | lists, tasks, archivedTasks, overdueTasks, stats | Todo-Verwaltung |
| **mail** | emails, summary, lastSync | E-Mail-Management |
| **validation** | pendingEntities, statistics | AI-Entity-Staging |

### 9.4 Visualisierungs-Libraries

| Library | Verwendung |
|---------|-----------|
| **vis-network** | Knowledge Graph (interaktiv) |
| **Plotly.js** | PCA-Vektor-Cluster |
| **Chart.js + vue-chartjs** | Statistik-Charts |

---

## 10. Infrastruktur & Deployment

### 10.1 Docker-Services

| Service | Image | Ports | Zweck |
|---------|-------|-------|-------|
| MariaDB | 11.2 | 3306 | Primäre DB |
| Redis | 7.2-alpine | 6379 | Cache |
| MinIO | latest | 9000/9001 | Datei-Storage |
| Qdrant | latest | 6333/6334 | Vektor-DB |
| RabbitMQ | 3.12-management | 5672/15672 | Message Queue |
| phpMyAdmin | latest | 8080 | DB-Admin |
| Nginx | alpine | 80/443 | Reverse Proxy (Prod) |

### 10.2 Deployment

```
Server: 192.168.178.198 (SSH: root@)
Projekt: /root/dhbw-automation-deploy/dhbw-automation
Git Remote: server → /root/git-repos/dhbw-automation.git

Auto-Deploy: git push server main
→ Post-Receive Hook → docker compose build → up -d
```

### 10.3 CI/CD (GitHub Actions)

```
1. Backend Tests (MariaDB + Redis)
2. Frontend Tests (Lint + Build + Unit)
3. Docker Build (nur bei Push auf main)
4. Security Scan (Trivy)
5. Deploy to Staging
6. E2E Tests (Playwright)
```

---

## 11. Dependency-Inventar

### 11.1 Backend (.NET 8) - Kerndependencies

| Package | Version | Zweck |
|---------|---------|-------|
| EF Core + Pomelo MySQL | 8.0.0 | ORM |
| JWT Bearer | 8.0.0 | Auth |
| Redis Cache | 8.0.1 | Caching |
| Polly | 8.6.5 | Resilience |
| Qdrant.Client | 1.12.0 | Vektor-DB |
| itext7 | 8.0.2 | PDF |
| DocumentFormat.OpenXml | 3.0.0 | Office-Docs |
| MailKit/MimeKit | 4.8.0 | E-Mail |
| Google.Apis.Calendar | 1.73.0 | Google Cal |
| Serilog | 8.0.0 | Logging |
| Minio | 6.0.2 | S3-Storage |
| RabbitMQ.Client | 6.8.1 | Message Queue |
| Ical.Net | 4.2.0 | iCalendar |
| Markdig | 0.38.0 | Markdown |

### 11.2 Frontend (Vue.js 3) - Kerndependencies

| Package | Version | Zweck |
|---------|---------|-------|
| Vue | 3.4.0 | Framework |
| Vue Router | 4.2.5 | Routing |
| Pinia | 2.1.7 | State |
| Vuetify | 3.5.0 | UI |
| Axios | 1.6.5 | HTTP |
| Chart.js + vue-chartjs | 4.4.1 / 5.3.0 | Charts |
| vis-network | 9.1.9 | Graphen |
| Plotly.js | 2.35.3 | Cluster-Viz |
| dayjs | 1.11.10 | Datum |
| DOMPurify | 3.0.8 | Sanitization |
| marked | 11.1.1 | Markdown |
| socket.io-client | 4.6.1 | WebSocket |
| pdfjs-dist | 4.0.379 | PDF-Viewer |
| vuedraggable | 4.1.0 | Drag&Drop |

### 11.3 Mobile (Flutter) - Kerndependencies

| Package | Version | Zweck |
|---------|---------|-------|
| flutter_riverpod | 2.6.1 | State |
| dio | 5.9.0 | HTTP |
| hive | 2.2.3 | Lokale DB |
| go_router | 15.1.2 | Navigation |
| freezed | 2.4.6 | Immutable Models |
| flutter_secure_storage | 10.0.0 | Sichere Speicherung |

---

## 12. Empfehlungen für den Neuanfang

### 12.1 Was beibehalten

**Behalten (bewährte Konzepte):**
- FSRS Spaced Repetition Algorithmus (wissenschaftlich fundiert)
- Bloom's Taxonomy 6-Level Progression
- 20/40/40 Schwierigkeitsverteilung (Vygotsky ZPD)
- Exponentieller Verfall (Ebbinghaus)
- Multi-Faktor Prioritätsberechnung
- 6 Übungstypen
- 12 Entity-Typen + 13 Beziehungstypen
- Dokument-Pipeline (Parse → Chunk → Embed → Extract)
- Moodle-Integration Scope

**Überdenken:**
- Braucht es wirklich RabbitMQ? (Background-Tasks können simpler sein)
- MinIO vs. einfacher Filesystem-Storage?
- Redis vs. In-Memory-Cache für Solo-Projekt?
- Separate Vektor-DB oder integrierte Lösung (pgvector)?

### 12.2 Architektur-Empfehlungen

**Option A: Monolith (empfohlen für Solo-Projekt)**
```
Backend: Python (FastAPI) oder Node.js (NestJS)
Frontend: Vue.js 3 oder Next.js (React)
DB: PostgreSQL + pgvector (Vektor + Relational in einem)
Cache: In-Memory (klein genug)
Storage: Lokales Filesystem oder S3
```

**Option B: Bestehende Tech beibehalten, sauberer**
```
Backend: .NET 8 (aber CLEAN Architecture)
Frontend: Vue.js 3 + Vuetify
DB: PostgreSQL + pgvector (statt MariaDB + Qdrant)
```

### 12.3 Architektur-Prinzipien für Neuanfang

1. **EIN Lernsystem** - nicht 3 parallele
2. **Repository Pattern** - keine direkten DbContext-Zugriffe
3. **CQRS** für komplexe Queries (Read/Write trennen)
4. **API-Response Standard** - IMMER gleiche Struktur
5. **Feature-Module** - nicht nach Typ (Controller/Service), sondern nach Feature
6. **Tests first** - Mindestens Service-Layer Tests
7. **TypeScript strict** - Keine `any` im Frontend
8. **Saubere Auth** - JWT vollständig implementieren
9. **API-Client generieren** - aus OpenAPI Spec
10. **Migrations-Pipeline** - nicht auto-migrate beim Start

### 12.4 Vereinfachte Service-Architektur

```
Statt 35+ Services:

1. AuthService           - Authentifizierung & JWT
2. DocumentService       - Upload, Parsing, Chunking, Embedding
3. KnowledgeService      - Entity-Extraktion, Graph, Beziehungen
4. LearningService       - Übungsgenerierung, FSRS, Bloom, Prioritäten
5. MoodleService         - Sync (Kurse, Aufgaben, Ressourcen, Kalender)
6. CalendarService       - Events aus allen Quellen
7. TaskService           - Todo-Listen & Todos
8. AIGatewayService      - Multi-Model API Gateway
9. EmbeddingService      - Vektor-Operationen
10. EmailService         - E-Mail-Sync (optional)
```

### 12.5 Feature-Priorität für MVP

**Phase 1 (MVP):**
- [ ] Auth (JWT)
- [ ] Dokument-Upload & Verarbeitung
- [ ] Knowledge Entity Extraktion
- [ ] Übungsgenerierung (MC + Freitext)
- [ ] FSRS Spaced Repetition
- [ ] Basie-Dashboard

**Phase 2:**
- [ ] Moodle-Integration
- [ ] Kalender (Rapla + Moodle)
- [ ] Alle 6 Übungstypen
- [ ] Knowledge Graph Visualisierung
- [ ] Bloom's Taxonomy Progression

**Phase 3:**
- [ ] Todo-Verwaltung
- [ ] E-Mail-Integration
- [ ] Google Calendar Sync
- [ ] Exam-Simulation
- [ ] PWA / Mobile

---

## Anhang: Environment-Variablen (vollständig)

```env
# App
APP_NAME=DHBW_Automation
APP_ENV=development
APP_DEBUG=true
APP_URL=http://localhost:5173
API_URL=http://localhost:5000

# Database
DB_HOST=localhost
DB_PORT=3306
DB_DATABASE=dhbw_automation
DB_USERNAME=dhbw_user
DB_PASSWORD=

# Redis
REDIS_HOST=localhost
REDIS_PORT=6379

# MinIO
MINIO_ENDPOINT=localhost:9000
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin
MINIO_BUCKET_NAME=dhbw-files

# RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest

# AI
OPENAI_API_KEY=
ANTHROPIC_API_KEY=
GEMINI_API_KEY=
DEEPGRAM_API_KEY=

# AI Modelle
OPENAI_MODEL=gpt-5-mini
ANTHROPIC_MODEL=claude-sonnet-4-5
GEMINI_MODEL=gemini-3-flash-preview
EMBEDDING_MODEL=text-embedding-3-small

# Auth
JWT_SECRET=min_32_zeichen_secret
JWT_ISSUER=DHBWAutomation
JWT_AUDIENCE=DHBWAutomationUsers
JWT_EXPIRATION_HOURS=24

# Moodle
MOODLE_BASE_URL=https://moodle.dhbw-ravensburg.de
MOODLE_TOKEN=

# Google
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
GOOGLE_REDIRECT_URI=http://localhost:5000/api/calendar/google/callback

# Rapla
RAPLA_BASE_URL=https://rapla-ravensburg.dhbw.de/rapla
RAPLA_POLL_INTERVAL_MINUTES=60

# Feature Flags
FEATURE_LIVE_LECTURE=true
FEATURE_TUTOR_MODE=true
FEATURE_AUTO_BACKUP=true
FEATURE_TTS_AUDIO=true
```

---

*Dieses Dokument enthält alle Informationen, die für einen vollständigen Neuanfang des DHBW Automation Systems benötigt werden. Jeder Algorithmus, jede Integration, jedes Feature und jede Datenbankstruktur ist dokumentiert.*

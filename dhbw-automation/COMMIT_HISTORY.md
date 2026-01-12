# Git Commit History - DHBW Automation

**Zeitraum:** 08.01.2026 - 12.01.2026
**Gesamt:** 181 Commits

---

## Donnerstag, 08.01.2026

**Commits:** 34 | **Zeitspanne:** 12:44:15 - 23:36:49 (10.9h)

### 1. `12:44:15` - first commit

### 2. `12:56:07` - F├╝ge Unterst├╝tzung f├╝r SQLite-Datenbank hinzu und verbessere die Datenbankinitialisierung

### 3. `13:05:14` - F├╝ge Registrierungskomponente hinzu und aktualisiere Authentifizierungslogik

### 4. `14:12:11` - F├╝ge Moodle-API-Client und Authentifizierungshelfer hinzu

### 5. `15:03:51` - F├╝ge CalendarController hinzu mit CRUD-Operationen f├╝r Events und Rapla-Integration

### 6. `15:56:48` - F├╝ge Rapla-Integration hinzu mit Synchronisation und Verbindungstest f├╝r Kalenderereignisse

### 7. `20:19:16` - Update: Implement Calendar and Profile views, update API controllers and routing

### 8. `20:40:40` - Add Docker production setup and deployment documentation

### 9. `20:44:49` - Disable backend/frontend in docker-compose.prod - infrastructure only

### 10. `20:47:00` - Change ports to avoid conflicts with existing containers

### 11. `20:50:33` - Fix phpMyAdmin port and add complete server deployment documentation

### 12. `20:54:23` - Add rebuild and deployment scripts

### 13. `21:00:15` - Add Frontend to production deployment

### 14. `21:17:47` - Fix TypeScript errors in Frontend

### 15. `21:21:57` - Fix remaining TypeScript errors

### 16. `21:24:11` - Remove unused formatDate function

### 17. `21:28:19` - Change Frontend port to 8091

### 18. `21:29:00` - Update docker-compose port to 8091

### 19. `21:34:33` - Add initial database schema migration

### 20. `21:38:13` - Configure CORS and enable Backend deployment

### 21. `21:41:48` - Add Model classes that were missing from git

### 22. `21:43:09` - Fix Program.cs using statement

### 23. `21:49:50` - Add missing using DHBWAutomation.Infrastructure.Storage

### 24. `21:52:11` - Add MinIOStorageService to Git repository

### 25. `21:57:44` - Fix backend port mapping from 80 to 8080

### 26. `22:00:37` - Add deployment success documentation

### 27. `22:01:59` - Add .env.production for frontend server deployment

### 28. `22:22:53` - Fix Login/Registration: Update API URLs, enable Nginx proxy, fix Swagger

### 29. `22:25:00` - Fix SwaggerFileOperationFilter namespace

### 30. `23:09:23` - Fix: Add SslMode=None to MariaDB connection string

### 31. `23:11:54` - Fix: Use correct DB environment variables and add SslMode=None

### 32. `23:17:53` - Fix: Use correct environment variable names (DB_HOST, REDIS_HOST) instead of ConnectionString format

### 33. `23:36:07` - Fix: Remove URL encoding for Rapla file parameter (use + instead of %2B)

### 34. `23:36:49` - Fix: Add API keys and endpoints to AIService; create fix_users_table.sql for database schema updates

---

## Freitag, 09.01.2026

**Commits:** 46 | **Zeitspanne:** 10:02:02 - 23:06:24 (13.1h)

### 1. `10:02:02` - feat: Implement Learning Analytics and Scheduling Services

### 2. `10:49:47` - feat: Add calendar event details dialog and list view components

### 3. `12:39:26` - feat: Add Profile and Todo screens with functionality for user management and task handling

### 4. `13:50:33` - feat: Implement rate limiting and bulk processing for document uploads

### 5. `14:20:06` - feat: Implement Knowledge Base System and Travel Feature

### 6. `14:22:13` - feat: Update exercise generation logic in LearningAnalyticsService to use ChatJsonAsync and enhance error handling

### 7. `16:06:03` - feat: Implement Travel feature with service and UI for train connections

### 8. `16:12:41` - Fix .gitignore and add Backend Core Models

### 9. `16:17:19` - Add Storage folder to Infrastructure

### 10. `16:22:04` - chore: add pubspec.lock file with updated dependencies

### 11. `16:34:30` - feat: Implement user-specific API keys with encryption - Add encrypted API key fields to User model - Create UserController for API key management - Update AIService to prioritize user keys over global env keys - Add EncryptionHelper for secure key storage - Create database migration for API key columns - Update frontend to send/receive keys from backend

### 12. `17:14:05` - feat: Add health check to backend Dockerfile and update gradle.properties for path check

### 13. `17:48:57` - Fix CORS: Allow multiple frontend origins

### 14. `17:52:18` - Fix JWT authentication configuration

### 15. `18:12:23` - Fix JWT validation: Add logging and relax token validation

### 16. `18:20:14` - Fix: Use MariaDB instead of SQLite for production

### 17. `18:58:12` - Fix: Use relative API URLs in production for Nginx proxy

### 18. `19:04:04` - Fix: Forward Authorization header in Nginx proxy

### 19. `19:14:36` - Fix: Use API service with JWT token instead of raw axios in Dashboard components

### 20. `19:30:04` - Add missing patch method to API service

### 21. `19:37:39` - Fix: Remove duplicate /api/ prefix from API calls

### 22. `19:44:07` - Fix: Add JWT_SECRET environment variable to backend

### 23. `19:47:36` - Add migration for core tables (todos, user_interactions, learning_deficits)

### 24. `19:48:41` - Fix: Use api service in GoogleCalendarConnect and remove /api/ duplication

### 25. `19:53:19` - Fix: Add Google Calendar env vars and fix calendar_events UpdatedAt default

### 26. `19:56:29` - Fix: Set UpdatedAt when creating CalendarEvent in RaplaClient

### 27. `20:01:42` - Fix: Sync JWT_SECRET default value between AuthService and Program.cs

### 28. `20:05:29` - Fix: Read JWT_SECRET at runtime inside AddJwtBearer lambda

### 29. `20:14:23` - Fix: Create SymmetricSecurityKey once and reuse it for token validation

### 30. `20:18:37` - Fix: Use IssuerSigningKeyResolver to ensure key is available during validation

### 31. `20:25:57` - Fix: Ersetze axios durch api-Service f├╝r JWT-Token-Authentifizierung

### 32. `20:32:16` - Fix: JWT Security Key direkt setzen statt Resolver verwenden

### 33. `20:35:16` - Neu: Dokumentation f├╝r Git Push zum Server hinzuf├╝gen

### 34. `20:50:35` - Fix calendar_events Subject column length for Rapla sync

### 35. `21:15:02` - Frontend: Kategorie-Auswahl optional - AI erkennt automatisch

### 36. `21:27:44` - Fix TypeScript: Verwende leeren String statt null f├╝r optionale Kategorie

### 37. `21:34:25` - Fix: F├╝ge Port-Mapping 8091:80 f├╝r Frontend hinzu

### 38. `21:51:31` - Kategorie-Dropdown komplett entfernt - KI macht alles automatisch

### 39. `21:53:51` - Fix: Korrigiere Upload-Funktion nach Kategorie-Entfernung

### 40. `21:59:26` - Fix: Sende category nur wenn vorhanden

### 41. `22:36:44` - Fix file upload: Remove explicit Content-Type header for FormData

### 42. `22:41:57` - Fix nginx file upload: Add client_max_body_size and proxy buffering settings

### 43. `22:46:11` - Add ModelState logging to debug file upload

### 44. `22:56:15` - Add ModelState logging to debug file upload

### 45. `22:57:45` - Add ModelState logging to debug file upload

### 46. `23:06:24` - Add extensive logging throughout file upload pipeline

---

## Samstag, 10.01.2026

**Commits:** 39 | **Zeitspanne:** 00:09:38 - 15:36:43 (15.5h)

### 1. `00:09:38` - Fix file upload: Set correct Content-Type for multipart/form-data

### 2. `00:26:25` - Fix file upload: Let browser set Content-Type with boundary automatically

### 3. `00:31:57` - Fix file upload: Remove Content-Type header for FormData in interceptor

### 4. `00:52:46` - Add debug logging for file upload

### 5. `00:57:17` - Fix file upload: Use File instead of File[] for v-file-input

### 6. `01:08:24` - Fix file upload: Let browser set Content-Type with boundary automatically

### 7. `01:19:39` - Add extensive logging to debug text extraction issue

### 8. `01:24:04` - Fix MinIO download: Use async CopyToAsync and add extensive logging

### 9. `09:15:28` - Fix MinIO download: Use synchronous callback instead of async

### 10. `09:36:39` - Fix MinIO download: Add extensive debug logging (force-add)

### 11. `09:42:34` - Fix MinIO download: Add StatObject + keep synchronous callback

### 12. `09:48:05` - Fix MinIO upload: Reset stream position to 0 before upload (from official docs)

### 13. `09:53:58` - Fix file upload: Copy IFormFile to MemoryStream for seekability

### 14. `10:02:57` - Fix MinIO configuration: Use IConfiguration instead of env vars

### 15. `10:16:31` - Fix: Pass userId to all AI services + Tags JSON constraint

### 16. `10:18:50` - Fix IIntentAnalysisService interface signature

### 17. `10:20:57` - Fix ChatCompletionAsync parameter order

### 18. `10:35:56` - CRITICAL FIX: Use user-specific API keys in OpenAI HTTP requests

### 19. `10:40:47` - DEBUG: Log OpenAI key prefix for troubleshooting

### 20. `10:49:17` - DEBUG: Log encrypted and decrypted API key for troubleshooting

### 21. `11:02:02` - Add comprehensive debug logging to GetApiKeyAsync to diagnose encryption issue

### 22. `11:35:02` - Fix Intent Analysis: Use user-specific Anthropic API keys from database

### 23. `11:43:44` - Fix: Use correct Claude model ID from official docs

### 24. `11:53:31` - Add debug logging for Anthropic response parsing

### 25. `12:01:32` - Fix: Remove markdown code blocks from Anthropic JSON response

### 26. `12:09:35` - Fix: Parse 'meetings' array from Claude response

### 27. `12:21:30` - Fix three critical issues with Intent Analysis

### 28. `12:30:06` - Fix 500 error in /api/validation/pending endpoint

### 29. `12:41:26` - Fix circular reference: Add [JsonIgnore] to AIQuestion.StagedEntity

### 30. `13:23:14` - Fix infinite loop in QuestionsList causing frontend freeze

### 31. `13:31:15` - Implement bulk delete functionality for documents in FilesController and IFileService; add corresponding API service method and UI support in FilesView

### 32. `13:38:30` - Fix: Improve markdown code block removal to handle trailing whitespace

### 33. `13:39:14` - Fehlerbehebung: Hinzuf├╝gen von Schaltfl├ñchen zum Schlie├ƒen von Dialogen und Verbesserung der Benutzeroberfl├ñche in mehreren Komponenten

### 34. `13:54:18` - Fix validation system: field mapping, text inputs, auto-reject

### 35. `14:16:37` - Fix tutoring system: Add user-specific API key support to AnthropicClient and set tutoring threshold to 1 error

### 36. `14:31:54` - Fehlerbehebung: Entfernen von Abh├ñngigkeiten zu connectivity_plus und dbus; Anpassung der Meeting-Verarbeitung auf eine Liste von Meetings

### 37. `14:32:27` - Fix meeting handling: Support multiple meetings as separate entities + Prevent empty TODOs and meta-tasks

### 38. `15:15:39` - feat: Implement file management and validation features

### 39. `15:36:43` - feat: Implement Learning Module with Repository, Provider, and UI

---

## Sonntag, 11.01.2026

**Commits:** 36 | **Zeitspanne:** 13:39:53 - 20:49:20 (7.2h)

### 1. `13:39:53` - fix: Correct Claude model ID from claude-sonnet-4.5 to claude-sonnet-4-5

### 2. `13:50:14` - fix: Update build mode to release in local.properties and streamline quickReject function in ValidationView.vue

### 3. `14:15:37` - fix: Correct JSON parsing in ParseExerciseJson

### 4. `14:29:54` - fix: Backend validates answers instead of trusting frontend

### 5. `14:49:58` - feat: Improve exercise difficulty distribution and HTML rendering

### 6. `15:00:10` - feat: Add hybrid exercise system (Brilliant + KA-Prep)

### 7. `15:05:11` - feat: Add interactive exercise Vue components (mobile-friendly)

### 8. `15:12:24` - feat: Integrate interactive exercises into LearningView

### 9. `15:17:23` - feat: Implement intelligent multi-list todo system

### 10. `15:20:34` - fix: Complete hybrid exercise system integration

### 11. `15:21:52` - feat: Add Knowledge Network System models and services

### 12. `16:55:04` - Add new features: Images, JavaDocs, KnowledgeNetwork, and IntegrationsCard

### 13. `18:16:40` - feat: Add Knowledge Network frontend with graph visualization

### 14. `18:17:56` - feat: Add architecture diagram for NGINX proxy and system components

### 15. `18:18:22` - fix: Correct property name in TagsController (AssignedAt instead of CreatedAt)

### 16. `18:21:24` - fix: Korrigiere Formatierung in der README.md f├╝r Anthropic

### 17. `18:38:00` - fix: Resolve all backend build errors

### 18. `18:49:26` - fix: Resolve TypeScript build errors in frontend

### 19. `18:51:55` - fix: Complete TypeScript error resolution

### 20. `18:53:13` - fix: Remove unused _touchDragElement variable

### 21. `19:05:34` - Update project paths, API constants, and calendar event model defaults

### 22. `19:10:16` - fix: Prevent deletion of past calendar events during Rapla sync

### 23. `19:11:11` - docs: Update system architecture diagram and enhance AI services section

### 24. `19:13:18` - docs: Update system architecture diagrams for clarity and detail

### 25. `19:16:01` - docs: Entferne Diagrammsektion aus der README.md

### 26. `19:54:15` - fix: Disable broken healthchecks for qdrant and backend (no curl), fix nginx healthcheck

### 27. `19:54:33` - docs: F├╝ge neue Datei deletme.md hinzu

### 28. `20:11:02` - fix: Add missing Knowledge Network functionality

### 29. `20:20:55` - feat: Add endpoint to index all existing content for Knowledge Network

### 30. `20:23:25` - feat: Hybrid-Ansatz f├╝r Rapla Sync - R├ñume aus HTML extrahieren

### 31. `20:25:58` - fix: KnowledgeNetworkService - JavaDocsExercise hat kein UserId

### 32. `20:34:48` - fix: WeekView overlapping events - display side by side

### 33. `20:36:09` - feat: Erweiterung der WebFetch-Funktionalit├ñt und Anpassung der Datumsberechnung in CalendarState

### 34. `20:41:11` - fix: WeekView - clamp events to visible time range (7:00-22:00)

### 35. `20:42:25` - fix: Add support for learning/ai_generated event sources

### 36. `20:49:20` - fix: WeekView timezone bug - use local dates instead of UTC

---

## Montag, 12.01.2026

**Commits:** 26 | **Zeitspanne:** 08:11:38 - 22:21:58 (14.2h)

### 1. `08:11:38` - feat: Erweiterung der WebFetch-Funktionalit├ñt und Hinzuf├╝gen von Flutter-Befehlen

### 2. `11:30:42` - Add temporal reference parsing and contextual linking services

### 3. `14:48:41` - feat: Hinzuf├╝gen von Chunking- und Dokumentenmodellen sowie Moodle API-Client-Verbesserungen

### 4. `15:06:38` - Add ChunkingService and MoodleSyncService for document processing and Moodle data synchronization

### 5. `15:19:29` - feat: Verwendung von Typaliasen zur Aufl├Âsung von Mehrdeutigkeiten zwischen Modellen und API DTOs in MoodleSyncService

### 6. `15:39:25` - feat: Add 2D cluster visualization to Knowledge Network

### 7. `16:09:59` - feat: Verbesserung der Zeitformatierung zur Vermeidung von Zeitzonenproblemen in Kalenderkomponenten

### 8. `16:11:08` - feat: Hinzuf├╝gen einer neuen Datei mit dem Projektpfad f├╝r die Datenwissenschafts├╝bungen

### 9. `19:10:57` - fix: Fix undefined 'start' variable in WeekView.vue

### 10. `19:13:53` - fix: Correct toLocalDateString call with proper Date object

### 11. `19:14:10` - feat: Neue Datei mit dem Projektpfad f├╝r die Datenwissenschafts├╝bungen hinzugef├╝gt

### 12. `19:35:08` - fix: Change Qdrant port from 6333 (REST) to 6334 (gRPC)

### 13. `20:49:51` - fix: Entfernen von tempor├ñren Dateien und Wiederherstellung des Projektpfads

### 14. `21:01:14` - feat: Implement vector retrieval and cluster visualization

### 15. `21:05:23` - fix: Fix build errors in cluster visualization

### 16. `21:10:38` - fix: Use correct Qdrant ScrollAsync API

### 17. `21:16:59` - fix: Map backend entity types to frontend expected format

### 18. `21:23:43` - fix: Lower search threshold from 0.7 to 0.5 and add logging

### 19. `21:33:21` - feat: Add SQL migration script for missing database tables

### 20. `21:37:15` - feat: Add detailed logging to semantic search for debugging

### 21. `21:44:41` - fix: Entferne tempor├ñre Dateien aus dem Projektverzeichnis

### 22. `21:47:40` - debug: Add threshold debugging to Qdrant search

### 23. `21:54:31` - fix: Lower semantic search threshold from 0.5 to 0.15

### 24. `22:06:09` - debug: Log API key preview to verify which key is used

### 25. `22:14:54` - debug: Log full OpenAI response and set threshold to 0

### 26. `22:21:58` - debug: Add logging to SearchAsync to find why results are lost

---

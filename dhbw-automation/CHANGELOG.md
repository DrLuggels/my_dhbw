# Changelog

Alle wichtigen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/),
und das Projekt folgt [Semantic Versioning](https://semver.org/lang/de/).

## [Unreleased]

### Geplant
- Live-Lecture-Mode mit Echtzeit-Transkription
- Tutor-Mode mit personalisierten Lernplänen
- Mobile App (iOS & Android)
- Video-Analyse für YouTube-Vorlesungen

---

## [0.3.0] - 2026-01-XX (In Development)

### Added
- 🎙️ Live-Lecture-Mode (Beta)
  - Echtzeit-Audio-Transkription via Deepgram
  - WebSocket-basiertes Streaming
  - Automatische Konzept-Erkennung
  - Speaker Diarization
- 🧑‍🏫 Tutor-Mode (Beta)
  - Personalisierte Lernpläne
  - Spaced Repetition System
  - Quiz-Generator
  - Fortschritts-Tracking
- 🔊 TTS-Integration für Auto-Wiedergabe

### Changed
- Verbesserte AI-Gateway-Architektur
- Optimierte Datei-Analyse Performance
- Überarbeitetes Dashboard-Design

### Fixed
- Umlauts in Mail-Subjects werden korrekt verarbeitet
- Calendar-Sync bei Zeitzonen-Wechsel
- Memory Leak im Background Worker

---

## [0.2.0] - 2026-01-07

### Added
- 📧 Mail-Integration (3 Accounts)
  - IMAP/SMTP Support
  - Automatische Datei-Extraktion
  - 5-Minuten Polling
- 📅 Google Calendar Sync
  - Bidirektionale Synchronisation
  - Termin-Änderungs-Tracking
- 🎓 Moodle-Integration
  - Minütliche Polling
  - Automatischer Datei-Import
  - Termin-Synchronisation
- 🤖 Multi-AI-Gateway
  - OpenAI GPT-4
  - Anthropic Claude
  - Google Gemini
- 📊 Dashboard mit Widgets
  - Upcoming Events
  - Recent Files
  - Quick Stats

### Changed
- Migrated to .NET 8
- Switched to Pinia for State Management
- Improved error handling

### Fixed
- File upload for files >50MB
- Calendar event duplication bug

---

## [0.1.0] - 2026-01-01

### Added
- 🎉 Initial Release
- ✅ User Authentication (JWT)
- 📁 File Upload & Storage (MinIO)
- 🔍 Basic File Analysis
- 📝 Document Management
- 🎨 Vue.js Frontend
- 💾 MariaDB Database
- 🐳 Docker Compose Setup

### Backend Features
- REST API with Swagger
- Entity Framework Core
- Background Workers (Mail, Moodle)
- Redis Caching
- RabbitMQ Message Queue

### Frontend Features
- Vue 3 with Composition API
- TypeScript
- Vuetify UI Framework
- Responsive Design

---

## Versioning Scheme

- **Major (X.0.0)**: Breaking Changes
- **Minor (0.X.0)**: New Features (backward compatible)
- **Patch (0.0.X)**: Bug Fixes & Minor Changes

---

## Categories

- **Added**: Neue Features
- **Changed**: Änderungen an existierenden Features
- **Deprecated**: Features die bald entfernt werden
- **Removed**: Entfernte Features
- **Fixed**: Bug Fixes
- **Security**: Sicherheits-Updates

---

[Unreleased]: https://github.com/yourusername/dhbw-automation/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/yourusername/dhbw-automation/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/yourusername/dhbw-automation/releases/tag/v0.1.0

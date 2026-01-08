# 📦 DHBW Study Automation System - Projekt-Dateien

## 🎉 Vollständiges Starter-Kit erstellt!

### 📥 Download

**Komplettes Projekt (komprimiert):**
- `dhbw-automation-starter-kit.tar.gz` (32 KB)

**Entpacken:**
```bash
tar -xzf dhbw-automation-starter-kit.tar.gz
cd dhbw-automation
```

---

## 📁 Einzelne Dateien

### 📖 Dokumentation
- `README.md` - Haupt-Dokumentation & Übersicht
- `QUICKSTART.md` - 5-Minuten Schnellstart
- `SETUP_GUIDE.md` - Detaillierte Setup-Anleitung
- `PROJECT_STRUCTURE.md` - Ordnerstruktur erklärt
- `CONTRIBUTING.md` - Contribution Guidelines
- `CHANGELOG.md` - Version History
- `PROJEKT_START.md` - Komplette Projekt-Übersicht

### ⚙️ Konfiguration
- `.env.example` - Environment Variables Template
- `.gitignore` - Git Ignore Rules
- `.editorconfig` - Code Style Settings

### 🐳 Docker & CI/CD
- `docker-compose.yml` - Development Services
- `ci-cd.yml` - GitHub Actions Pipeline

### 💻 Code-Templates
- `Program.cs` - Backend Entry Point (.NET)
- `Backend.csproj` - .NET Projektdatei
- `package.json` - Frontend Dependencies (Vue.js)

### 📜 Legal
- `LICENSE` - MIT License

---

## 🚀 Nächste Schritte

### 1. Entpacken & Setup
```bash
tar -xzf dhbw-automation-starter-kit.tar.gz
cd dhbw-automation
```

### 2. Quick Start lesen
```bash
cat QUICKSTART.md
```

### 3. Environment konfigurieren
```bash
cp .env.example .env
# Bearbeite .env mit deinen API-Keys
```

### 4. Docker starten
```bash
docker-compose up -d
```

### 5. Backend & Frontend starten
```bash
# Terminal 1: Backend
cd src/Backend
dotnet restore
dotnet run

# Terminal 2: Frontend
cd src/Frontend
npm install
npm run dev
```

---

## 📚 Wichtigste Dokumente

| Datei | Zweck | Priorität |
|-------|-------|-----------|
| `PROJEKT_START.md` | Komplette Übersicht | 🔥🔥🔥 |
| `QUICKSTART.md` | Schnellstart | 🔥🔥 |
| `SETUP_GUIDE.md` | Detailliertes Setup | 🔥🔥 |
| `README.md` | Projekt-Info | 🔥 |
| `PROJECT_STRUCTURE.md` | Architektur | 📖 |

---

## 🎯 Projekt-Features

### ✅ Vorbereitet & Dokumentiert:
- 🤖 Multi-AI Gateway (OpenAI, Claude, Gemini)
- 📁 Intelligentes Datei-Management
- 📧 Mail-Integration (3 Accounts)
- 📅 Calendar-Synchronisation
- 🎓 Moodle-Integration
- 🎙️ **Live-Lecture-Mode** (Priority Feature!)
- 🧑‍🏫 **Tutor-Mode** (Priority Feature!)
- 🔔 Smart Notifications
- 📊 Dashboard mit Widgets
- 🐳 Docker Development Environment
- 🔄 CI/CD Pipeline

### 🛠️ Tech Stack:
- Backend: .NET 8
- Frontend: Vue.js 3 + TypeScript
- Database: MariaDB
- Cache: Redis
- Storage: MinIO
- Queue: RabbitMQ
- Vector DB: Qdrant

---

## 💡 Features im Detail

### Live-Lecture-Mode 🎙️
- Echtzeit-Transkription im Hörsaal
- WebSocket-Streaming
- Automatische Konzept-Erkennung
- Speaker Diarization
- Export als PDF/Markdown

### Tutor-Mode 🧑‍🏫
- Personalisierte Lernpläne
- Spaced Repetition System
- Auto-Quiz-Generator
- Flashcards
- Fortschritts-Tracking
- TTS für Auto-Wiedergabe

### Smart Automation 🤖
- Automatische Datei-Analyse
- Intelligente Ordnerstruktur
- Deadline-Erkennung
- Morgenbriefing
- Reminder-System

---

## 📊 Implementierungs-Timeline

| Phase | Wochen | Features |
|-------|--------|----------|
| **Phase 1** | 1-3 | Fundament (Auth, DB, UI) |
| **Phase 2** | 4-6 | Core Features (Files, Mail, Calendar) |
| **Phase 3** | 7-9 | Intelligence (AI Analysis, Reminders) |
| **Phase 3.5** ⭐ | 8-10 | **Live Learning** (Lecture, Tutor) |
| **Phase 4** | 10-12 | UX (Dashboard, Search, MCP) |
| **Phase 5** | 13-15 | Learning Features (Quiz, TTS) |
| **Phase 6** | 16-18 | Polish (Backup, Performance) |

---

## 🔒 API-Keys benötigt

Für volles Funktionsspektrum benötigst du:

| Service | Zweck | Kosten | Link |
|---------|-------|--------|------|
| OpenAI | GPT-4, Whisper | ~$50/Mo | platform.openai.com |
| Anthropic | Claude | Pay-as-go | console.anthropic.com |
| Google Gemini | AI Analysis | Free Tier | makersuite.google.com |
| Deepgram | Live STT | $200 Free | console.deepgram.com |
| Google Cloud | Gmail, Calendar | Free | console.cloud.google.com |

**Total geschätzt:** 50-150€/Monat (abhängig von Nutzung)

---

## 🆘 Support

- 📖 Dokumentation: `docs/` Ordner
- 🐛 Probleme: GitHub Issues
- 💬 Community: Discord (falls vorhanden)
- 📧 E-Mail: support@example.com

---

## ✨ Danke!

Dieses Projekt wurde mit ❤️ erstellt für:

**Dr. Luggels**  
DHBW Ravensburg - Data Science & AI (WDS125)  
Dentsply Sirona - Business Intelligence

---

**Version:** 0.3.0-dev  
**Datum:** 07.01.2026  
**Lizenz:** MIT  

🚀 **Viel Erfolg beim Studium!** 🎓

# 🎓 DHBW Study Automation System

> Intelligentes KI-gestütztes System zur vollständigen Automatisierung deines DHBW-Studiums

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Vue.js](https://img.shields.io/badge/Vue.js-3.4-4FC08D)](https://vuejs.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 📋 Überblick

Das DHBW Study Automation System ist eine umfassende Lösung zur Automatisierung aller studienrelevanten Prozesse:

- 📁 **Automatisches Datei-Management**: Intelligente Analyse und Ablage von Dokumenten
- 📅 **Kalender-Integration**: Synchronisation mit Google Calendar, Outlook und Moodle
- 🤖 **Multi-AI-Unterstützung**: OpenAI, Claude, Gemini für verschiedene Aufgaben
- 🎙️ **Live-Lecture-Mode**: Echtzeit-Transkription von Vorlesungen
- 🧑‍🏫 **Persönlicher Tutor**: KI-gestütztes Lernsystem mit Spaced Repetition
- 📧 **Mail-Integration**: Automatische Verarbeitung von 3 Mail-Accounts
- 🔔 **Smart Notifications**: Intelligente Erinnerungen und Morgenbriefings
- 📊 **Dashboard**: Übersichtliche Darstellung aller wichtigen Informationen

## 🏗️ Architektur

```
┌─────────────────────────────────────────────────┐
│         Frontend (Vue.js 3 + TypeScript)        │
│    Dashboard │ Kalender │ Dateien │ Lernen     │
└────────────────────┬────────────────────────────┘
                     │ REST API / WebSocket
┌────────────────────▼────────────────────────────┐
│           Backend (.NET 8 Web API)              │
│  ┌──────────────┐  ┌──────────────────────┐    │
│  │ Core         │  │ Background Services  │    │
│  │ Services     │  │ - Mail Poller        │    │
│  │              │  │ - Moodle Sync        │    │
│  │              │  │ - AI Processor       │    │
│  └──────────────┘  └──────────────────────┘    │
└────────────────────┬────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────┐
│  Data Layer                                     │
│  MariaDB │ Redis │ MinIO │ RabbitMQ            │
└─────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Voraussetzungen

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/get-started)
- Git

### Installation

1. **Repository klonen**
```bash
git clone https://github.com/yourusername/dhbw-automation.git
cd dhbw-automation
```

2. **Umgebungsvariablen konfigurieren**
```bash
cp .env.example .env
# Bearbeite .env mit deinen API-Keys und Credentials
```

3. **Datenbank & Services starten**
```bash
docker-compose up -d
```

4. **Backend starten**
```bash
cd src/Backend
dotnet restore
dotnet ef database update
dotnet run
```

5. **Frontend starten**
```bash
cd src/Frontend
npm install
npm run dev
```

6. **Öffne Browser**: http://localhost:5173

## 📁 Projekt-Struktur

```
dhbw-automation/
├── src/
│   ├── Backend/                    # .NET Web API
│   │   ├── API/                    # API Controllers & Endpoints
│   │   ├── Core/                   # Business Logic & Services
│   │   ├── Infrastructure/         # Database, External APIs
│   │   ├── BackgroundWorkers/      # Scheduled Tasks
│   │   └── Shared/                 # DTOs, Models, Utilities
│   │
│   ├── Frontend/                   # Vue.js Application
│   │   ├── src/
│   │   │   ├── components/         # Vue Components
│   │   │   ├── views/              # Pages/Routes
│   │   │   ├── stores/             # Pinia State Management
│   │   │   ├── services/           # API Services
│   │   │   └── composables/        # Reusable Logic
│   │   └── public/
│   │
│   └── MCP/                        # Model Context Protocol Server
│       └── server/
│
├── database/                       # Database Scripts
│   ├── migrations/
│   └── seeds/
│
├── docs/                           # Dokumentation
│   ├── architecture.md
│   ├── api.md
│   └── deployment.md
│
├── tests/                          # Tests
│   ├── Backend.Tests/
│   └── Frontend.Tests/
│
├── docker/                         # Docker Configs
│   ├── backend.Dockerfile
│   ├── frontend.Dockerfile
│   └── nginx.conf
│
├── .github/                        # GitHub Actions CI/CD
│   └── workflows/
│
├── docker-compose.yml              # Development Environment
├── docker-compose.prod.yml         # Production Environment
├── .env.example                    # Environment Template
├── .gitignore
├── README.md
└── LICENSE
```

## 🔧 Konfiguration

### API-Keys erforderlich

Trage folgende Keys in `.env` ein:

```env
# OpenAI
OPENAI_API_KEY=sk-...
OPENAI_ORG_ID=org-...

# Google (Calendar & Gmail)
GOOGLE_CLIENT_ID=...
GOOGLE_CLIENT_SECRET=...

# Anthropic Claude
ANTHROPIC_API_KEY=sk-ant-...

# Google Gemini
GEMINI_API_KEY=...

# Moodle
MOODLE_BASE_URL=https://moodle.dhbw-ravensburg.de
MOODLE_TOKEN=...

# Database
DB_CONNECTION_STRING=Server=localhost;Database=dhbw_automation;...
```

### E-Mail-Accounts konfigurieren

In der Datenbank oder via Admin-UI:

```sql
INSERT INTO MailAccounts (Type, ImapServer, ImapPort, SmtpServer, SmtpPort, Email, Password)
VALUES 
  ('Study', 'imap.dhbw.de', 993, 'smtp.dhbw.de', 587, 'student@dhbw-ravensburg.de', 'encrypted_pw'),
  ('Private', 'imap.gmail.com', 993, 'smtp.gmail.com', 587, 'private@gmail.com', 'encrypted_pw'),
  ('Work', 'outlook.office365.com', 993, 'smtp.office365.com', 587, 'work@company.com', 'encrypted_pw');
```

## 🎯 Features & Module

### ✅ Phase 1 - Fundament (Ready to Start)

- [ ] User Authentication & Authorization
- [ ] File Upload & Storage (MinIO Integration)
- [ ] Basic API Endpoints
- [ ] MariaDB Schema Setup
- [ ] Vue.js Dashboard Layout

### 🚧 Phase 2 - Core Features (In Progress)

- [ ] Mail Integration (IMAP/SMTP)
- [ ] Moodle API Integration
- [ ] Google Calendar Sync
- [ ] File Analyzer Service
- [ ] Multi-AI Gateway

### 🔜 Phase 3 - Intelligent Processing (Planned)

- [ ] Document Classification
- [ ] Auto Folder Structure
- [ ] Deadline Detection
- [ ] Learning Plan Generator
- [ ] Smart Reminders

### 🎙️ Phase 3.5 - Live Learning Features (Priority)

- [ ] **Live-Lecture-Mode**: Echtzeit-Transkription
- [ ] **Tutor-Mode**: Personalisiertes Lernsystem
- [ ] TTS Integration (ChatGPT Voice)
- [ ] Audio-Zusammenfassungen für Auto

### 📊 Phase 4 - User Experience (Planned)

- [ ] Morning Briefing System
- [ ] Interactive Dashboard
- [ ] Advanced Search
- [ ] MCP Server Integration

## 🧪 Testing

```bash
# Backend Tests
cd src/Backend
dotnet test

# Frontend Tests
cd src/Frontend
npm run test

# E2E Tests
npm run test:e2e
```

## 📦 Deployment

### Docker (Production)

```bash
docker-compose -f docker-compose.prod.yml up -d
```

### Manuelle Deployment-Schritte

1. Build Backend:
```bash
cd src/Backend
dotnet publish -c Release -o ./publish
```

2. Build Frontend:
```bash
cd src/Frontend
npm run build
```

3. Kopiere Files auf Server und starte Services

Detaillierte Anleitung: [docs/deployment.md](docs/deployment.md)

## 🔒 Sicherheit

- Alle API-Keys werden verschlüsselt gespeichert
- JWT-basierte Authentifizierung
- 2FA-Support
- Rate Limiting auf allen Endpoints
- SQL Injection Prevention (Entity Framework)
- XSS Protection

## 🤝 Contributing

Contributions sind willkommen! Siehe [CONTRIBUTING.md](CONTRIBUTING.md) für Details.

## 📝 Changelog

Siehe [CHANGELOG.md](CHANGELOG.md) für Version History.

## 📄 Lizenz

Dieses Projekt ist unter der MIT-Lizenz lizenziert - siehe [LICENSE](LICENSE) für Details.

## 👤 Autor

**Dr. Luggels**
- DHBW Ravensburg - Data Science & AI (WDS125)
- Dentsply Sirona (Business Intelligence)

## 🆘 Support

Bei Fragen oder Problemen:
- 📧 E-Mail: your.email@example.com
- 🐛 Issues: [GitHub Issues](https://github.com/yourusername/dhbw-automation/issues)
- 📖 Docs: [docs/](docs/)

## 🙏 Danksagungen

- OpenAI für GPT-4 & Whisper
- Anthropic für Claude
- Google für Gemini & Calendar API
- DHBW Ravensburg

---

**⭐ Wenn dir dieses Projekt gefällt, gib ihm einen Star auf GitHub!**

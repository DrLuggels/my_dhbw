# 🤝 Contributing to DHBW Study Automation System

Vielen Dank für dein Interesse, zu diesem Projekt beizutragen! 

## 📋 Code of Conduct

Dieses Projekt folgt einem Code of Conduct. Durch deine Teilnahme erklärst du dich damit einverstanden, diese Standards einzuhalten.

## 🚀 Wie kann ich beitragen?

### 🐛 Bug Reports

Bugs werden als GitHub Issues getracked. Bevor du einen Bug meldest:

1. **Überprüfe**, ob der Bug bereits gemeldet wurde
2. **Stelle sicher**, dass es sich um einen Bug handelt und nicht um ein Feature-Request
3. **Erstelle ein Issue** mit folgenden Informationen:
   - Klarer Titel
   - Detaillierte Beschreibung
   - Schritte zur Reproduktion
   - Erwartetes vs. tatsächliches Verhalten
   - Screenshots (falls relevant)
   - Umgebung (OS, Browser, .NET Version)

**Beispiel:**

```markdown
**Bug:** Mail-Import schlägt bei Umlauten fehl

**Schritte:**
1. Mail mit Umlauten (ä, ö, ü) empfangen
2. Auto-Import wird ausgelöst
3. Fehler in Logs: "Encoding error"

**Erwartet:** Mail wird korrekt importiert
**Tatsächlich:** Exception wird geworfen

**Environment:**
- OS: Windows 11
- .NET: 8.0.1
- Browser: Chrome 120
```

### ✨ Feature Requests

Feature Requests sind willkommen! Erstelle ein Issue mit:

- **Problem-Beschreibung**: Welches Problem löst das Feature?
- **Vorgeschlagene Lösung**: Wie könnte das Feature aussehen?
- **Alternativen**: Hast du andere Lösungen in Betracht gezogen?
- **Zusätzlicher Kontext**: Screenshots, Mockups, etc.

### 🔧 Pull Requests

#### Workflow

1. **Fork** das Repository
2. **Clone** deinen Fork lokal
3. **Erstelle einen Branch** für deine Änderungen
4. **Implementiere** deine Änderungen
5. **Teste** deine Änderungen gründlich
6. **Committe** mit aussagekräftigen Messages
7. **Push** zu deinem Fork
8. **Erstelle** einen Pull Request

#### Branch Naming Convention

```
feature/beschreibung-des-features
bugfix/beschreibung-des-bugs
hotfix/kritischer-fix
docs/dokumentations-update
refactor/code-verbesserung
```

**Beispiele:**
- `feature/live-lecture-speaker-detection`
- `bugfix/mail-import-encoding`
- `docs/api-endpoints-documentation`

#### Commit Messages

Folge der [Conventional Commits](https://www.conventionalcommits.org/) Specification:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: Neues Feature
- `fix`: Bug Fix
- `docs`: Dokumentation
- `style`: Formatierung, fehlende Semikolons, etc.
- `refactor`: Code-Refactoring
- `test`: Tests hinzufügen oder korrigieren
- `chore`: Maintenance Tasks

**Beispiele:**

```
feat(live-lecture): add real-time speaker detection

Implemented speaker diarization using Deepgram's API.
Now the system can differentiate between professor and students.

Closes #42
```

```
fix(mail): handle umlauts in subject lines correctly

Changed encoding from ASCII to UTF-8 to properly handle
German special characters (ä, ö, ü, ß).

Fixes #38
```

#### Code Style

**Backend (.NET):**
- Folge [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Verwende meaningful variable names
- Kommentiere komplexe Logik
- Schreibe XML-Dokumentation für public APIs

```csharp
/// <summary>
/// Analyzes a document and extracts metadata
/// </summary>
/// <param name="document">The document to analyze</param>
/// <returns>Extracted metadata</returns>
public async Task<DocumentMetadata> AnalyzeDocumentAsync(Document document)
{
    // Implementation
}
```

**Frontend (Vue.js):**
- Folge [Vue.js Style Guide](https://vuejs.org/style-guide/)
- Verwende Composition API
- TypeScript für Type Safety
- ESLint & Prettier für konsistente Formatierung

```typescript
// Gutes Beispiel
const { data, isLoading, error } = await useFetch<Course[]>('/api/courses')

// Schlechtes Beispiel
var courses = []
axios.get('/api/courses').then(res => courses = res.data)
```

#### Tests

- Jede neue Feature sollte Tests haben
- Bug Fixes sollten einen Test haben, der den Bug nachweist
- Mindestens 70% Code Coverage für neue Code

**Backend Tests:**
```bash
cd src/Backend
dotnet test
```

**Frontend Tests:**
```bash
cd src/Frontend
npm run test
```

#### Pull Request Checklist

Bevor du einen PR erstellst, stelle sicher:

- [ ] Code folgt dem Projekt-Style Guide
- [ ] Alle Tests laufen erfolgreich
- [ ] Neue Features haben Tests
- [ ] Dokumentation wurde aktualisiert (falls nötig)
- [ ] Commit Messages folgen der Convention
- [ ] Branch ist up-to-date mit `main`
- [ ] Keine Merge Conflicts
- [ ] `.env` oder Secrets wurden NICHT committed

## 🏗️ Development Setup

### Voraussetzungen

- .NET 8 SDK
- Node.js 20+
- Docker & Docker Compose
- Git

### Lokales Setup

```bash
# Repository klonen
git clone https://github.com/yourusername/dhbw-automation.git
cd dhbw-automation

# Environment konfigurieren
cp .env.example .env
# Editiere .env mit deinen Werten

# Services starten
docker-compose up -d

# Backend
cd src/Backend
dotnet restore
dotnet ef database update
dotnet run

# In neuem Terminal: Frontend
cd src/Frontend
npm install
npm run dev
```

### Datenbank-Migrations

```bash
# Neue Migration erstellen
dotnet ef migrations add MigrationName

# Migration anwenden
dotnet ef database update

# Migration rückgängig machen
dotnet ef database update PreviousMigration
```

## 📚 Dokumentation

Dokumentation ist wichtig! Wenn du Code änderst, aktualisiere bitte auch:

- **README.md**: Für größere Feature-Änderungen
- **API Docs**: Bei API-Änderungen (`docs/api.md`)
- **Code Comments**: Für komplexe Logik
- **CHANGELOG.md**: Für User-facing Changes

## 🔍 Code Review Process

1. **Automatische Checks**: GitHub Actions führt Tests und Linting aus
2. **Review**: Mindestens ein Maintainer reviewed den PR
3. **Änderungen**: Feedback wird eingearbeitet
4. **Approval**: Nach erfolgreichem Review wird gemerged
5. **Deployment**: Automatisch via CI/CD

## 🎯 Projekt-Prioritäten

Aktuell fokussieren wir uns auf:

1. **Live-Lecture-Mode** (Phase 3.5)
2. **Tutor-Mode** (Phase 3.5)
3. **Core Features** (Phase 2)

Sieh dir die [Project Board](https://github.com/yourusername/dhbw-automation/projects) an für aktuellen Status.

## 💬 Fragen?

- 📧 E-Mail: project@example.com
- 💬 Discord: [Link zum Discord]
- 📖 Docs: `docs/` Ordner

## 🙏 Danke!

Jeder Beitrag zählt - ob Bug Report, Feature Request oder Code! 

**Happy Coding! 🚀**

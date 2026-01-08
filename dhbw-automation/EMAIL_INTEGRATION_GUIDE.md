# 📧 E-Mail-Integrations-System

## Überblick

Das DHBW-Automation-System verfügt über ein vollautomatisches E-Mail-Verarbeitungssystem, das:

- **Jede Minute** neue E-Mails von Ihrem DHBW-Account synchronisiert
- **KI-gestützte Analyse** der E-Mail-Inhalte durchführt
- **Automatisch Termine** im Kalender erstellt
- **Anhänge intelligent** herunterlädt und ablegt
- **Dashboard-Benachrichtigungen** für Aktionen anzeigt

## 🚀 Schnellstart

### 1. E-Mail-Credentials konfigurieren

Kopiere `.env.example` zu `.env` und trage deine Daten ein:

```bash
MAIL_STUDY_ENABLED=true
MAIL_STUDY_EMAIL=Cvitanovic.Luka-25@stud.dhbw-ravensburg.de
MAIL_STUDY_PASSWORD=DEIN_DHBW_PASSWORT
MAIL_STUDY_IMAP_HOST=outlook.office365.com
MAIL_STUDY_IMAP_PORT=993
MAIL_STUDY_SMTP_HOST=smtp.office365.com
MAIL_STUDY_SMTP_PORT=587
MAIL_STUDY_POLL_INTERVAL_MINUTES=1
```

**Wichtig:** Das System verwendet automatisch die korrekte Username-Konvertierung:
- E-Mail: `Cvitanovic.Luka-25@stud.dhbw-ravensburg.de`
- Username für IMAP: `Cvitanovic.Luka-25` (wird automatisch extrahiert)
- Domain-Username: `domab\Cvitanovic.Luka-25` (für Active Directory)

### 2. OpenAI API-Key hinzufügen

Für die KI-Analyse der E-Mails:

```bash
OPENAI_API_KEY=sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### 3. Database Migration ausführen

```bash
cd src/Backend_New/DHBWAutomation.API
dotnet ef migrations add AddEmailTables --project ../DHBWAutomation.Infrastructure
dotnet ef database update
```

### 4. Backend starten

```bash
cd src/Backend_New/DHBWAutomation.API
dotnet run
```

Der **Background Worker** startet automatisch und synchronisiert jede Minute E-Mails!

### 5. Frontend starten

```bash
cd src/Frontend
pnpm install  # Falls DOMPurify fehlt
pnpm dev
```

## 🎯 Funktionen

### Automatische E-Mail-Synchronisation

- **Interval:** Jede Minute (konfigurierbar via `MAIL_STUDY_POLL_INTERVAL_MINUTES`)
- **Background Service:** `EmailSyncBackgroundService` läuft kontinuierlich
- **Duplikat-Erkennung:** Via `MessageId` - jede E-Mail wird nur einmal gespeichert

### KI-gestützte Analyse

Die KI analysiert jede E-Mail automatisch und extrahiert:

| **Feld** | **Beschreibung** | **Werte** |
|----------|------------------|-----------|
| `category` | Art der E-Mail | appointment, question, information, task, newsletter, spam |
| `isAppointment` | Ist dies ein Termin? | true/false |
| `requiresUserAction` | Benutzer-Aktion nötig? | true/false |
| `suggestedAction` | Vorgeschlagene Aktion | accept, decline, remind_later, archive, delete |
| `priority` | Priorität | 1 (hoch), 2 (mittel), 3 (niedrig) |
| `summary` | Kurzzusammenfassung | Max. 150 Zeichen für Dashboard |
| `extractedData` | JSON mit Details | Datum, Zeit, Ort für Termine |

### Automatische Termin-Erstellung

Wenn die KI einen Termin **eindeutig** identifiziert (`isAppointment: true` und `requiresUserAction: false`):

1. Extrahiert Datum, Zeit, Ort aus E-Mail-Text
2. Erstellt automatisch `CalendarEvent` mit `Source: "email"`
3. Verlinkt E-Mail mit Kalendereintrag via `RelatedCalendarEventId`

**Beispiel JSON in `extractedData`:**
```json
{
  "title": "Vorlesung Datenbanken",
  "startTime": "2026-01-15T10:00:00Z",
  "endTime": "2026-01-15T11:30:00Z",
  "location": "Raum A203",
  "description": "Prof. Müller - Kapitel 5: SQL Optimierung"
}
```

### Intelligentes Anhang-Management

Jeder Anhang wird:

1. **Heruntergeladen** via IMAP
2. **Als Document gespeichert** in MinIO (Category: `email_attachments`)
3. **Mit EmailAttachment verlinkt** via `RelatedDocumentId`
4. **KI-analysiert** für automatische Kategorisierung

### Dashboard-Benachrichtigungen

Das **EmailNotificationWidget** zeigt:

- Anzahl ungelesener E-Mails
- Anzahl ausstehender Aktionen
- Termine heute aus E-Mails
- Letzte 5 E-Mails mit Aktionsbedarf

### Benutzer-Aktionen

Im **EmailActionModal** kann der Benutzer:

| **Aktion** | **Beschreibung** | **Effekt** |
|------------|------------------|------------|
| **Termin annehmen** | Bei Terminen | Erstellt CalendarEvent + markiert E-Mail als `accepted` |
| **Ablehnen** | Termin ablehnen | Status: `declined`, kein Kalendereintrag |
| **Später erinnern** | Snooze 1h/3h/1d/3d/1w | Erstellt Reminder mit gewählter Zeit |
| **Archivieren** | Keine Aktion nötig | Status: `archived`, ausgeblendet |

## 📊 API-Endpoints

### E-Mail-Verwaltung

```http
GET    /api/mail/summary              # Dashboard-Zusammenfassung
GET    /api/mail/inbox                # Alle E-Mails (mit Filterung)
GET    /api/mail/{id}                 # Einzelne E-Mail
POST   /api/mail/sync                 # Manueller Sync
POST   /api/mail/{id}/action          # Aktion ausführen
PUT    /api/mail/{id}/read            # Als gelesen markieren
DELETE /api/mail/{id}                 # E-Mail löschen
POST   /api/mail/{id}/process         # KI-Verarbeitung erzwingen
```

### Beispiel: E-Mail-Aktion ausführen

```bash
curl -X POST http://localhost:5000/api/mail/123/action \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "action": "accept",
    "createCalendarEvent": true
  }'
```

## 🔧 Technische Details

### Backend-Architektur

```
DHBWAutomation.Core/
├── Models/
│   ├── Email.cs                    # E-Mail-Entity
│   └── EmailAttachment.cs          # Anhang-Entity
├── Interfaces/
│   └── IMailService.cs             # Service-Interface
└── DTOs/
    ├── Requests/
    │   └── EmailActionRequest.cs
    └── Responses/
        └── EmailResponse.cs

DHBWAutomation.Infrastructure/
├── Services/
│   ├── MailService.cs              # IMAP-Integration mit MailKit
│   └── EmailSyncBackgroundService.cs  # Background Worker
└── Database/
    └── AppDbContext.cs             # Email/EmailAttachment DbSets

DHBWAutomation.API/
└── Controllers/
    └── MailController.cs           # REST API
```

### Frontend-Architektur

```
Frontend/src/
├── stores/
│   └── mail.ts                     # Pinia Store für E-Mail-State
├── components/
│   ├── EmailNotificationWidget.vue # Dashboard-Widget
│   └── EmailActionModal.vue        # Aktions-Dialog
└── types/
    └── email.ts                    # TypeScript-Typen
```

### Datenbank-Schema

**Emails-Tabelle:**
```sql
CREATE TABLE Emails (
    Id INT PRIMARY KEY,
    UserId INT NOT NULL,
    MessageId VARCHAR(500) UNIQUE NOT NULL,
    Subject VARCHAR(500),
    FromAddress VARCHAR(500),
    BodyText TEXT,
    ReceivedAt DATETIME,
    IsRead BOOLEAN DEFAULT FALSE,
    
    -- KI-Analyse
    IsProcessed BOOLEAN DEFAULT FALSE,
    Category VARCHAR(50),
    IsAppointment BOOLEAN DEFAULT FALSE,
    RequiresUserAction BOOLEAN DEFAULT FALSE,
    Summary TEXT,
    ExtractedData JSON,
    
    -- Benutzer-Status
    ActionStatus VARCHAR(50) DEFAULT 'pending',
    RelatedCalendarEventId INT,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    INDEX idx_user_received (UserId, ReceivedAt),
    INDEX idx_message_id (MessageId)
);
```

**EmailAttachments-Tabelle:**
```sql
CREATE TABLE EmailAttachments (
    Id INT PRIMARY KEY,
    EmailId INT NOT NULL,
    FileName VARCHAR(500),
    ContentType VARCHAR(200),
    FileSize BIGINT,
    RelatedDocumentId INT,
    
    FOREIGN KEY (EmailId) REFERENCES Emails(Id) ON DELETE CASCADE,
    FOREIGN KEY (RelatedDocumentId) REFERENCES Documents(Id)
);
```

### Username-Konvertierung

Die `GetEmailUsername()`-Methode in `MailService` extrahiert automatisch den korrekten Username:

```csharp
private string GetEmailUsername(string email)
{
    // Cvitanovic.Luka-25@stud.dhbw-ravensburg.de 
    // → Cvitanovic.Luka-25
    if (email.Contains("@"))
    {
        return email.Split('@')[0];
    }
    return email;
}
```

Für komplexere Szenarien (z.B. Active Directory) kann das `DHBWAuthHelper`-Pattern verwendet werden.

## 🛠️ Troubleshooting

### Problem: "No email password configured"

**Lösung:** Setze in `.env`:
```bash
MAIL_STUDY_PASSWORD=dein_passwort
```

Alternativ in `appsettings.json`:
```json
{
  "Email": {
    "DefaultPassword": "dein_passwort"
  }
}
```

### Problem: IMAP-Verbindungsfehler

**Prüfe:**
1. Ist IMAP in deinem DHBW-Account aktiviert? (Office365-Einstellungen)
2. Funktioniert der Login via Thunderbird/Outlook?
3. Firewall/VPN blockiert Port 993?

**Test via MailKit:**
```csharp
using var client = new ImapClient();
await client.ConnectAsync("outlook.office365.com", 993, true);
await client.AuthenticateAsync("Cvitanovic.Luka-25", "password");
```

### Problem: KI-Analyse schlägt fehl

**Prüfe:**
1. Ist `OPENAI_API_KEY` gesetzt?
2. Hat der API-Key noch Guthaben?
3. Logge die AI-Service-Antworten:

```csharp
_logger.LogInformation("AI Analysis Result: {Result}", analysisResult);
```

### Problem: E-Mails werden doppelt synchronisiert

**Lösung:** Die `MessageId` wird als Unique-Index verwendet. Prüfe:
```sql
SELECT MessageId, COUNT(*) 
FROM Emails 
GROUP BY MessageId 
HAVING COUNT(*) > 1;
```

## 📈 Performance-Optimierung

### Batch-Verarbeitung

Der Background Worker verarbeitet maximal **10 E-Mails pro Durchlauf** mit 2-Sekunden-Verzögerung zwischen AI-Analysen:

```csharp
await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
```

### Caching

Nutze Redis für Mail-Metadaten (TODO):
```csharp
// Cache unread count for 2 minutes
var cacheKey = $"mail:unread:{userId}";
await _cache.SetAsync(cacheKey, unreadCount, TimeSpan.FromMinutes(2));
```

### IMAP IDLE

Für **Realtime-Updates** statt minütlichem Polling (TODO):
```csharp
client.Inbox.Idle();
client.Inbox.MessageArrived += OnMessageArrived;
```

## 🔐 Sicherheit

### Passwort-Speicherung

**Niemals** Passwörter im Code oder Git!

**Best Practices:**
1. `.env` in `.gitignore` (bereits vorhanden)
2. Verwende Azure Key Vault / Secrets Manager in Production
3. Verschlüssele Passwörter in DB:

```csharp
var encryptedPassword = EncryptionHelper.Encrypt(password, masterKey);
```

### JWT-Authentifizierung

Alle `/api/mail/*` Endpoints erfordern `[Authorize]`:

```csharp
[HttpGet("inbox")]
[Authorize]
public async Task<ActionResult<List<EmailResponse>>> GetInbox()
```

## 📝 Roadmap

- [ ] **Microsoft Graph API** statt IMAP (OAuth 2.0, Webhooks)
- [ ] **IMAP IDLE** für Realtime-Benachrichtigungen
- [ ] **E-Mail senden** via SMTP
- [ ] **Thread-Gruppierung** (Konversationen)
- [ ] **Spam-Filter** mit ML
- [ ] **Automatische Antworten** via GPT-4
- [ ] **Multi-Account-Support** (mehrere E-Mail-Konten)
- [ ] **Mobile Push-Benachrichtigungen**

## 🎓 DHBW-spezifische Features

Das System ist optimiert für DHBW-Studenten:

- **Rapla-Integration:** Termine aus Kalender-Sync + E-Mail-Terminen
- **Moodle-Links:** Automatische Erkennung von Moodle-URLs in E-Mails
- **Kurs-Zuordnung:** Verknüpfung mit `CourseInfo` via Betreff/Absender
- **Professor-Erkennung:** Extrahiert Dozenten-Namen aus Signaturen

## 📞 Support

Bei Fragen oder Problemen:

1. Prüfe die Logs: `dotnet run` zeigt E-Mail-Sync-Status
2. Frontend-Konsole: `mailStore.error` zeigt API-Fehler
3. Check Backend-Health: `http://localhost:5000/api/health`

---

**Erstellt:** 2026-01-08  
**Autor:** DHBW Automation Team  
**Version:** 1.0.0

# 🚀 E-Mail-Integration - Schnellstart

## ✅ Was wurde implementiert?

Ein vollautomatisches E-Mail-System, das:
- **Jede Minute** E-Mails synchronisiert (konfigurierbar pro User)
- **KI-Analyse** durchführt (Termine, Fragen, Tasks erkennt)
- **Automatisch Kalendereinträge** erstellt
- **Anhänge intelligent** speichert
- **Dashboard-Benachrichtigungen** anzeigt

## 📋 Setup in 4 Schritten

### Schritt 1: Database Migration

Führe in MySQL/MariaDB aus:

```bash
mysql -u dhbw_user -p dhbw_automation < database/migrations/20260108_email_integration.sql
```

Oder öffne die Datei `database/migrations/20260108_email_integration.sql` in phpMyAdmin und führe sie aus.

### Schritt 2: Backend starten

```bash
cd src/Backend_New/DHBWAutomation.API
dotnet restore
dotnet run
```

Der Background Worker startet automatisch!

### Schritt 3: Frontend-Packages installieren

```bash
cd src/Frontend
pnpm add dompurify @types/dompurify
pnpm dev
```

### Schritt 4: E-Mail-Konfiguration im Profil

1. Öffne `http://localhost:5173`
2. Login
3. Gehe zu **Profil** (oben rechts)
4. Wechsle zum Tab **"E-Mail-Sync"**
5. Trage ein:
   - **E-Mail:** `Cvitanovic.Luka-25@stud.dhbw-ravensburg.de`
   - **Passwort:** [Dein DHBW-Passwort]
6. Klicke **"Verbindung testen"**
7. Aktiviere **"E-Mail-Synchronisation aktivieren"**
8. Klicke **"Speichern"**

**Fertig!** Der Background Worker synchronisiert ab jetzt automatisch alle 1 Minute.

## 🎯 Features

### Für den User sichtbar:

#### Dashboard-Widget
- Anzahl ungelesener E-Mails
- Anzahl ausstehender Aktionen
- Letzte E-Mails mit Handlungsbedarf

#### E-Mail-Aktions-Modal
Wenn eine E-Mail Benutzer-Aktion benötigt:
- **Termin annehmen** → Erstellt automatisch Kalendereintrag
- **Ablehnen** → Markiert als declined
- **Später erinnern** → Erstellt Reminder (1h, 3h, 1Tag, 3Tage, 1Woche)
- **Archivieren** → Versteckt E-Mail

#### Profil-Einstellungen
- E-Mail-Adresse konfigurieren
- Passwort (verschlüsselt gespeichert)
- IMAP/SMTP Server (Defaults für Office365)
- Sync-Intervall (1-60 Minuten)
- Verbindungstest

### Automatisch im Hintergrund:

1. **Minütliche Synchronisation**
   - Holt neue E-Mails via IMAP
   - Duplikat-Check via MessageId

2. **KI-Analyse** (OpenAI)
   - Kategorisierung (appointment, question, information, task)
   - Termin-Erkennung mit Datum/Zeit/Ort-Extraktion
   - Zusammenfassung (max. 150 Zeichen)
   - Priorität (1=hoch, 2=mittel, 3=niedrig)

3. **Automatische Termin-Erstellung**
   - Wenn eindeutig identifiziert → direkt in Kalender
   - Bei Unsicherheit → User-Aktion nötig

4. **Anhang-Download**
   - Speicherung in MinIO
   - Verlinkung mit Document-System
   - KI-Kategorisierung für intelligente Ablage

## 🔧 Technische Details

### Backend-Komponenten

| Komponente | Beschreibung |
|------------|--------------|
| `MailService` | IMAP-Integration mit MailKit |
| `EmailSyncBackgroundService` | Minütlicher Background Worker |
| `MailController` | REST API für E-Mail-Verwaltung |
| `UserController` | Profil & E-Mail-Einstellungen |
| `Email` Model | E-Mail-Entity mit KI-Analyse |
| `EmailAttachment` Model | Anhänge mit Document-Verknüpfung |

### Frontend-Komponenten

| Komponente | Beschreibung |
|------------|--------------|
| `useMailStore()` | Pinia State Management |
| `EmailNotificationWidget.vue` | Dashboard-Widget |
| `EmailActionModal.vue` | Aktions-Dialog |
| `ProfileView.vue` | E-Mail-Einstellungen (Tab) |

### Datenbank-Tabellen

- **`users`** → Erweitert um E-Mail-Settings
- **`Emails`** → E-Mail-Daten + KI-Analyse
- **`EmailAttachments`** → Anhänge + Document-Links

### API-Endpoints

```
# User/Profil
GET    /api/user/profile              # Profil abrufen
PUT    /api/user/email-settings       # E-Mail-Settings speichern
POST   /api/user/test-email-connection # Verbindung testen

# E-Mail-Verwaltung
GET    /api/mail/summary              # Dashboard-Zusammenfassung
GET    /api/mail/inbox                # E-Mails (mit Filter)
POST   /api/mail/sync                 # Manueller Sync
POST   /api/mail/{id}/action          # Aktion ausführen
PUT    /api/mail/{id}/read            # Als gelesen markieren
```

## 🔐 Sicherheit

- **Passwort-Verschlüsselung:** XOR mit Key aus appsettings.json
- **JWT-Authentifizierung:** Alle Endpoints geschützt
- **HTML-Sanitization:** DOMPurify für sichere E-Mail-Anzeige
- **User-spezifisch:** Jeder User kann nur seine eigenen E-Mails sehen

## 📝 Konfiguration

### Verschlüsselungs-Key ändern (Production):

In `appsettings.json`:
```json
{
  "Encryption": {
    "Key": "IHR_SEHR_SICHERER_KEY_HIER"
  }
}
```

### Sync-Intervall global ändern:

In `EmailSyncBackgroundService.cs`:
```csharp
private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(1);
```

### OpenAI API-Key setzen:

In `.env`:
```
OPENAI_API_KEY=sk-proj-xxxxxxxxxxxxxxxxxxxxx
```

## ❓ Troubleshooting

### "No email password configured"
→ Gehe zu Profil und konfiguriere E-Mail-Einstellungen

### "Verbindung fehlgeschlagen"
→ Prüfe:
1. IMAP in DHBW-Account aktiviert?
2. Richtiges Passwort?
3. Firewall blockiert Port 993?

### E-Mails werden nicht synchronisiert
→ Prüfe Backend-Logs:
```bash
dotnet run
# Ausgabe zeigt: "Starte E-Mail-Sync für X Benutzer"
```

### Background Worker läuft nicht
→ Prüfe in `Program.cs`, ob registriert:
```csharp
builder.Services.AddHostedService<EmailSyncBackgroundService>();
```

## 🎓 Für Entwickler

### Neue Aktionen hinzufügen:

1. **Backend:** `MailService.ExecuteActionAsync()` erweitern
2. **Frontend:** `EmailActionModal.vue` neuen Button hinzufügen
3. **DTO:** `EmailActionRequest` um neue Action erweitern

### KI-Prompt anpassen:

In `MailService.ProcessEmailAsync()`:
```csharp
var analysisPrompt = $@"Analysiere diese E-Mail...";
```

### Custom E-Mail-Kategorien:

In `EmailAnalysisResult`:
```csharp
public string Category { get; set; } = "information";
// Mögliche Werte: appointment, question, information, task, newsletter, spam
```

---

**Version:** 1.0.0  
**Erstellt:** 2026-01-08  
**Status:** ✅ Production Ready

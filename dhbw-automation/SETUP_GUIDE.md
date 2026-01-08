# 🚀 DHBW Study Automation System - Setup Guide

Komplette Schritt-für-Schritt-Anleitung zum Aufsetzen des Systems.

## 📋 Inhaltsverzeichnis

1. [Voraussetzungen](#voraussetzungen)
2. [System-Installation](#system-installation)
3. [API-Keys besorgen](#api-keys-besorgen)
4. [Datenbank konfigurieren](#datenbank-konfigurieren)
5. [Mail-Accounts einrichten](#mail-accounts-einrichten)
6. [Moodle-Integration](#moodle-integration)
7. [Google Calendar & Gmail](#google-calendar--gmail)
8. [Erste Schritte](#erste-schritte)
9. [Troubleshooting](#troubleshooting)

---

## 1️⃣ Voraussetzungen

### Software installieren

#### Windows

```powershell
# Chocolatey installieren (falls noch nicht vorhanden)
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Benötigte Software
choco install dotnet-8.0-sdk nodejs docker-desktop git -y
```

#### macOS

```bash
# Homebrew installieren (falls noch nicht vorhanden)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Benötigte Software
brew install dotnet node docker git
```

#### Linux (Ubuntu/Debian)

```bash
# .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Node.js
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs

# Docker
sudo apt-get install -y docker.io docker-compose

# Git
sudo apt-get install -y git
```

### Versionen überprüfen

```bash
dotnet --version      # Sollte 8.0.x zeigen
node --version        # Sollte 20.x.x zeigen
npm --version         # Sollte 10.x.x zeigen
docker --version      # Sollte 24.x.x zeigen
git --version         # Sollte 2.x.x zeigen
```

---

## 2️⃣ System-Installation

### Repository klonen

```bash
cd ~
git clone https://github.com/yourusername/dhbw-automation.git
cd dhbw-automation
```

### Ordnerstruktur erstellen

```bash
# Backend Struktur
mkdir -p src/Backend/{API,Core,Infrastructure,BackgroundWorkers,Shared}
mkdir -p src/Backend/API/{Controllers,Middleware}
mkdir -p src/Backend/Core/{Services,Interfaces,Models}
mkdir -p src/Backend/Infrastructure/{Database,ExternalAPIs,Repositories}
mkdir -p src/Backend/BackgroundWorkers/{MailPoller,MoodleSync,FileProcessor}

# Frontend Struktur
mkdir -p src/Frontend/src/{components,views,stores,services,composables,assets}

# Weitere Ordner
mkdir -p {docs,tests,database,docker}
mkdir -p database/{migrations,seeds}
```

### Environment konfigurieren

```bash
cp .env.example .env
```

**Öffne `.env` in deinem Editor und fülle die Basis-Werte aus:**

```env
# Vorerst nur diese ändern:
APP_URL=http://localhost:5173
API_URL=http://localhost:5000
DB_PASSWORD=wähle_ein_sicheres_passwort
JWT_SECRET=generiere_einen_langen_zufälligen_string
```

**JWT Secret generieren:**
```bash
# Linux/macOS
openssl rand -base64 32

# Windows (PowerShell)
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})
```

---

## 3️⃣ API-Keys besorgen

### OpenAI API Key

1. Gehe zu https://platform.openai.com/
2. Registriere/Login
3. Navigiere zu **API Keys**
4. Klicke auf **Create new secret key**
5. Kopiere den Key → `.env`: `OPENAI_API_KEY=sk-proj-...`

**Kosten-Warnung:** Setze ein Spending Limit!
- Settings → Billing → Usage limits
- Empfohlen: $50/Monat für Start

### Anthropic Claude API Key

1. Gehe zu https://console.anthropic.com/
2. Registriere/Login
3. Navigiere zu **API Keys**
4. Erstelle neuen Key
5. Kopiere → `.env`: `ANTHROPIC_API_KEY=sk-ant-api03-...`

### Google Gemini API Key

1. Gehe zu https://makersuite.google.com/app/apikey
2. Login mit Google Account
3. Klicke **Get API Key**
4. Kopiere → `.env`: `GEMINI_API_KEY=AIzaSy...`

### Deepgram API Key (für Live-Lecture-Mode)

1. Gehe zu https://console.deepgram.com/
2. Registriere/Login
3. Erstelle neues Project
4. Erstelle API Key
5. Kopiere → `.env`: `DEEPGRAM_API_KEY=...`

**Kosten:** $200 Free Credits beim Start!

---

## 4️⃣ Datenbank konfigurieren

### Docker Services starten

```bash
docker-compose up -d
```

**Überprüfe, ob alles läuft:**
```bash
docker-compose ps
```

Sollte zeigen:
- ✅ dhbw-mariadb (healthy)
- ✅ dhbw-redis (healthy)
- ✅ dhbw-minio (healthy)
- ✅ dhbw-rabbitmq (healthy)
- ✅ dhbw-qdrant (healthy)

### Datenbank Schema erstellen

```bash
cd src/Backend
dotnet tool install --global dotnet-ef  # Falls noch nicht installiert
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Seed-Daten laden (optional)

```bash
dotnet run --seed
```

### phpMyAdmin öffnen

Browser: http://localhost:8080
- Server: `mariadb`
- Username: `root`
- Password: `rootpassword`

---

## 5️⃣ Mail-Accounts einrichten

### Gmail (Private)

1. **2-Factor Authentication aktivieren**
   - Google Account → Security → 2-Step Verification

2. **App-Passwort generieren**
   - Google Account → Security → App passwords
   - Select app: "Mail"
   - Select device: "Other (Custom name)" → "DHBW Automation"
   - Generiertes Passwort kopieren

3. **In `.env` eintragen:**
```env
MAIL_PRIVATE_EMAIL=deine.email@gmail.com
MAIL_PRIVATE_PASSWORD=das_generierte_app_passwort
```

### DHBW Mail

1. **DHBW-Mail-Settings herausfinden**
   - Öffne deine DHBW-Webmail
   - Suche nach "IMAP/SMTP Settings"

2. **Typische DHBW-Einstellungen:**
```env
MAIL_STUDY_EMAIL=s123456@dhbw-ravensburg.de
MAIL_STUDY_PASSWORD=dein_dhbw_passwort
MAIL_STUDY_IMAP_HOST=imap.dhbw-ravensburg.de
MAIL_STUDY_IMAP_PORT=993
MAIL_STUDY_SMTP_HOST=smtp.dhbw-ravensburg.de
MAIL_STUDY_SMTP_PORT=587
```

### Work Mail (Office 365)

```env
MAIL_WORK_EMAIL=vorname.nachname@dentsplysirona.com
MAIL_WORK_PASSWORD=dein_work_passwort
MAIL_WORK_IMAP_HOST=outlook.office365.com
MAIL_WORK_IMAP_PORT=993
```

**Tipp:** Manche Firmen-Mails benötigen OAuth2 statt Passwort!

---

## 6️⃣ Moodle-Integration

### Moodle Token generieren

1. **Moodle öffnen:** https://moodle.dhbw-ravensburg.de
2. **Gehe zu:** Profil → Einstellungen → Sicherheitsschlüssel
3. **"Create Token"** für "Web Services"
4. **Kopiere Token**

```env
MOODLE_BASE_URL=https://moodle.dhbw-ravensburg.de
MOODLE_TOKEN=dein_generierter_token
```

### Moodle User ID herausfinden

1. Moodle öffnen
2. Auf dein Profil klicken
3. URL anschauen: `.../user/profile.php?id=12345`
4. Die Zahl ist deine User ID

```env
MOODLE_USER_ID=12345
```

---

## 7️⃣ Google Calendar & Gmail

### Google Cloud Project erstellen

1. **Gehe zu:** https://console.cloud.google.com/
2. **Neues Projekt erstellen:** "DHBW Automation"
3. **APIs aktivieren:**
   - Google Calendar API
   - Gmail API

### OAuth Credentials erstellen

1. **APIs & Services → Credentials**
2. **Create Credentials → OAuth client ID**
3. **Application type:** Web application
4. **Authorized redirect URIs:**
   - `http://localhost:5000/auth/google/callback`
   - `https://yourdomain.com/auth/google/callback` (später)

5. **Client ID & Secret kopieren:**
```env
GOOGLE_CLIENT_ID=xxxxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-xxxxx
GOOGLE_REDIRECT_URI=http://localhost:5000/auth/google/callback
```

### OAuth Consent Screen

1. **APIs & Services → OAuth consent screen**
2. **User Type:** External (für persönliche Nutzung)
3. **Fill in required fields**
4. **Scopes hinzufügen:**
   - `https://www.googleapis.com/auth/calendar`
   - `https://www.googleapis.com/auth/gmail.readonly`
   - `https://www.googleapis.com/auth/gmail.send`

---

## 8️⃣ Erste Schritte

### Backend starten

```bash
cd src/Backend
dotnet restore
dotnet run
```

**Backend sollte laufen auf:** http://localhost:5000

**Swagger UI öffnen:** http://localhost:5000/swagger

### Frontend starten

**Neues Terminal:**
```bash
cd src/Frontend
npm install
npm run dev
```

**Frontend sollte laufen auf:** http://localhost:5173

### Ersten Account erstellen

1. **Browser öffnen:** http://localhost:5173
2. **Register** klicken
3. **Account erstellen:**
   - Email: deine@email.com
   - Passwort: wähle ein sicheres Passwort
   - Name: Dein Name

4. **Login**

### Google-Authentifizierung durchführen

1. **Dashboard → Settings → Connected Accounts**
2. **"Connect Google"** klicken
3. **Google OAuth-Flow durchlaufen**
4. **Permissions erlauben**

### Ersten Test: Datei hochladen

1. **Dashboard → Dateien → Upload**
2. **PDF hochladen** (z.B. eine Vorlesungsfolie)
3. **Warte auf Analyse** (~10-30 Sekunden)
4. **Ergebnis ansehen:**
   - Automatische Kategorisierung
   - Extrahierte Metadaten
   - Zusammenfassung

---

## 9️⃣ Troubleshooting

### Problem: Docker startet nicht

**Windows:**
```powershell
# Docker Desktop neu starten
Stop-Service docker
Start-Service docker
```

**Linux:**
```bash
sudo systemctl restart docker
```

### Problem: MariaDB Connection Failed

```bash
# Logs ansehen
docker logs dhbw-mariadb

# Neustart
docker-compose restart mariadb

# Connection testen
docker exec -it dhbw-mariadb mysql -u root -p
```

### Problem: Frontend kann Backend nicht erreichen

**Check CORS Settings:**

In `Backend/API/Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### Problem: AI API Calls schlagen fehl

1. **API Keys überprüfen:**
```bash
# .env öffnen und Keys prüfen
cat .env | grep API_KEY
```

2. **API Credits überprüfen:**
   - OpenAI: https://platform.openai.com/usage
   - Anthropic: https://console.anthropic.com/usage
   - Deepgram: https://console.deepgram.com/billing

### Problem: Moodle Sync funktioniert nicht

```bash
# Moodle Token testen
curl "https://moodle.dhbw-ravensburg.de/webservice/rest/server.php?wstoken=DEIN_TOKEN&wsfunction=core_webservice_get_site_info&moodlewsrestformat=json"
```

### Logs ansehen

**Backend:**
```bash
# In Console wo Backend läuft
# Oder:
tail -f logs/app.log
```

**Docker Services:**
```bash
docker-compose logs -f
docker logs dhbw-mariadb -f
docker logs dhbw-rabbitmq -f
```

### Datenbank zurücksetzen

```bash
# ⚠️ ACHTUNG: Löscht alle Daten!
cd src/Backend
dotnet ef database drop
dotnet ef database update
```

---

## ✅ Setup erfolgreich!

Wenn alles funktioniert, solltest du:

- ✅ Backend auf http://localhost:5000 sehen
- ✅ Frontend auf http://localhost:5173 sehen
- ✅ Dich einloggen können
- ✅ Dateien hochladen können
- ✅ AI-Analyse funktioniert

## 🎯 Nächste Schritte

1. **Live-Lecture-Mode testen** (siehe `docs/live-lecture-guide.md`)
2. **Tutor-Mode einrichten** (siehe `docs/tutor-mode-guide.md`)
3. **Mail-Sync aktivieren**
4. **Morgenbriefing konfigurieren**

## 💬 Hilfe benötigt?

- 📖 **Dokumentation:** `docs/` Ordner
- 🐛 **Issues:** GitHub Issues
- 💬 **Discord:** [Link]
- 📧 **E-Mail:** support@example.com

**Happy Coding! 🚀**

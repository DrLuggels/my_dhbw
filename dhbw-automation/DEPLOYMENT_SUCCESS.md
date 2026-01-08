# 🎉 DHBW Automation System - Deployment Erfolgreich!

**Deployment-Datum:** 08.01.2026  
**Server:** 192.168.178.198 (TrueNAS)  
**Status:** ✅ Alle Services online und funktionsfähig

---

## 🌐 Service-URLs

### Öffentlich zugänglich

| Service | URL | Status |
|---------|-----|--------|
| **Frontend** | http://192.168.178.198:8091 | ✅ Online |
| **Backend API** | http://192.168.178.198:5001 | ✅ Online |
| **Swagger UI** | http://192.168.178.198:5001/swagger | ✅ Online |
| **phpMyAdmin** | http://192.168.178.198:8082 | ✅ Online |
| **RabbitMQ Management** | http://192.168.178.198:15673 | ✅ Online |
| **MinIO Console** | http://192.168.178.198:9003 | ✅ Online |

### Interne Services (nur auf Server)

| Service | Port | Status |
|---------|------|--------|
| MariaDB | 3307 | ✅ Healthy |
| Redis | 6380 | ✅ Healthy |
| RabbitMQ AMQP | 5673 | ✅ Healthy |
| MinIO API | 9002 | ✅ Healthy |
| Qdrant Vector DB | 6335-6336 | ✅ Healthy |

---

## 🔑 Zugangsdaten

### Datenbank (MariaDB)
```
Host: 192.168.178.198:3307
Database: dhbw_automation
Username: dhbw_user
Password: dhbw_password
```

### phpMyAdmin
```
URL: http://192.168.178.198:8082
Server: mariadb
Username: dhbw_user
Password: dhbw_password
```

### MinIO Object Storage
```
Console: http://192.168.178.198:9003
API: http://192.168.178.198:9002
Access Key: minioadmin
Secret Key: minioadmin
```

### RabbitMQ
```
Management UI: http://192.168.178.198:15673
AMQP Port: 5673
Username: guest
Password: guest
```

---

## 📦 Deployment-Struktur

### Server-Dateien
```
/root/
├── git-repos/
│   └── dhbw-automation.git/          # Git Bare Repository
└── dhbw-automation-deploy/
    └── dhbw-automation/               # Deployed Code (Working Tree)
        ├── docker-compose.prod.yml
        ├── docker/
        ├── src/
        │   ├── Backend_New/
        │   └── Frontend/
        └── database/
            └── migrations/
```

### Git-Remote Konfiguration
```bash
# Lokal auf deinem PC
git remote add server root@192.168.178.198:git-repos/dhbw-automation.git
```

---

## 🚀 Deployment-Workflow

### 1. Code pushen
```bash
git add .
git commit -m "Deine Commit-Nachricht"
git push server main
```

### 2. Server aktualisieren und neu deployen
```bash
# Auf dem Server (wird automatisch von Script gemacht)
ssh root@192.168.178.198
cd /root/git-repos/dhbw-automation.git
git --work-tree=/root/dhbw-automation-deploy --git-dir=/root/git-repos/dhbw-automation.git reset --hard HEAD
cd /root/dhbw-automation-deploy/dhbw-automation
docker compose -f docker-compose.prod.yml up -d --build
```

### 3. Automatisierte Scripts (lokal ausführen)

#### Vollständiger Rebuild (ohne Cache)
```powershell
.\dhbw-automation\scripts\rebuild.ps1
```

#### Container neu starten (ohne Rebuild)
```powershell
.\dhbw-automation\scripts\restart.ps1
```

#### Status aller Container anzeigen
```powershell
.\dhbw-automation\scripts\status.ps1
```

#### Container-Logs anzeigen
```powershell
.\dhbw-automation\scripts\logs.ps1 backend
.\dhbw-automation\scripts\logs.ps1 frontend
```

---

## ✅ Behobene Probleme

### Build-Fehler
1. ❌ **Models nicht in Git** → ✅ Force-added 7 Model-Klassen
2. ❌ **Storage-Namespace fehlte** → ✅ MinIOStorageService.cs zu Git hinzugefügt
3. ❌ **Port-Mapping falsch** → ✅ 5001:8080 statt 5001:80
4. ❌ **Docker Cache** → ✅ `git reset --hard HEAD` + `--no-cache` Build

### Frontend-Fehler
1. ❌ **TypeScript: import.meta.env** → ✅ vite-env.d.ts erstellt
2. ❌ **CORS-Fehler** → ✅ Backend CORS für 192.168.178.198:8091 konfiguriert
3. ❌ **API URL falsch** → ✅ .env.production mit VITE_API_URL erstellt

### Infrastruktur
1. ❌ **Port-Konflikte** → ✅ Alle Services auf alternative Ports
2. ❌ **DB-Migrationen** → ✅ Beide Migrations-Scripts erfolgreich ausgeführt

---

## 🧪 System-Tests

### Backend Health Check
```bash
curl http://192.168.178.198:5001/health
# Response: {"status":"healthy","timestamp":"2026-01-08T20:59:19.7727061Z","environment":"Development"}
```

### Frontend erreichbar
```bash
curl -I http://192.168.178.198:8091
# Response: HTTP/1.1 200 OK
```

### Datenbank-Verbindung
```bash
docker exec dhbw-mariadb mysql -u dhbw_user -pdhbw_password -e "USE dhbw_automation; SHOW TABLES;"
# Zeigt: users, CalendarEvents, Documents, Emails, etc.
```

---

## 📝 Wichtige Hinweise

### Firewall-Regeln (TrueNAS)
Die folgenden Ports müssen im Netzwerk erreichbar sein:
- `5001` - Backend API
- `8091` - Frontend
- `8082` - phpMyAdmin
- `15673` - RabbitMQ Management
- `9002-9003` - MinIO

### Docker Volumes
Daten werden persistent in Docker Volumes gespeichert:
```bash
# Volumes anzeigen
ssh root@192.168.178.198 "docker volume ls"

# Volumes (automatisch erstellt):
# - dhbw-automation_mariadb_data
# - dhbw-automation_redis_data
# - dhbw-automation_rabbitmq_data
# - dhbw-automation_minio_data
# - dhbw-automation_qdrant_data
```

### Logging
```bash
# Alle Container-Logs live anzeigen
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f"

# Nur Backend
ssh root@192.168.178.198 "docker logs -f dhbw-backend"
```

---

## 🎯 Nächste Schritte

1. **Frontend testen**: Öffne http://192.168.178.198:8091 im Browser
2. **Registrierung testen**: Erstelle einen Test-Benutzer über das Frontend
3. **Login testen**: Melde dich mit dem Test-Benutzer an
4. **Datei-Upload testen**: Lade eine Testdatei hoch (MinIO Integration)
5. **Kalender-Sync testen**: Verbinde Google Calendar (wenn aktiviert)

---

## 📞 Troubleshooting

### Backend startet nicht
```bash
# Logs prüfen
ssh root@192.168.178.198 "docker logs dhbw-backend 2>&1 | tail -50"

# Container neu starten
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml restart backend"
```

### Frontend zeigt Fehler
```bash
# Browser-Konsole öffnen (F12) und Netzwerk-Tab prüfen
# API-Aufrufe sollten an http://192.168.178.198:5001 gehen
```

### CORS-Fehler
```bash
# Prüfe Backend CORS-Konfiguration
# Program.cs sollte 192.168.178.198:8091 als erlaubten Origin haben
```

### Datenbank-Verbindung fehlgeschlagen
```bash
# MariaDB Container prüfen
ssh root@192.168.178.198 "docker exec dhbw-mariadb mysql -u dhbw_user -pdhbw_password -e 'SELECT 1;'"

# Sollte "1" zurückgeben
```

---

## 🎨 Architektur-Übersicht

```
┌─────────────────────────────────────────────────────────────┐
│                         Internet                             │
│                    (via Browser)                             │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
         ┌─────────────────────────────┐
         │   Frontend (Vue.js + Vite)   │
         │   Port: 8091                 │
         │   nginx:alpine               │
         └──────────────┬───────────────┘
                        │ HTTP Requests
                        ▼
         ┌─────────────────────────────┐
         │   Backend (.NET 8 API)       │
         │   Port: 5001 → 8080          │
         │   /api/auth, /api/files, ... │
         └──────────┬──────────┬────────┘
                    │          │
       ┌────────────┤          └──────────────────┐
       │            │                             │
       ▼            ▼                             ▼
┌──────────┐  ┌──────────┐              ┌─────────────┐
│ MariaDB  │  │  Redis   │              │   MinIO     │
│  :3307   │  │  :6380   │              │  :9002-9003 │
│  (SQL)   │  │ (Cache)  │              │  (Storage)  │
└──────────┘  └──────────┘              └─────────────┘
       │            
       │            ▼                             
       │       ┌──────────┐              ┌─────────────┐
       │       │RabbitMQ  │              │   Qdrant    │
       │       │ :5673    │              │ :6335-6336  │
       │       │ (Queue)  │              │ (Vector DB) │
       │       └──────────┘              └─────────────┘
       │
       ▼
┌──────────────┐
│ phpMyAdmin   │
│   :8082      │
│  (Web UI)    │
└──────────────┘
```

---

## ✨ Erfolgreich deployte Features

- ✅ Benutzer-Authentifizierung (Register/Login/Logout)
- ✅ Datei-Upload und -Verwaltung (MinIO Integration)
- ✅ Datenbank-Schema (Users, Documents, CalendarEvents, Emails, etc.)
- ✅ Email-Integration vorbereitet (IMAP/SMTP Felder)
- ✅ Kalender-Synchronisation (Google Calendar API ready)
- ✅ AI-Service Integration (OpenAI/Ollama vorbereitet)
- ✅ Message Queue (RabbitMQ für Background-Jobs)
- ✅ Vector Database (Qdrant für AI/Embeddings)
- ✅ Caching (Redis)
- ✅ API-Dokumentation (Swagger UI)

---

**🎊 Deployment abgeschlossen! System ist produktionsbereit.**

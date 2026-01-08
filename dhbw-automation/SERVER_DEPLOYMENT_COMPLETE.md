# ✅ Server Deployment - Abgeschlossen

## 🎯 Was wurde eingerichtet?

### 1. Git-Repository auf dem Server
- **Bare Repository**: `/root/git-repos/dhbw-automation.git`
- **Deployment-Verzeichnis**: `/root/dhbw-automation-deploy/dhbw-automation`
- **SSH-Zugriff**: Bereits konfiguriert (passwortlos)

### 2. Docker Infrastructure Services
Die folgenden Services laufen auf dem Server **192.168.178.198**:

| Service | Port (Host) | Port (Container) | URL |
|---------|-------------|------------------|-----|
| **MariaDB** | 3307 | 3306 | - |
| **Redis** | 6380 | 6379 | - |
| **MinIO** | 9002 | 9000 | http://192.168.178.198:9002 |
| **MinIO Console** | 9003 | 9001 | http://192.168.178.198:9003 |
| **RabbitMQ** | 5673 | 5672 | - |
| **RabbitMQ Mgmt** | 15673 | 15672 | http://192.168.178.198:15673 |
| **Qdrant** | 6335 | 6333 | http://192.168.178.198:6335 |
| **Qdrant gRPC** | 6336 | 6334 | - |
| **phpMyAdmin** | 8082 | 80 | http://192.168.178.198:8082 |

**Hinweis**: Ports wurden angepasst, um Konflikte mit bestehenden Containern zu vermeiden.

## 🚀 Deployment-Workflow

### Von deinem PC Code deployen:

```powershell
# 1. Änderungen committen
cd "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw"
git add .
git commit -m "Deine Nachricht"

# 2. Zum Server pushen
git push server main

# 3. Auf Server Code auschecken und Container neu starten
ssh root@192.168.178.198 "cd /root/git-repos/dhbw-automation.git && git --work-tree=/root/dhbw-automation-deploy --git-dir=/root/git-repos/dhbw-automation.git checkout -f main && cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml up -d"
```

### Oder als Ein-Befehl:

```powershell
git add . && git commit -m "Update" && git push server main && ssh root@192.168.178.198 "cd /root/git-repos/dhbw-automation.git && git --work-tree=/root/dhbw-automation-deploy --git-dir=/root/git-repos/dhbw-automation.git checkout -f main && cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml up -d"
```

## 🔍 Wichtige Befehle

### Container-Status prüfen
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml ps"
```

### Logs ansehen
```bash
# Alle Container
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f"

# Einzelner Service
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f mariadb"
```

### Container neu starten
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml restart"
```

### Container stoppen
```bash
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml down"
```

### Neuestes Code-Update ohne Rebuild
```bash
ssh root@192.168.178.198 "cd /root/git-repos/dhbw-automation.git && git --work-tree=/root/dhbw-automation-deploy --git-dir=/root/git-repos/dhbw-automation.git checkout -f main"
```

## 📊 Git-Repository Struktur

```
Server: 192.168.178.198
├── /root/git-repos/dhbw-automation.git/     # Bare Git Repository
│   ├── hooks/
│   │   └── post-receive                     # (Aktuell deaktiviert wegen Fehler)
│   ├── objects/
│   └── refs/
│
└── /root/dhbw-automation-deploy/            # Working Tree
    └── dhbw-automation/
        ├── docker-compose.prod.yml          # Production Docker Compose
        ├── docker/
        │   ├── backend.Dockerfile
        │   ├── frontend.Dockerfile
        │   └── nginx.conf
        ├── .env                             # Environment Variables
        └── src/                             # Source Code
```

## ⚙️ Git Remote Konfiguration

Auf deinem lokalen PC:
```powershell
git remote -v
```

Ausgabe:
```
origin  https://github.com/DrLuggels/my_dhbw.git (fetch)
origin  https://github.com/DrLuggels/my_dhbw.git (push)
server  root@192.168.178.198:git-repos/dhbw-automation.git (fetch)
server  root@192.168.178.198:git-repos/dhbw-automation.git (push)
```

## 🔐 Credentials

### MinIO
- **User**: minioadmin (konfigurierbar in .env)
- **Password**: minioadmin (konfigurierbar in .env)
- **Console**: http://192.168.178.198:9003

### RabbitMQ Management
- **User**: guest (konfigurierbar in .env)
- **Password**: guest (konfigurierbar in .env)
- **Console**: http://192.168.178.198:15673

### MariaDB
- **Host**: 192.168.178.198:3307
- **Database**: dhbw_automation
- **User**: dhbw_user
- **Password**: dhbw_password (konfigurierbar in .env)
- **Root Password**: rootpassword (konfigurierbar in .env)
- **phpMyAdmin**: http://192.168.178.198:8082

## 📝 Nächste Schritte

1. **Backend-Anwendung fertigstellen**
   - Models vervollständigen
   - Dockerfiles testen
   - Backend-Container aktivieren

2. **Frontend-Anwendung fertigstellen**
   - Build-Prozess optimieren
   - Frontend-Container aktivieren

3. **Post-Receive Hook reparieren**
   - Syntaxfehler beheben für automatisches Deployment

4. **Reverse Proxy einrichten**
   - nginx/Caddy für SSL/TLS
   - Let's Encrypt Zertifikate

5. **Monitoring einrichten**
   - Prometheus/Grafana (optional)
   - Logging zentralisieren

## 🎓 Wie funktioniert das?

### Git-Workflow ohne externe Repository

```
┌─────────────┐                      ┌──────────────────────────┐
│  Dein PC    │  git push server     │   Server (192.168.178.198)│
│             │  ──────────────────> │                          │
│ my_dhbw/    │                      │ Bare Repo:               │
│  .git/      │                      │ /root/git-repos/         │
└─────────────┘                      │  dhbw-automation.git/    │
                                     │                          │
                                     │ Working Tree:            │
                                     │ /root/dhbw-automation-   │
                                     │  deploy/dhbw-automation/ │
                                     └──────────────────────────┘
                                              │
                                              │ git checkout
                                              │ --work-tree
                                              ▼
                                     ┌──────────────────────────┐
                                     │  Docker Container        │
                                     │  lesen Code aus:         │
                                     │  /root/dhbw-automation-  │
                                     │  deploy/dhbw-automation/ │
                                     └──────────────────────────┘
```

### Warum kein "git pull"?

- Das Bare-Repository hat **keinen Remote** (es ist selbst der Server!)
- Stattdessen: `git push` von PC → Bare-Repo
- Dann: `git checkout` vom Bare-Repo → Working Tree

## ✅ Status

- [x] SSH-Zugriff konfiguriert
- [x] Git-Repository auf Server eingerichtet
- [x] Docker Compose Production-Config erstellt
- [x] Ports angepasst (Konflikte vermieden)
- [x] Infrastructure Services deployed (MariaDB, Redis, MinIO, RabbitMQ, Qdrant)
- [x] Management-Tools deployed (phpMyAdmin)
- [x] Deployment-Dokumentation erstellt
- [ ] Post-Receive Hook reparieren
- [ ] Backend-Container aktivieren
- [ ] Frontend-Container aktivieren
- [ ] SSL/TLS Reverse Proxy einrichten

---

**Erstellt**: 2026-01-08  
**Server**: 192.168.178.198  
**Projekt**: DHBW Automation System

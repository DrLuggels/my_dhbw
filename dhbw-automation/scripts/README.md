# =============================================================================
# DHBW Automation - Scripts Overview
# =============================================================================

## 📜 Verfügbare Skripte

### 1. Rebuild & Deploy (von deinem PC)
**Datei**: `rebuild-and-deploy.ps1`

Führt kompletten Deployment-Workflow aus:
- Commitet lokale Änderungen
- Pusht zum Server
- Rebuilded Container auf Server (ohne Cache)
- Zeigt Status

**Verwendung**:
```powershell
# Mit eigener Commit-Message
.\scripts\rebuild-and-deploy.ps1 "Meine Änderungen"

# Mit Default-Message
.\scripts\rebuild-and-deploy.ps1
```

---

### 2. Rebuild auf Server
**Datei**: `rebuild.sh`

Rebuilded alle Container auf dem Server (ohne Cache):
- Holt neueste Version aus Git
- Stoppt alte Container
- Baut Container neu (--no-cache)
- Startet Container

**Verwendung**:
```bash
# Direkt auf dem Server
ssh root@192.168.178.198
cd /root/dhbw-automation-deploy/dhbw-automation/scripts
chmod +x rebuild.sh
./rebuild.sh

# Oder remote von deinem PC
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation/scripts && chmod +x rebuild.sh && ./rebuild.sh"
```

---

### 3. Quick Restart
**Datei**: `restart.sh`

Schneller Neustart ohne Rebuild:
- Stoppt Container
- Startet Container neu
- Kein Cache-Clear, kein Rebuild

**Verwendung**:
```bash
# Direkt auf dem Server
ssh root@192.168.178.198
cd /root/dhbw-automation-deploy/dhbw-automation/scripts
chmod +x restart.sh
./restart.sh

# Oder remote von deinem PC
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation/scripts && chmod +x restart.sh && ./restart.sh"
```

---

## 🎯 Wann welches Skript verwenden?

| Szenario | Skript | Beschreibung |
|----------|--------|--------------|
| **Code geändert & deployen** | `rebuild-and-deploy.ps1` | Kompletter Workflow von PC |
| **Dockerfile geändert** | `rebuild.sh` | Container komplett neu bauen |
| **Nur Config geändert** | `restart.sh` | Schneller Neustart |
| **Container hängen** | `restart.sh` | Quick Fix |

---

## 📋 Beispiel-Workflows

### Workflow 1: Development-Änderungen deployen
```powershell
# Auf deinem PC (PowerShell)
cd "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw\dhbw-automation"

# Alles in einem
.\scripts\rebuild-and-deploy.ps1 "Feature XYZ hinzugefügt"
```

### Workflow 2: Nur Dockerfile geändert
```bash
# Auf Server
ssh root@192.168.178.198
cd /root/dhbw-automation-deploy/dhbw-automation/scripts
./rebuild.sh
```

### Workflow 3: Container neustarten (ohne Rebuild)
```bash
# Remote von PC
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation/scripts && ./restart.sh"
```

---

## 🔧 Manuelle Befehle (falls Skripte nicht funktionieren)

### Container rebuild (ohne Cache)
```bash
cd /root/dhbw-automation-deploy/dhbw-automation
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml build --no-cache
docker compose -f docker-compose.prod.yml up -d
```

### Nur bestimmten Service rebuilden
```bash
cd /root/dhbw-automation-deploy/dhbw-automation
docker compose -f docker-compose.prod.yml build --no-cache mariadb
docker compose -f docker-compose.prod.yml up -d mariadb
```

### Container neustarten (einzeln)
```bash
docker compose -f docker-compose.prod.yml restart mariadb
docker compose -f docker-compose.prod.yml restart redis
```

---

## 🚨 Troubleshooting

### Skript hat keine Ausführungsrechte
```bash
chmod +x /root/dhbw-automation-deploy/dhbw-automation/scripts/*.sh
```

### PowerShell Execution Policy
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Container bauen nicht
```bash
# Alle Container und Images entfernen (VORSICHT: Löscht Daten!)
docker compose -f docker-compose.prod.yml down -v --rmi all
docker compose -f docker-compose.prod.yml build --no-cache
docker compose -f docker-compose.prod.yml up -d
```

---

## 📊 Nützliche Befehle

### Logs anzeigen
```bash
# Alle Container
docker compose -f docker-compose.prod.yml logs -f

# Einzelner Container
docker compose -f docker-compose.prod.yml logs -f mariadb
```

### Container Status
```bash
docker compose -f docker-compose.prod.yml ps
```

### Ressourcen-Nutzung
```bash
docker stats
```

### Container Shell öffnen
```bash
docker exec -it dhbw-mariadb bash
docker exec -it dhbw-redis sh
```

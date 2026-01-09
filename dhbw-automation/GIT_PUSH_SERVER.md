# Git Push zum Server (ohne GitHub)

## Voraussetzungen
Der Server hat ein Bare Git Repository unter `/root/git-repos/dhbw-automation.git` und ein Arbeitsverzeichnis unter `/root/dhbw-automation-deploy/dhbw-automation`.

## Git Remote ist bereits konfiguriert
```bash
git remote -v
# Zeigt:
# server  root@192.168.178.198:git-repos/dhbw-automation.git (fetch)
# server  root@192.168.178.198:git-repos/dhbw-automation.git (push)
```

## Deploy-Prozess

### 1. Änderungen committen
```powershell
cd "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Ubungen\Projekte\my_dhbw\dhbw-automation"
git add .
git commit -m "Deine Änderung beschreiben"
```

### 2. Zum Server pushen
```powershell
git push server main
```

### 3. Code auf Server auschecken (in Arbeitsverzeichnis)
```powershell
ssh root@192.168.178.198 "cd /root/git-repos/dhbw-automation.git && git --work-tree=/root/dhbw-automation-deploy/dhbw-automation --git-dir=/root/git-repos/dhbw-automation.git checkout -f main"
```

### 4. Container neu bauen und starten
```powershell
# Nur Backend
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml build backend && docker compose -f docker-compose.prod.yml up -d backend"

# Nur Frontend
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml build frontend && docker compose -f docker-compose.prod.yml up -d frontend"

# Beides
ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml build && docker compose -f docker-compose.prod.yml up -d"
```

## Oder: Deploy-Skript verwenden
```powershell
.\scripts\deploy.ps1 "Meine Änderung"
```

Das Skript macht alle Schritte automatisch (außer Container neu starten).

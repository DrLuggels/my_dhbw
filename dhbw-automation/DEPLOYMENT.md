# 🚀 DHBW Automation - Server Deployment Guide

## Übersicht

Dieses Dokument beschreibt, wie du das DHBW Automation System auf dem Server deployed und updates durchführst.

## Server-Informationen

- **Server-IP**: `192.168.178.198`
- **SSH-Zugang**: `root@192.168.178.198` (SSH-Key Authentication)
- **Git Repository**: `/root/git-repos/dhbw-automation.git` (Bare Repository)
- **Deployment-Verzeichnis**: `/root/dhbw-automation-deploy`

## 📦 Erste Einrichtung

### 1. Umgebungsvariablen konfigurieren

Verbinde dich per SSH mit dem Server und erstelle eine `.env`-Datei:

```bash
ssh root@192.168.178.198
cd ~/dhbw-automation-deploy/dhbw-automation
cp .env.example .env
nano .env  # Passe die Passwörter an!
```

**Wichtig**: Ändere alle Passwörter in der `.env`-Datei!

### 2. Initiales Deployment

Nach dem ersten Git-Push wird das Projekt automatisch deployed. Wenn nicht, führe manuell aus:

```bash
cd ~/dhbw-automation-deploy/dhbw-automation
docker compose -f docker-compose.prod.yml up -d --build
```

## 🔄 Regelmäßige Updates/Deployment

### Workflow: Lokale Änderungen zum Server pushen

1. **Lokale Änderungen committen**:
   ```powershell
   cd "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw"
   git add .
   git commit -m "Deine Änderungen beschreiben"
   ```

2. **Zum Server pushen**:
   ```powershell
   git push server main
   ```

3. **Automatisches Deployment erfolgt!**
   - Der Git Post-Receive Hook checkt den Code automatisch aus
   - Stoppt die alten Container
   - Startet die neuen Container

### Manuelles Deployment

Falls du manuell deployen möchtest:

```bash
# SSH zum Server
ssh root@192.168.178.198

# Zum Projekt-Verzeichnis
cd ~/dhbw-automation-deploy/dhbw-automation

# Container neu bauen und starten
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d --build
```

## 📋 Wichtige Docker-Befehle

### Container-Status prüfen
```bash
docker compose -f docker-compose.prod.yml ps
```

### Logs ansehen
```bash
# Alle Container
docker compose -f docker-compose.prod.yml logs -f

# Spezifischer Container
docker compose -f docker-compose.prod.yml logs -f backend
docker compose -f docker-compose.prod.yml logs -f frontend
```

### Container neu starten
```bash
# Alle Container
docker compose -f docker-compose.prod.yml restart

# Einzelner Container
docker compose -f docker-compose.prod.yml restart backend
```

### Container stoppen
```bash
docker compose -f docker-compose.prod.yml down
```

### Container starten (ohne rebuild)
```bash
docker compose -f docker-compose.prod.yml up -d
```

### Volumes löschen (VORSICHT: Löscht alle Daten!)
```bash
docker compose -f docker-compose.prod.yml down -v
```

## 🌐 Zugriff auf Services

Nach erfolgreichem Deployment sind folgende Services verfügbar:

- **Frontend**: http://192.168.178.198
- **Backend API**: http://192.168.178.198:5000
- **phpMyAdmin**: http://192.168.178.198:8080
- **MinIO Console**: http://192.168.178.198:9001
- **RabbitMQ Management**: http://192.168.178.198:15672
- **Qdrant Dashboard**: http://192.168.178.198:6333/dashboard

## 🔧 Troubleshooting

### Container startet nicht
```bash
# Logs ansehen
docker compose -f docker-compose.prod.yml logs backend

# Container neu bauen
docker compose -f docker-compose.prod.yml up -d --build --force-recreate backend
```

### Datenbank-Probleme
```bash
# In MariaDB Container einloggen
docker exec -it dhbw-mariadb mysql -u root -p

# Oder mit phpMyAdmin: http://192.168.178.198:8080
```

### Git-Repository-Probleme
```bash
# Manuell pullen
ssh root@192.168.178.198
cd ~/dhbw-automation-deploy
git --work-tree=/root/dhbw-automation-deploy --git-dir=/root/git-repos/dhbw-automation.git pull origin main
```

### Speicherplatz prüfen
```bash
# Docker Speichernutzung
docker system df

# Ungenutzte Images/Container aufräumen
docker system prune -a
```

## 🔒 Sicherheitshinweise

1. **Ändere alle Standard-Passwörter** in der `.env`-Datei
2. **Firewall konfigurieren**: Nur notwendige Ports öffnen
3. **SSL/TLS einrichten**: Für Production einen Reverse-Proxy (nginx/Caddy) mit Let's Encrypt verwenden
4. **Backups**: Regelmäßige Backups der Docker Volumes erstellen

### Backup erstellen
```bash
# Volumes sichern
docker run --rm \
  -v dhbw-automation_mariadb_data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/mariadb_$(date +%Y%m%d_%H%M%S).tar.gz /data
```

## 📝 Deployment-Checkliste

Vor jedem Deployment:

- [ ] Lokale Tests erfolgreich
- [ ] Änderungen committed
- [ ] `.env`-Datei auf Server aktualisiert (falls nötig)
- [ ] Backup erstellt (bei größeren Änderungen)
- [ ] Push zum Server: `git push server main`
- [ ] Logs prüfen: `docker compose logs -f`
- [ ] Services testen

## 🆘 Support

Bei Problemen:
1. Logs prüfen (`docker compose logs`)
2. Container-Status prüfen (`docker compose ps`)
3. Service-Health prüfen (Health-Endpoints)
4. System-Ressourcen prüfen (`htop`, `df -h`)

## 📚 Weitere Ressourcen

- [Docker Compose Dokumentation](https://docs.docker.com/compose/)
- [.NET Docker Best Practices](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/docker-application-development-process/)
- [Vue.js Deployment Guide](https://vuejs.org/guide/best-practices/production-deployment.html)

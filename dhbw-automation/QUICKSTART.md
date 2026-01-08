# 🚀 DHBW Automation - Quick Start

## ⚡ Schnellstart in 5 Minuten

### 1️⃣ Repository Setup
```bash
# Erstelle Projektordner
mkdir dhbw-automation
cd dhbw-automation

# Kopiere alle Dateien aus dem Download hierhin
```

### 2️⃣ Environment konfigurieren
```bash
# .env erstellen
cp .env.example .env

# Öffne .env und fülle minimal aus:
# - OPENAI_API_KEY (hol dir einen auf platform.openai.com)
# - DB_PASSWORD (wähle ein sicheres Passwort)
# - JWT_SECRET (generiere einen mit: openssl rand -base64 32)
```

### 3️⃣ Docker starten
```bash
docker-compose up -d

# Überprüfen ob alles läuft:
docker-compose ps
```

### 4️⃣ Backend initialisieren
```bash
cd src/Backend
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

### 5️⃣ Frontend starten
```bash
# Neues Terminal
cd src/Frontend
npm install
npm run dev
```

### ✅ Fertig!

- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger
- **phpMyAdmin**: http://localhost:8080

---

## 📚 Wichtige Dokumente

| Dokument | Zweck |
|----------|-------|
| [README.md](README.md) | Projekt-Übersicht |
| [SETUP_GUIDE.md](SETUP_GUIDE.md) | Detaillierte Setup-Anleitung |
| [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) | Ordnerstruktur erklärt |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Wie du beitragen kannst |

---

## 🎯 Nächste Schritte

1. **Account erstellen** auf http://localhost:5173
2. **Erste Datei hochladen** und KI-Analyse testen
3. **Google Calendar verbinden**
4. **Mail-Accounts konfigurieren**
5. **Live-Lecture-Mode aktivieren**

---

## 💡 Häufige Probleme

### Docker startet nicht?
```bash
# Windows
net stop docker
net start docker

# Linux
sudo systemctl restart docker
```

### Port 3306 bereits belegt?
```bash
# Port in docker-compose.yml ändern:
ports:
  - "3307:3306"  # statt 3306:3306
```

### Backend findet .env nicht?
```bash
# .env muss im gleichen Ordner wie Backend.csproj sein
cp .env src/Backend/.env
```

---

## 🆘 Hilfe

- 📖 Vollständige Doku: [SETUP_GUIDE.md](SETUP_GUIDE.md)
- 🐛 Probleme: [Troubleshooting](docs/troubleshooting.md)
- 💬 Fragen: GitHub Issues

**Happy Coding! 🎓**

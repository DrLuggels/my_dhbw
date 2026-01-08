# 🔄 Quick Deploy Guide

## Schneller Deploy zum Server

### 1. Änderungen pushen
```powershell
cd "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw"
git add .
git commit -m "Update Beschreibung"
git push server main
```

### 2. Das war's! 🎉

Der Server deployed automatisch:
- ✅ Code wird ausgecheckt
- ✅ Container werden gestoppt
- ✅ Neue Container werden gebaut und gestartet

### Status prüfen
```bash
ssh root@192.168.178.198 "cd ~/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml ps"
```

### Logs ansehen
```bash
ssh root@192.168.178.198 "cd ~/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f"
```

---

📖 Für Details siehe [DEPLOYMENT.md](DEPLOYMENT.md)

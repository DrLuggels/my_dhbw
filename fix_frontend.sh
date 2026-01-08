#!/bin/bash
set -e

echo "===================================="
echo "Frontend Fix - Step by Step"
echo "===================================="

echo ""
echo "Schritt 1: Git Repository aktualisieren..."
cd /root/git-repos/dhbw-automation.git
GIT_WORK_TREE=/root/dhbw-automation-deploy git reset --hard HEAD

echo ""
echo "Schritt 2: .env.production prüfen..."
if [ -f /root/dhbw-automation-deploy/dhbw-automation/src/Frontend/.env.production ]; then
    echo "✓ .env.production gefunden:"
    cat /root/dhbw-automation-deploy/dhbw-automation/src/Frontend/.env.production
else
    echo "✗ .env.production NICHT gefunden!"
    exit 1
fi

echo ""
echo "Schritt 3: Dockerfile aktualisieren..."
cd /root/dhbw-automation-deploy/dhbw-automation

# Backup erstellen
cp docker/frontend.Dockerfile docker/frontend.Dockerfile.bak

# Zeile nach "COPY src/Frontend/. ." hinzufügen, falls noch nicht vorhanden
if ! grep -q "COPY src/Frontend/.env.production" docker/frontend.Dockerfile; then
    echo "Füge .env.production COPY Zeile hinzu..."
    sed -i '/COPY src\/Frontend\/\. \./a\
\# Ensure .env.production is available for build\
COPY src/Frontend/.env.production ./' docker/frontend.Dockerfile
    echo "✓ Dockerfile aktualisiert"
else
    echo "✓ Dockerfile bereits korrekt"
fi

echo ""
echo "Schritt 4: Frontend Container stoppen..."
docker compose -f docker-compose.prod.yml down frontend

echo ""
echo "Schritt 5: Frontend neu bauen (ohne Cache)..."
docker compose -f docker-compose.prod.yml build --no-cache frontend

echo ""
echo "Schritt 6: Frontend Container starten..."
docker compose -f docker-compose.prod.yml up -d frontend

echo ""
echo "Schritt 7: Status prüfen..."
sleep 5
docker ps | grep dhbw-frontend

echo ""
echo "===================================="
echo "✓ Frontend Fix abgeschlossen!"
echo "===================================="
echo ""
echo "Teste jetzt im Browser: http://192.168.178.198:8091"
echo "Hard Refresh: Ctrl+Shift+R"

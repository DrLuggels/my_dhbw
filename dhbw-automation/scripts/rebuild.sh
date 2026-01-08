#!/bin/bash
# =============================================================================
# DHBW Automation - Server Rebuild Script
# =============================================================================
# Dieses Skript baut alle Docker Container neu (ohne Cache) und startet sie
# Verwendung: ./rebuild.sh

set -e  # Bei Fehler abbrechen

# Farben für Output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}DHBW Automation - Container Rebuild${NC}"
echo -e "${GREEN}========================================${NC}"

# Ins Deployment-Verzeichnis wechseln
cd /root/dhbw-automation-deploy/dhbw-automation

# Neueste Version aus Git holen
echo -e "\n${YELLOW}[1/4] Hole neueste Version aus Git...${NC}"
cd /root/git-repos/dhbw-automation.git
git --work-tree=/root/dhbw-automation-deploy --git-dir=/root/git-repos/dhbw-automation.git checkout -f main
echo -e "${GREEN}✓ Code aktualisiert${NC}"

# Zurück zum Deployment-Verzeichnis
cd /root/dhbw-automation-deploy/dhbw-automation

# Container stoppen und entfernen
echo -e "\n${YELLOW}[2/4] Stoppe und entferne alte Container...${NC}"
docker compose -f docker-compose.prod.yml down
echo -e "${GREEN}✓ Container gestoppt${NC}"

# Alte Images entfernen (optional - auskommentiert für Sicherheit)
# echo -e "\n${YELLOW}Entferne alte Images...${NC}"
# docker compose -f docker-compose.prod.yml down --rmi all

# Container neu bauen (ohne Cache)
echo -e "\n${YELLOW}[3/4] Baue Container neu (ohne Cache)...${NC}"
docker compose -f docker-compose.prod.yml build --no-cache
echo -e "${GREEN}✓ Container gebaut${NC}"

# Container starten
echo -e "\n${YELLOW}[4/4] Starte Container...${NC}"
docker compose -f docker-compose.prod.yml up -d
echo -e "${GREEN}✓ Container gestartet${NC}"

# Kurz warten
sleep 3

# Status anzeigen
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Container Status:${NC}"
echo -e "${GREEN}========================================${NC}"
docker compose -f docker-compose.prod.yml ps

echo -e "\n${GREEN}✓ Rebuild abgeschlossen!${NC}"
echo -e "\n${YELLOW}Logs anzeigen:${NC} docker compose -f docker-compose.prod.yml logs -f"
echo -e "${YELLOW}Services:${NC}"
echo -e "  - MariaDB:        localhost:3307"
echo -e "  - Redis:          localhost:6380"
echo -e "  - MinIO:          http://localhost:9002"
echo -e "  - MinIO Console:  http://localhost:9003"
echo -e "  - RabbitMQ:       localhost:5673"
echo -e "  - RabbitMQ Mgmt:  http://localhost:15673"
echo -e "  - Qdrant:         http://localhost:6335"
echo -e "  - phpMyAdmin:     http://localhost:8082"

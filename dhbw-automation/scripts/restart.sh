#!/bin/bash
# =============================================================================
# DHBW Automation - Quick Restart (ohne Rebuild)
# =============================================================================
# Dieses Skript startet die Container schnell neu (ohne neu zu bauen)
# Verwendung: ./restart.sh

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}DHBW Automation - Quick Restart${NC}"
echo -e "${GREEN}========================================${NC}"

cd /root/dhbw-automation-deploy/dhbw-automation

echo -e "\n${YELLOW}[1/2] Stoppe Container...${NC}"
docker compose -f docker-compose.prod.yml down

echo -e "\n${YELLOW}[2/2] Starte Container...${NC}"
docker compose -f docker-compose.prod.yml up -d

sleep 2

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Container Status:${NC}"
echo -e "${GREEN}========================================${NC}"
docker compose -f docker-compose.prod.yml ps

echo -e "\n${GREEN}✓ Restart abgeschlossen!${NC}"

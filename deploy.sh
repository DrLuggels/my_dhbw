#!/bin/bash
# Deploy to server: push code via git, rebuild Docker containers with --no-cache
set -e

SERVER="root@192.168.178.198"
REPO_DIR="/root/dhbw-automation.git"
DEPLOY_DIR="/root/dhbw-automation-deploy"
BRANCH="main"

echo "=== DHBW Automation Deploy ==="

# Ensure server remote exists
if ! git remote | grep -q "^server$"; then
    echo "Adding server remote..."
    git remote add server "${SERVER}:${REPO_DIR}"
fi

# Push to server
echo "Pushing ${BRANCH} to server..."
git push server ${BRANCH}

# SSH to server: rebuild with --no-cache and restart
echo "Rebuilding containers with --no-cache..."
ssh ${SERVER} << 'EOF'
cd /root/dhbw-automation-deploy
docker compose -f docker-compose.prod.yml build --no-cache
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml exec -T backend alembic upgrade head 2>/dev/null || echo "Migrations skipped (backend may still be starting)"
echo "=== Deploy complete ==="
docker compose -f docker-compose.prod.yml ps
EOF

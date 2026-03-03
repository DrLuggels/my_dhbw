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

# Push to server (triggers post-receive hook which does checkout)
echo "Pushing ${BRANCH} to server..."
git push server ${BRANCH}

# SSH to server: rebuild with --no-cache and restart
echo "Rebuilding containers with --no-cache..."
ssh ${SERVER} << 'EOF'
cd /root/dhbw-automation-deploy

# Stop existing containers
docker compose -f docker-compose.prod.yml down

# Rebuild everything from scratch
docker compose -f docker-compose.prod.yml build --no-cache

# Start all services
docker compose -f docker-compose.prod.yml up -d

# Wait for DB to be healthy, then run migrations
echo "Waiting for services to start..."
sleep 10
docker compose -f docker-compose.prod.yml exec -T backend alembic upgrade head 2>/dev/null || echo "Migrations: will retry on next deploy"

echo ""
echo "=== Deploy complete ==="
docker compose -f docker-compose.prod.yml ps
echo ""
echo "App: http://192.168.178.198:8090"
echo "API: http://192.168.178.198:8090/health"
EOF

#!/bin/bash
# One-time server setup script for 192.168.178.198
# Run: ssh root@192.168.178.198 'bash -s' < deploy/setup-server.sh

set -e

DEPLOY_DIR="/root/dhbw-automation-deploy"
REPO_DIR="/root/dhbw-automation.git"

echo "=== Setting up DHBW Automation on server ==="

# Create bare git repo
mkdir -p "$REPO_DIR"
cd "$REPO_DIR"
git init --bare

# Create deploy directory
mkdir -p "$DEPLOY_DIR"

# Install post-receive hook
cat > "$REPO_DIR/hooks/post-receive" << 'HOOK'
#!/bin/bash
set -e

DEPLOY_DIR="/root/dhbw-automation-deploy"
BRANCH="main"

while read oldrev newrev refname; do
    if [ "$refname" = "refs/heads/$BRANCH" ]; then
        echo "=== Deploying $BRANCH ==="
        git --work-tree="$DEPLOY_DIR" --git-dir="$(pwd)" checkout -f "$BRANCH"
        cd "$DEPLOY_DIR"
        docker compose -f docker-compose.prod.yml build
        docker compose -f docker-compose.prod.yml up -d
        docker compose -f docker-compose.prod.yml exec -T backend alembic upgrade head
        echo "=== Deployment complete ==="
    fi
done
HOOK

chmod +x "$REPO_DIR/hooks/post-receive"

echo "=== Server setup complete ==="
echo "Add remote: git remote add server root@192.168.178.198:$REPO_DIR"
echo "Deploy:     git push server main"

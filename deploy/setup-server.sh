#!/bin/bash
# One-time server setup for 192.168.178.198
# Run: ssh root@192.168.178.198 'bash -s' < deploy/setup-server.sh
set -e

DEPLOY_DIR="/root/dhbw-automation-deploy"
REPO_DIR="/root/dhbw-automation.git"

echo "=== Setting up DHBW Automation on server ==="

# Install Docker if not present
if ! command -v docker &> /dev/null; then
    echo "Installing Docker..."
    curl -fsSL https://get.docker.com | sh
    systemctl enable --now docker
fi

# Install Docker Compose plugin if not present
if ! docker compose version &> /dev/null; then
    echo "Installing Docker Compose plugin..."
    apt-get update && apt-get install -y docker-compose-plugin
fi

# Create bare git repo
if [ ! -d "$REPO_DIR" ]; then
    mkdir -p "$REPO_DIR"
    cd "$REPO_DIR"
    git init --bare
    echo "Created bare git repo at $REPO_DIR"
else
    echo "Bare git repo already exists at $REPO_DIR"
fi

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
        echo "=== Deploying $BRANCH to $DEPLOY_DIR ==="

        # Update working copy
        git --work-tree="$DEPLOY_DIR" --git-dir="$(pwd)" checkout -f "$BRANCH"

        cd "$DEPLOY_DIR"

        # Create .env from example if it doesn't exist
        if [ ! -f .env ]; then
            cp .env.example .env
            echo "WARNING: Created .env from .env.example - fill in your API keys!"
        fi

        # Build and restart containers
        docker compose -f docker-compose.prod.yml build --no-cache
        docker compose -f docker-compose.prod.yml up -d

        # Wait for backend to be healthy, then run migrations
        echo "Waiting for backend to start..."
        sleep 10
        docker compose -f docker-compose.prod.yml exec -T backend alembic upgrade head 2>/dev/null || echo "Migrations: will retry on next deploy"

        echo "=== Deployment complete ==="
        docker compose -f docker-compose.prod.yml ps
    fi
done
HOOK

chmod +x "$REPO_DIR/hooks/post-receive"

# Create .env from example if deploy dir has .env.example but no .env
if [ -f "$DEPLOY_DIR/.env.example" ] && [ ! -f "$DEPLOY_DIR/.env" ]; then
    cp "$DEPLOY_DIR/.env.example" "$DEPLOY_DIR/.env"
    echo "Created .env from .env.example"
fi

echo ""
echo "=== Server setup complete ==="
echo "On your local machine, run:"
echo "  git remote add server root@192.168.178.198:$REPO_DIR"
echo "  git push server main"
echo ""
echo "Or use the deploy script:"
echo "  bash deploy.sh"

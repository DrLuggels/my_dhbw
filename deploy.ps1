# DHBW Automation Deployment Script
# This script builds and deploys the backend to the production server

param(
    [string]$Service = "backend",
    [switch]$ShowLogs = $true,
    [int]$LogLines = 100
)

$SERVER = "root@192.168.178.198"
$PROJECT_PATH = "/root/dhbw-automation-deploy/dhbw-automation"
$COMPOSE_FILE = "docker-compose.prod.yml"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "DHBW Automation Deployment" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Build and deploy
Write-Host "[1/2] Building and deploying $Service..." -ForegroundColor Yellow
$deployCommand = "cd $PROJECT_PATH && docker compose -f $COMPOSE_FILE build $Service && docker compose -f $COMPOSE_FILE up -d $Service"
ssh $SERVER $deployCommand

if ($LASTEXITCODE -eq 0) {
    Write-Host "[SUCCESS] Deployment completed!" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Deployment failed!" -ForegroundColor Red
    exit 1
}

# Show logs
if ($ShowLogs) {
    Write-Host ""
    Write-Host "[2/2] Fetching logs (last $LogLines lines)..." -ForegroundColor Yellow
    $logCommand = "cd $PROJECT_PATH && docker compose -f $COMPOSE_FILE logs --tail=$LogLines $Service"
    ssh $SERVER $logCommand
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan

# =============================================================================
# DHBW Automation - Status anzeigen (von PC)
# =============================================================================
# Zeigt Container-Status vom Server an
# Verwendung: .\status.ps1

$ErrorActionPreference = "Stop"

function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "========================================"
Write-ColorOutput Green "DHBW Automation - Container Status"
Write-ColorOutput Green "========================================"

ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml ps"

Write-ColorOutput Yellow "`nServices:"
Write-ColorOutput White "  - MariaDB:        192.168.178.198:3307"
Write-ColorOutput White "  - Redis:          192.168.178.198:6380"
Write-ColorOutput White "  - MinIO:          http://192.168.178.198:9002"
Write-ColorOutput White "  - MinIO Console:  http://192.168.178.198:9003"
Write-ColorOutput White "  - RabbitMQ:       192.168.178.198:5673"
Write-ColorOutput White "  - RabbitMQ Mgmt:  http://192.168.178.198:15673"
Write-ColorOutput White "  - Qdrant:         http://192.168.178.198:6335"
Write-ColorOutput White "  - phpMyAdmin:     http://192.168.178.198:8082"

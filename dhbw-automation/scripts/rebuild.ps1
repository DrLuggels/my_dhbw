# =============================================================================
# DHBW Automation - Rebuild Container (von PC)
# =============================================================================
# Führt rebuild.sh auf dem Server aus (baut Container neu mit --no-cache)
# Verwendung: .\rebuild.ps1

$ErrorActionPreference = "Stop"

function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "========================================"
Write-ColorOutput Green "DHBW Automation - Container Rebuild"
Write-ColorOutput Green "========================================"
Write-ColorOutput Yellow "`nVerbinde mit Server 192.168.178.198..."
Write-ColorOutput Yellow "Dies kann einige Minuten dauern..."

ssh root@192.168.178.198 "/root/dhbw-automation-deploy/dhbw-automation/scripts/rebuild.sh"

if ($LASTEXITCODE -eq 0) {
    Write-ColorOutput Green "`n✓ Rebuild erfolgreich abgeschlossen!"
    Write-ColorOutput Yellow "`nServices verfügbar unter:"
    Write-ColorOutput White "  - MinIO Console:  http://192.168.178.198:9003"
    Write-ColorOutput White "  - RabbitMQ Mgmt:  http://192.168.178.198:15673"
    Write-ColorOutput White "  - phpMyAdmin:     http://192.168.178.198:8082"
} else {
    Write-ColorOutput Red "`n✗ Fehler beim Rebuild!"
    exit 1
}

# =============================================================================
# DHBW Automation - Restart Container (von PC)
# =============================================================================
# Führt restart.sh auf dem Server aus
# Verwendung: .\restart.ps1

$ErrorActionPreference = "Stop"

function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "========================================"
Write-ColorOutput Green "DHBW Automation - Container Restart"
Write-ColorOutput Green "========================================"
Write-ColorOutput Yellow "`nVerbinde mit Server 192.168.178.198..."

ssh root@192.168.178.198 "/root/dhbw-automation-deploy/dhbw-automation/scripts/restart.sh"

if ($LASTEXITCODE -eq 0) {
    Write-ColorOutput Green "`n[OK] Restart erfolgreich abgeschlossen!"
} else {
    Write-ColorOutput Red "`n[FEHLER] Fehler beim Restart!"
    exit 1
}

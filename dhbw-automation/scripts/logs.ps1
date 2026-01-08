# =============================================================================
# DHBW Automation - Logs anzeigen (von PC)
# =============================================================================
# Zeigt Live-Logs aller Container vom Server an
# Verwendung: .\logs.ps1 [service-name]
# Beispiele:
#   .\logs.ps1           # Alle Container
#   .\logs.ps1 mariadb   # Nur MariaDB
#   .\logs.ps1 redis     # Nur Redis

param(
    [Parameter(Mandatory=$false)]
    [string]$Service = ""
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "========================================"
if ($Service) {
    Write-ColorOutput Green "DHBW Automation - Logs: $Service"
} else {
    Write-ColorOutput Green "DHBW Automation - Logs: Alle Container"
}
Write-ColorOutput Green "========================================"
Write-ColorOutput Yellow "`nDrücke Ctrl+C zum Beenden`n"

if ($Service) {
    ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f $Service"
} else {
    ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f"
}

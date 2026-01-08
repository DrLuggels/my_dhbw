# =============================================================================
# DHBW Automation - Rebuild & Deploy from PC
# =============================================================================
# Dieses Skript committed, pusht und rebuilded die Container auf dem Server
# Verwendung: .\rebuild-and-deploy.ps1 "Commit message"

param(
    [Parameter(Mandatory=$false)]
    [string]$CommitMessage = "Update and rebuild"
)

$ErrorActionPreference = "Stop"

# Farben
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "========================================"
Write-ColorOutput Green "DHBW Automation - Deploy & Rebuild"
Write-ColorOutput Green "========================================"

# Projektverzeichnis
$projectPath = "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw"

Set-Location $projectPath

# Git Status prüfen
Write-ColorOutput Yellow "`n[1/5] Prüfe Git Status..."
git status --short
Write-ColorOutput Green "✓ Status geprüft"

# Änderungen hinzufügen
Write-ColorOutput Yellow "`n[2/5] Füge Änderungen hinzu..."
git add -A
Write-ColorOutput Green "✓ Änderungen hinzugefügt"

# Commit (wenn es Änderungen gibt)
Write-ColorOutput Yellow "`n[3/5] Committe Änderungen..."
try {
    git commit -m $CommitMessage
    Write-ColorOutput Green "✓ Commit erstellt: $CommitMessage"
} catch {
    Write-ColorOutput Yellow "⚠ Keine Änderungen zum Committen"
}

# Push zum Server
Write-ColorOutput Yellow "`n[4/5] Pushe zum Server..."
git push server main
Write-ColorOutput Green "✓ Code gepusht"

# Auf Server rebuilden
Write-ColorOutput Yellow "`n[5/5] Rebuild auf Server..."
Write-ColorOutput Yellow "Dies kann einige Minuten dauern..."

$rebuildScript = @"
cd /root/dhbw-automation-deploy/dhbw-automation/scripts && chmod +x rebuild.sh && ./rebuild.sh
"@

ssh root@192.168.178.198 $rebuildScript

Write-ColorOutput Green "`n========================================"
Write-ColorOutput Green "✓ Deployment & Rebuild abgeschlossen!"
Write-ColorOutput Green "========================================"

Write-ColorOutput Yellow "`nServices verfügbar unter:"
Write-ColorOutput White "  - MinIO Console:  http://192.168.178.198:9003"
Write-ColorOutput White "  - RabbitMQ Mgmt:  http://192.168.178.198:15673"
Write-ColorOutput White "  - Qdrant:         http://192.168.178.198:6335"
Write-ColorOutput White "  - phpMyAdmin:     http://192.168.178.198:8082"

Write-ColorOutput Yellow "`nLogs anzeigen:"
Write-ColorOutput White '  ssh root@192.168.178.198 "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml logs -f"'

# =============================================================================
# DHBW Automation - Deploy aktueller Code (von PC)
# =============================================================================
# Commitet, pusht und deployed Code zum Server
# Verwendung: .\deploy.ps1 "Commit message"

param(
    [Parameter(Mandatory=$false)]
    [string]$CommitMessage = "Update"
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "========================================"
Write-ColorOutput Green "DHBW Automation - Deploy"
Write-ColorOutput Green "========================================"

# Projektverzeichnis
$projectPath = "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw"

Push-Location $projectPath

try {
    # Git Status
    Write-ColorOutput Yellow "`n[1/4] Git Status:"
    git status --short
    
    # Add
    Write-ColorOutput Yellow "`n[2/4] Fuege Aenderungen hinzu..."
    git add -A
    Write-ColorOutput Green "[OK] Aenderungen hinzugefuegt"
    
    # Commit
    Write-ColorOutput Yellow "`n[3/4] Committe..."
    try {
        git commit -m $CommitMessage
        Write-ColorOutput Green "[OK] Commit: $CommitMessage"
    } catch {
        Write-ColorOutput Yellow "[INFO] Keine Aenderungen zum Committen"
    }
    
    # Push
    Write-ColorOutput Yellow "`n[4/4] Pushe zum Server..."
    git push server main
    Write-ColorOutput Green "[OK] Code gepusht"
    
    # Code auf Server auschecken
    Write-ColorOutput Yellow "`nAktualisiere Code auf Server..."
    ssh root@192.168.178.198 "cd /root/git-repos/dhbw-automation.git && git --work-tree=/root/dhbw-automation-deploy --git-dir=/root/git-repos/dhbw-automation.git checkout -f main"
    Write-ColorOutput Green "[OK] Code auf Server aktualisiert"
    
    Write-ColorOutput Green "`n[OK] Deploy abgeschlossen!"
    Write-ColorOutput Yellow "`nContainer neu starten mit:"
    Write-ColorOutput White "  .\scripts\restart.ps1"
    
} finally {
    Pop-Location
}

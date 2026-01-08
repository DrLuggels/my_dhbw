# ========================================================================
# DHBW Automation - E-Mail-Integration Setup Script
# ========================================================================

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "E-Mail-Integration Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Schritt 1: Database Migration
Write-Host "[1/5] Führe Database Migration aus..." -ForegroundColor Yellow

$migrationPath = "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw\dhbw-automation\database\migrations\20260108_email_integration.sql"

if (Test-Path $migrationPath) {
    Write-Host "Migration-Datei gefunden: $migrationPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "Bitte führen Sie die Migration manuell in MySQL aus:" -ForegroundColor Yellow
    Write-Host "mysql -u dhbw_user -p dhbw_automation < '$migrationPath'" -ForegroundColor White
    Write-Host ""
    $continue = Read-Host "Migration durchgeführt? (j/n)"
    
    if ($continue -ne "j") {
        Write-Host "Setup abgebrochen." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "FEHLER: Migration-Datei nicht gefunden!" -ForegroundColor Red
    exit 1
}

# Schritt 2: NuGet Packages überprüfen
Write-Host ""
Write-Host "[2/5] Überprüfe NuGet Packages..." -ForegroundColor Yellow

$infraProject = "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw\dhbw-automation\src\Backend_New\DHBWAutomation.Infrastructure"

Push-Location $infraProject
Write-Host "Stelle sicher, dass MailKit und MimeKit installiert sind..." -ForegroundColor White
dotnet add package MailKit --version 4.3.0
dotnet add package MimeKit --version 4.3.0
Pop-Location

Write-Host "NuGet Packages OK" -ForegroundColor Green

# Schritt 3: Frontend Packages
Write-Host ""
Write-Host "[3/5] Installiere Frontend-Packages..." -ForegroundColor Yellow

$frontendPath = "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw\dhbw-automation\src\Frontend"

if (Test-Path $frontendPath) {
    Push-Location $frontendPath
    Write-Host "Installiere DOMPurify..." -ForegroundColor White
    pnpm add dompurify @types/dompurify
    Pop-Location
    Write-Host "Frontend-Packages OK" -ForegroundColor Green
} else {
    Write-Host "WARNING: Frontend-Verzeichnis nicht gefunden" -ForegroundColor Yellow
}

# Schritt 4: .env konfigurieren
Write-Host ""
Write-Host "[4/5] Konfiguration..." -ForegroundColor Yellow

$envExample = "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw\dhbw-automation\.env.example"
$env = "C:\Users\6000718\OneDrive - Planmeca Oy\Dateien von Moder, Frank - 001_Werksstudent_DataScience_KI 1\Übungen\Projekte\my_dhbw\dhbw-automation\.env"

if (-not (Test-Path $env)) {
    Write-Host "Erstelle .env aus .env.example..." -ForegroundColor White
    Copy-Item $envExample $env
    Write-Host ".env erstellt - bitte konfigurieren!" -ForegroundColor Yellow
} else {
    Write-Host ".env existiert bereits" -ForegroundColor Green
}

# Schritt 5: Zusammenfassung
Write-Host ""
Write-Host "[5/5] Setup abgeschlossen!" -ForegroundColor Green
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Nächste Schritte:" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Backend starten:" -ForegroundColor White
Write-Host "   cd src\Backend_New\DHBWAutomation.API" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Frontend starten:" -ForegroundColor White
Write-Host "   cd src\Frontend" -ForegroundColor Gray
Write-Host "   pnpm dev" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Im Browser öffnen:" -ForegroundColor White
Write-Host "   http://localhost:5173" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Profil öffnen und E-Mail konfigurieren:" -ForegroundColor White
Write-Host "   - E-Mail: Cvitanovic.Luka-25@stud.dhbw-ravensburg.de" -ForegroundColor Gray
Write-Host "   - Passwort: [IHR DHBW-PASSWORT]" -ForegroundColor Gray
Write-Host "   - 'Verbindung testen' klicken" -ForegroundColor Gray
Write-Host "   - E-Mail-Sync aktivieren" -ForegroundColor Gray
Write-Host ""
Write-Host "Der Background Worker synchronisiert automatisch jede Minute!" -ForegroundColor Green
Write-Host ""

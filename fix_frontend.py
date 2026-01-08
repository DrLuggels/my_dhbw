import subprocess
import sys

def run_ssh_command(command, description):
    """Führt SSH-Command aus und zeigt Fortschritt"""
    print(f"\n{'='*60}")
    print(f"  {description}")
    print('='*60)
    
    try:
        result = subprocess.run(
            ['ssh', 'root@192.168.178.198', command],
            capture_output=True,
            text=True,
            timeout=300
        )
        
        if result.stdout:
            print(result.stdout)
        if result.stderr:
            print(f"STDERR: {result.stderr}", file=sys.stderr)
            
        if result.returncode != 0:
            print(f"✗ Fehler (Exit Code: {result.returncode})")
            return False
        else:
            print("✓ Erfolgreich")
            return True
            
    except subprocess.TimeoutExpired:
        print("✗ Timeout nach 5 Minuten")
        return False
    except Exception as e:
        print(f"✗ Fehler: {e}")
        return False

# Schritt 1: Git aktualisieren
if not run_ssh_command(
    "cd /root/git-repos/dhbw-automation.git && GIT_WORK_TREE=/root/dhbw-automation-deploy git reset --hard HEAD",
    "Schritt 1: Git Repository aktualisieren"
):
    sys.exit(1)

# Schritt 2: .env.production prüfen
if not run_ssh_command(
    "cat /root/dhbw-automation-deploy/dhbw-automation/src/Frontend/.env.production",
    "Schritt 2: .env.production prüfen"
):
    print("WARNUNG: .env.production nicht gefunden, wird ausgecheckt...")
    run_ssh_command(
        "cd /root/git-repos/dhbw-automation.git && GIT_WORK_TREE=/root/dhbw-automation-deploy git checkout -f HEAD -- dhbw-automation/src/Frontend/.env.production",
        "  → .env.production auschecken"
    )

# Schritt 3: Dockerfile aktualisieren
run_ssh_command(
    """cd /root/dhbw-automation-deploy/dhbw-automation && \
    cp docker/frontend.Dockerfile docker/frontend.Dockerfile.bak && \
    sed -i '/COPY src\\/Frontend\\/\\. \\./a\\# Ensure .env.production is available for build\\nCOPY src/Frontend/.env.production .\\/' docker/frontend.Dockerfile && \
    echo "Dockerfile aktualisiert:" && \
    grep -A 2 "COPY src/Frontend/\\. \\." docker/frontend.Dockerfile""",
    "Schritt 3: Dockerfile aktualisieren"
)

# Schritt 4: Frontend Container stoppen
run_ssh_command(
    "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml down frontend",
    "Schritt 4: Frontend Container stoppen"
)

# Schritt 5: Frontend neu bauen (OHNE Cache!)
print("\n" + "="*60)
print("  Schritt 5: Frontend neu bauen (kann 2-3 Minuten dauern)")
print("="*60)
print("Bitte warten...")

if not run_ssh_command(
    "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml build --no-cache frontend 2>&1 | tail -30",
    "  → Build wird ausgeführt..."
):
    print("WARNUNG: Build hatte Fehler, versuche trotzdem zu starten...")

# Schritt 6: Frontend Container starten
run_ssh_command(
    "cd /root/dhbw-automation-deploy/dhbw-automation && docker compose -f docker-compose.prod.yml up -d frontend",
    "Schritt 6: Frontend Container starten"
)

# Schritt 7: Status prüfen
import time
print("\nWarte 10 Sekunden auf Container-Start...")
time.sleep(10)

run_ssh_command(
    "docker ps | grep dhbw-frontend",
    "Schritt 7: Container Status"
)

print("\n" + "="*60)
print("  ✓ FERTIG!")
print("="*60)
print("\nTeste jetzt im Browser:")
print("  → http://192.168.178.198:8091")
print("\nHard Refresh (Cache leeren):")
print("  → Windows: Ctrl+Shift+R")
print("  → Mac: Cmd+Shift+R")
print("\nRegistriere einen Test-User und prüfe ob der API-Call")
print("jetzt an http://192.168.178.198:5001 geht!")

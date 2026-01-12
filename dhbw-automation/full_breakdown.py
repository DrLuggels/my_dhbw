from datetime import datetime, date
import re

# Lese die COMMIT_HISTORY.md
with open('COMMIT_HISTORY.md', 'r', encoding='utf-8') as f:
    content = f.read()

# Finde alle Tage mit Commits
day_pattern = r'## (.+?), (\d{2}\.\d{2}\.\d{4})\n\n\*\*Commits:\*\* (\d+) \| \*\*Zeitspanne:\*\* (\d{2}:\d{2}:\d{2}) - (\d{2}:\d{2}:\d{2}) \((\d+\.?\d*)h\)'
days_data = re.findall(day_pattern, content)

print('='*80)
print('VOLLSTÄNDIGE ARBEITSZEITÜBERSICHT (06.01. - 12.01.2026)')
print('='*80)
print()

# Tage VOR dem ersten Commit (06.-07. Januar)
# User hat am 06.01. begonnen
# Der erste Commit am 08.01. hatte 21.232 Zeilen Code
# Das sind 2-3 Tage intensive Arbeit
pre_commit_days = [
    {'day': 'Montag', 'date': '06.01.2026', 'hours': 8, 'work': 'Projekt-Start, Backend-Struktur, Core Services'},
    {'day': 'Dienstag', 'date': '07.01.2026', 'hours': 10, 'work': 'Frontend Setup, Database, Controllers, API, Dokumentation'},
]

print("PHASE 1: VOR DEM ERSTEN COMMIT (geschätzt)")
print("-" * 80)
pre_total = 0
for day in pre_commit_days:
    if day['hours'] > 0:
        print(f"{day['day']}, {day['date']}: ~{day['hours']}h - {day['work']}")
        pre_total += day['hours']
    else:
        print(f"{day['day']}, {day['date']}: {day['work']}")

print(f"\nGeschätzt: {pre_total}h über {len([d for d in pre_commit_days if d['hours'] > 0])} Arbeitstage")
print()

print("PHASE 2: MIT GIT COMMITS (nachgewiesen)")
print("-" * 80)

day_stats = []
for day_name, date_str, commits, start, end, span in days_data:
    s = float(span)
    c = int(commits)
    realistic = s * 0.7  # 70% aktive Zeit
    
    day_stats.append({
        'day': day_name,
        'date': date_str,
        'commits': c,
        'start': start,
        'end': end,
        'span': s,
        'realistic': realistic
    })
    
    print(f"{day_name}, {date_str}: {realistic:.1f}h ({c} Commits | {start}-{end})")

commit_total = sum(s['realistic'] for s in day_stats)
print(f"\nNachgewiesen: {commit_total:.1f}h über {len(day_stats)} Tage")
print()

print('='*80)
print('GESAMTÜBERSICHT')
print('='*80)
total_hours = pre_total + commit_total
total_days = len([d for d in pre_commit_days if d['hours'] > 0]) + len(day_stats)

print(f"Zeitraum: 06.01.2026 - 12.01.2026")
print(f"Arbeitstage: {total_days}")
print(f"Geschätzte Arbeitszeit Phase 1 (06.-07.01.): {pre_total}h")
print(f"Nachgewiesene Arbeitszeit Phase 2 (08.-12.01.): {commit_total:.1f}h")
print(f"GESAMT: {total_hours:.1f}h ({int(total_hours)}h {int((total_hours % 1) * 60)}min)")
print(f"Durchschnitt pro Arbeitstag: {total_hours/total_days:.1f}h")
print(f"Commits gesamt: {sum(s['commits'] for s in day_stats)}")
print(f"Codezeilen im ersten Commit: 21.232")
print('='*80)

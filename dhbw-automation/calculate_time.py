from datetime import datetime
import re

# Lese die COMMIT_HISTORY.md
with open('COMMIT_HISTORY.md', 'r', encoding='utf-8') as f:
    content = f.read()

# Finde alle Tage
day_pattern = r'## (.+?), (\d{2}\.\d{2}\.\d{4})\n\n\*\*Commits:\*\* (\d+) \| \*\*Zeitspanne:\*\* (\d{2}:\d{2}:\d{2}) - (\d{2}:\d{2}:\d{2}) \((\d+\.?\d*)h\)'
days_data = re.findall(day_pattern, content)

print('='*80)
print('ARBEITSZEITBERECHNUNG AUS GIT COMMITS')
print('='*80)
print()

total_span = 0
total_commits = 0

for day_name, date, commits, start, end, span in days_data:
    s = float(span)
    c = int(commits)
    total_span += s
    total_commits += c
    print(f'{day_name}, {date}')
    print(f'  {c:2d} Commits | {start} - {end} | Zeitspanne: {s:5.1f}h')
    print()

print('='*80)
print('VERSCHIEDENE BERECHNUNGSMETHODEN:')
print('='*80)
print()
print(f'1. Reine Zeitspanne (erster bis letzter Commit):')
print(f'   Gesamt: {total_span:.1f}h | Durchschnitt: {total_span/len(days_data):.1f}h/Tag')
print()

# Mit Pausen
pause_total = len(days_data) * 2  # 2h Pause pro Tag
after_pause = total_span - pause_total
print(f'2. Mit Pause-Abzug (2h/Tag):')
print(f'   Gesamt: {after_pause:.1f}h | Durchschnitt: {after_pause/len(days_data):.1f}h/Tag')
print()

# 70% Schätzung
realistic = total_span * 0.7
print(f'3. Realistische Schätzung (70% aktive Arbeit):')
print(f'   Gesamt: {realistic:.1f}h | Durchschnitt: {realistic/len(days_data):.1f}h/Tag')
print()

# Mit Nacharbeit
with_followup = total_span + (len(days_data) * 0.5)
print(f'4. Mit Nacharbeit (+30min/Tag):')
print(f'   Gesamt: {with_followup:.1f}h | Durchschnitt: {with_followup/len(days_data):.1f}h/Tag')
print()

print('='*80)
print('STATISTIK:')
print('='*80)
print(f'Arbeitstage: {len(days_data)}')
print(f'Commits gesamt: {total_commits}')
print(f'Commits/Tag: {total_commits/len(days_data):.1f}')
print(f'Commits/Stunde: {total_commits/total_span:.1f}')
print('='*80)

from datetime import datetime
import re

# Lese die COMMIT_HISTORY.md
with open('COMMIT_HISTORY.md', 'r', encoding='utf-8') as f:
    content = f.read()

# Finde alle Tage
day_pattern = r'## (.+?), (\d{2}\.\d{2}\.\d{4})\n\n\*\*Commits:\*\* (\d+) \| \*\*Zeitspanne:\*\* (\d{2}:\d{2}:\d{2}) - (\d{2}:\d{2}:\d{2}) \((\d+\.?\d*)h\)'
days_data = re.findall(day_pattern, content)

print('='*80)
print('TÄGLICHE ARBEITSZEIT-AUFSCHLÜSSELUNG')
print('='*80)
print()

day_stats = []

for day_name, date, commits, start, end, span in days_data:
    s = float(span)
    c = int(commits)
    
    # Verschiedene Berechnungen
    with_pause = max(0, s - 2.0)  # 2h Pause abziehen, min 0
    realistic = s * 0.7  # 70% aktive Zeit
    with_followup = s + 0.5  # +30min Nacharbeit
    
    day_stats.append({
        'day': day_name,
        'date': date,
        'commits': c,
        'start': start,
        'end': end,
        'span': s,
        'with_pause': with_pause,
        'realistic': realistic,
        'with_followup': with_followup
    })

# Ausgabe
for stat in day_stats:
    print(f"{stat['day']}, {stat['date']}")
    print(f"  {stat['commits']} Commits | {stat['start']} - {stat['end']}")
    print(f"  Zeitspanne:              {stat['span']:5.1f}h")
    print(f"  Mit Pause (-2h):         {stat['with_pause']:5.1f}h")
    print(f"  Realistisch (70%):       {stat['realistic']:5.1f}h")
    print(f"  Mit Nacharbeit (+30min): {stat['with_followup']:5.1f}h")
    print()

print('='*80)
print('ZUSAMMENFASSUNG')
print('='*80)
print()

totals = {
    'span': sum(s['span'] for s in day_stats),
    'with_pause': sum(s['with_pause'] for s in day_stats),
    'realistic': sum(s['realistic'] for s in day_stats),
    'with_followup': sum(s['with_followup'] for s in day_stats),
    'commits': sum(s['commits'] for s in day_stats)
}

print(f"Zeitspanne gesamt:              {totals['span']:.1f}h")
print(f"Mit Pausen (-2h/Tag):           {totals['with_pause']:.1f}h")
print(f"Realistisch (70% aktiv):        {totals['realistic']:.1f}h")
print(f"Mit Nacharbeit (+30min/Tag):    {totals['with_followup']:.1f}h")
print()
print(f"Commits gesamt: {totals['commits']}")
print(f"Arbeitstage: {len(day_stats)}")
print('='*80)

# 🎯 Adaptive Learning System - Implementierungsplan

## Vision
Ein intelligentes Lernsystem, das:
- Automatisch Schwierigkeiten erkennt und Übungen generiert
- Bei ausbleibenden Fortschritten Nachhilfe-Termine vorschlägt
- Dokument-übergreifende Zusammenhänge erkennt
- Zwischen Grundlagen-Festigung und Maximum-Training balanciert

## Status: GEPLANT (Noch nicht implementiert)

---

## Phase 1: Automatische Lerndefizit-Erkennung ✅ (Teilweise fertig)

### Was bereits existiert:
- ✅ `LearningDeficit` Model mit Tracking (OccurrenceCount, Severity)
- ✅ Automatische Fehler-Extraktion aus Dokumenten (`DetectedError`)
- ✅ `AnalyzeDocumentErrorsAsync` - erstellt LearningDeficits
- ✅ NeedsTutoring Flag (jetzt ab 1. Fehler, früher ab 3)

### Was fehlt:
- ❌ "Ich brauche Nachhilfe bei X" → LearningDeficit erstellen
- ❌ "Ich habe Schwierigkeiten mit Y" → LearningDeficit erstellen
- ❌ Pattern-Erkennung im AI Prompt (IntentAnalysisService)

### Implementierung:
**IntentAnalysisService.cs - AI Prompt erweitern:**
```
7. **Lerndefizite**: Erkenne wenn der User Hilfe braucht
   - Patterns: "Nachhilfe", "Schwierigkeiten", "verstehe nicht", "Probleme mit"
   - Beispiele:
     * "Ich brauche Java Nachhilfe" → LearningDeficit (Subject: Programmierung, Topic: Java)
     * "Habe Schwierigkeiten bei Mathe" → LearningDeficit (Subject: Mathematik)
   - NICHT als Meeting extrahieren!
   - Subject, Topic, ErrorDescription, NeedsTutoring=true
```

**ValidationService.cs - LearningDeficit staging:**
```csharp
// 4. Stage LearningDeficits (wenn vorhanden)
for (int i = 0; i < intent.LearningDeficits.Count; i++)
{
    var deficit = intent.LearningDeficits[i];
    var staged = await StageLearningDeficitAsync(deficit, userId, documentId);
    stagedEntities.Add(staged);
}
```

---

## Phase 2: Automatische Übungsgenerierung ✅ (Bereits implementiert)

### Was bereits funktioniert:
- ✅ `GenerateExerciseForDeficitAsync` - erstellt Übungen basierend auf LearningDeficit
- ✅ Spaced Repetition System (SM-2 Algorithm)
- ✅ `NextReviewDate`, `EaseFactor`, `ReviewCount` Tracking
- ✅ Difficulty-Anpassung (high severity → easy exercises)

### User-spezifische API Keys jetzt unterstützt:
- ✅ LearningAnalyticsService nutzt user-spezifische Anthropic Keys
- ✅ Keine Environment Variable mehr nötig

---

## Phase 3: Eskalation - Übungen → Nachhilfe-Termin 🔄 (In Planung)

### Konzept:
1. **Übungen werden generiert** (bereits implementiert)
2. **Tracking ob Übungen gemacht werden**
3. **Eskalation bei ausbleibenden Fortschritten:**
   - Übung nicht gemacht nach 3 Tagen → Reminder
   - Übung 3x falsch gelöst → Schwierigkeit senken
   - Übungen 7+ Tage ignoriert → **Nachhilfe-Termin vorschlagen**

### Implementierung:

**Neue Tabelle: `ExerciseAttempts`**
```sql
CREATE TABLE ExerciseAttempts (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ExerciseId INT NOT NULL,
    UserId INT NOT NULL,
    IsCorrect BOOLEAN NOT NULL,
    TimeSpentSeconds INT,
    AttemptedAt DATETIME NOT NULL,
    UserAnswer TEXT,
    FOREIGN KEY (ExerciseId) REFERENCES GeneratedExercises(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

**Background Worker: `ExerciseEscalationWorker`**
```csharp
public class ExerciseEscalationWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var overdueExercises = await GetOverdueExercises(); // 7+ days

            foreach (var exercise in overdueExercises)
            {
                // Erstelle Meeting-Vorschlag für Nachhilfe
                var meeting = new StagedEntity
                {
                    EntityType = "meeting",
                    EntityData = JsonSerializer.Serialize(new ExtractedMeeting
                    {
                        PersonName = "Tutor (zu finden)",
                        Purpose = $"Nachhilfe: {exercise.Subject} - {exercise.Topic}",
                        SuggestedDate = DateTime.Now.AddDays(3),
                        EstimatedDurationMinutes = 90
                    }),
                    Priority = "high",
                    Status = "pending_review"
                };

                await _context.StagedEntities.AddAsync(meeting);
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
```

---

## Phase 4: Cross-Document Context Analysis 🔮 (Zukunft)

### Konzept:
**Problem:** AI sieht nur einzelnes Dokument, keine Zusammenhänge
**Lösung:** Vector Database (Qdrant) für semantische Suche

### Implementierung:

**1. Dokumente in Qdrant speichern**
```csharp
public async Task IndexDocumentAsync(Document doc)
{
    var embedding = await _aiService.GetEmbeddingAsync(doc.ExtractedText);

    await _qdrantClient.UpsertAsync("documents", new[]
    {
        new PointStruct
        {
            Id = doc.Id,
            Vectors = embedding,
            Payload = new Dictionary<string, object>
            {
                ["text"] = doc.ExtractedText,
                ["subject"] = doc.DetectedSubject,
                ["created_at"] = doc.CreatedAt,
                ["category"] = doc.DocumentCategory
            }
        }
    });
}
```

**2. Kontext bei Intent-Analyse hinzufügen**
```csharp
public async Task<DocumentIntent> AnalyzeWithContextAsync(string text, int userId)
{
    // 1. Finde ähnliche Dokumente
    var embedding = await _aiService.GetEmbeddingAsync(text);
    var similar = await _qdrantClient.SearchAsync("documents", embedding, limit: 5);

    // 2. Baue Kontext
    var context = string.Join("\n\n", similar.Select(s =>
        $"Früher: {s.Payload["text"]} (Thema: {s.Payload["subject"]})"
    ));

    // 3. AI Prompt mit Kontext
    var systemPrompt = $@"Analysiere dieses Dokument.

KONTEXT aus früheren Dokumenten:
{context}

Erkenne Zusammenhänge:
- Ist das eine Fortsetzung eines früheren Themas?
- Baut das auf früheren Fehlern auf?
- Gibt es wiederkehrende Schwierigkeiten?
";

    return await AnalyzeDocumentIntentAsync(text, systemPrompt, userId);
}
```

**Beispiel-Erkennung:**
- Doc 1: "Mathe Aufgabe falsch gelöst: 2+2=5"
- Doc 2: "Mathe Test: Wieder Fehler bei Addition"
- Doc 3: "Ich verstehe Mathe nicht"
→ **AI erkennt**: Wiederkehrende Grundlagen-Probleme bei Addition → Höchste Priorität

---

## Phase 5: Adaptive Difficulty & Balance 🎮 (Zukunft)

### Konzept:
**"Am Maximum trainieren, ohne Grundlagen zu vernachlässigen"**

### Implementierung:

**1. User Skill Tracking**
```sql
CREATE TABLE UserSkills (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    Subject VARCHAR(100) NOT NULL,
    Topic VARCHAR(100) NOT NULL,
    SkillLevel DECIMAL(3,2) DEFAULT 0.00, -- 0.00 = Anfänger, 1.00 = Experte
    LastPracticed DATETIME,
    ConsecutiveCorrect INT DEFAULT 0,
    UpdatedAt DATETIME NOT NULL,
    UNIQUE KEY (UserId, Subject, Topic)
);
```

**2. Adaptive Exercise Generation**
```csharp
public async Task<GeneratedExercise> GenerateAdaptiveExerciseAsync(int userId, string subject)
{
    // 1. Hole User Skills
    var skills = await _context.UserSkills
        .Where(u => u.UserId == userId && u.Subject == subject)
        .ToListAsync();

    // 2. Finde schwächsten Skill (Grundlage)
    var weakest = skills.OrderBy(s => s.SkillLevel).FirstOrDefault();

    // 3. Finde stärksten Skill (Maximum)
    var strongest = skills.OrderByDescending(s => s.SkillLevel).FirstOrDefault();

    // 4. Balance-Strategie: 70% Maximum, 30% Grundlagen
    var useMaximum = Random.Shared.NextDouble() < 0.7;
    var targetSkill = useMaximum ? strongest : weakest;

    // 5. Difficulty basierend auf Skill Level
    var difficulty = targetSkill.SkillLevel switch
    {
        < 0.3f => "easy",
        < 0.6f => "medium",
        < 0.8f => "hard",
        _ => "expert"
    };

    // 6. Generiere Übung
    return await GenerateExerciseAsync(targetSkill.Topic, difficulty);
}
```

**3. Skill Level Update nach Übung**
```csharp
public async Task UpdateSkillLevelAsync(int userId, int exerciseId, bool isCorrect)
{
    var exercise = await _context.GeneratedExercises.FindAsync(exerciseId);
    var skill = await GetOrCreateSkillAsync(userId, exercise.Subject, exercise.Topic);

    if (isCorrect)
    {
        skill.ConsecutiveCorrect++;
        skill.SkillLevel += 0.05f * (1.0f - skill.SkillLevel); // Diminishing returns
    }
    else
    {
        skill.ConsecutiveCorrect = 0;
        skill.SkillLevel = Math.Max(0, skill.SkillLevel - 0.1f);
    }

    skill.LastPracticed = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```

---

## Prioritäten & Timeline

### Kurzfristig (nächste Session):
1. ✅ Backend Cache-Problem lösen (läuft gerade)
2. ⏳ Frontend deployment abwarten
3. ⏳ Testen ob Nachhilfe-Übungen jetzt funktionieren

### Mittelfristig (nächste Woche):
1. 🎯 **Phase 1**: "Nachhilfe brauchen" Pattern erkennen → LearningDeficit
2. 🎯 **Phase 3**: Eskalation - Übungen → Meeting vorschlagen
3. 🎯 Test mit echten Daten

### Langfristig (nächster Monat):
1. 🔮 **Phase 4**: Cross-Document Context (Qdrant Integration)
2. 🔮 **Phase 5**: Adaptive Difficulty System
3. 🔮 Frontend für Exercise Review

---

## Technische Voraussetzungen

### Bereits vorhanden:
- ✅ MariaDB (Primärdatenbank)
- ✅ Qdrant (Vector Database) - läuft bereits im Docker Compose
- ✅ LearningDeficit Model
- ✅ GeneratedExercise Model
- ✅ User-spezifische API Keys
- ✅ Background Services Framework

### Noch zu implementieren:
- ❌ ExerciseAttempts Tabelle
- ❌ UserSkills Tabelle
- ❌ ExerciseEscalationWorker
- ❌ Qdrant Integration (Client existiert, aber ungenutzt)
- ❌ Frontend für Exercise Review

---

## Nächster Schritt
**Warten auf Deployment, dann testen ob Nachhilfe-System funktioniert!**

Wenn das klappt, implementieren wir Phase 1 (Nachhilfe-Pattern-Erkennung).

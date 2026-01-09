# AI Staging System - Dokumentation

## Übersicht

Das AI Staging System stellt sicher, dass **alle AI-extrahierten Daten validiert werden**, bevor sie in die Produktiv-Datenbank geschrieben werden. Die AI kann bei unklaren Informationen **Rückfragen** stellen, bevor Entitäten erstellt werden.

**Status:** ✅ Produktiv (seit 10.01.2026)

---

## Problem

### Vorher (ALT)
```
Dokument → AI analysiert → DIREKT in DB geschrieben
```

**Probleme:**
- ❌ Ungenaue Daten in der Datenbank
- ❌ Keine Validierung durch User
- ❌ AI-Fehler werden nicht korrigiert
- ❌ Unklare Informationen führen zu unvollständigen Einträgen

**Beispiel:**
```
Text: "Treffe Paulina nächste Woche für Matheprojekt"
AI erstellt direkt: Meeting mit Paulina
  ❌ Wann? → NULL
  ❌ Uhrzeit? → NULL
  ❌ Dauer? → 60 Min (Annahme)
```

### Jetzt (NEU)
```
Dokument → AI analysiert + Confidence Score + Fragen
         ↓
     Staging-Tabelle (wartet auf User)
         ↓
     User bestätigt/korrigiert/beantwortet Fragen
         ↓
     Produktiv-DB (100% validiert!)
```

**Vorteile:**
- ✅ 100% validierte Daten
- ✅ User hat volle Kontrolle
- ✅ AI stellt gezielte Rückfragen
- ✅ Transparenz über AI-Extraktionen
- ✅ Saubere Datenbank

---

## Architektur

### Database Schema

#### StagedEntities Tabelle
Hält extrahierte Entitäten bis zur User-Bestätigung.

```sql
CREATE TABLE StagedEntities (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    SourceDocumentId INT NULL,
    EntityType VARCHAR(50) NOT NULL, -- 'todo', 'meeting', 'project', etc.
    EntityData TEXT NOT NULL,        -- JSON-serialisierte Daten
    ConfidenceScore INT NOT NULL,    -- 0-100
    Status VARCHAR(50) DEFAULT 'pending_review',
    Priority VARCHAR(20) DEFAULT 'medium',
    IsPromoted BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

**ConfidenceScore Bedeutung:**
- **90-100%**: Sehr sicher, alle Daten klar
- **70-89%**: Unsicher, einige Daten fehlen (→ Fragen generieren)
- **0-69%**: Sehr unklar, kritische Daten fehlen (→ viele Fragen)

#### AIQuestions Tabelle
Speichert Rückfragen der AI zu unklaren Feldern.

```sql
CREATE TABLE AIQuestions (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    StagedEntityId INT NOT NULL,
    FieldName VARCHAR(100) NOT NULL,      -- z.B. "meeting.suggestedDate"
    QuestionText VARCHAR(500) NOT NULL,   -- "Wann möchtest du Paulina treffen?"
    SuggestedAnswers TEXT NULL,           -- JSON: ["Montag", "Mittwoch", ...]
    Priority VARCHAR(20) DEFAULT 'medium', -- critical, high, medium, low
    IsAnswered BOOLEAN DEFAULT FALSE,
    UserAnswer TEXT NULL,
    AnswerType VARCHAR(20) DEFAULT 'text' -- text, date, time, datetime, choice, number
);
```

**Priority Levels:**
- **critical**: Entität ist OHNE diese Info unbrauchbar (z.B. Person bei Meeting fehlt)
- **high**: Stark empfohlen (z.B. Datum bei Meeting fehlt)
- **medium**: Hilfreich (z.B. Dauer bei Meeting fehlt)
- **low**: Optional (z.B. Beschreibung bei TODO fehlt)

---

## API Endpoints

### 1. Ausstehende Entitäten abrufen

```http
GET /api/validation/pending?status=pending_review
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "count": 3,
  "entities": [
    {
      "id": 1,
      "entityType": "meeting",
      "entityData": "{\"personName\":\"Paulina\",\"purpose\":\"Matheprojekt besprechen\"}",
      "confidenceScore": 65,
      "status": "pending_review",
      "priority": "high",
      "questions": [
        {
          "id": 1,
          "fieldName": "meeting.suggestedDate",
          "questionText": "Wann möchtest du Paulina treffen?",
          "suggestedAnswers": "[\"Montag 14:00\",\"Mittwoch 16:00\",\"Freitag 10:00\"]",
          "priority": "high",
          "answerType": "datetime",
          "isAnswered": false
        }
      ]
    }
  ],
  "summary": {
    "highPriority": 1,
    "withQuestions": 1,
    "lowConfidence": 1
  }
}
```

### 2. Fragen beantworten

```http
POST /api/validation/{id}/answer
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "answers": {
    "meeting.suggestedDate": "Mittwoch 16:00",
    "meeting.suggestedTime": "16:00"
  }
}
```

**Response:**
```json
{
  "message": "2 Fragen beantwortet"
}
```

### 3. Entität bestätigen & in Produktiv-DB übertragen

```http
POST /api/validation/{id}/confirm
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "userNotes": "Meeting mit Paulina am Mittwoch um 16:00"
}
```

**Response:**
```json
{
  "message": "Entität erfolgreich bestätigt und in Produktiv-DB übertragen",
  "promotedEntityId": 42
}
```

### 4. Entität ablehnen

```http
POST /api/validation/{id}/reject
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "reason": "Ist doch kein Meeting, nur eine Notiz"
}
```

### 5. Bulk-Bestätigung (Auto-Promote)

```http
POST /api/validation/bulk-confirm?minConfidence=95
Authorization: Bearer {jwt_token}
```

Bestätigt automatisch alle Entitäten mit Confidence >= 95% und ohne offene Fragen.

### 6. Statistiken abrufen

```http
GET /api/validation/statistics?days=30
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "totalStaged": 50,
  "totalConfirmed": 42,
  "totalRejected": 3,
  "totalModified": 5,
  "averageConfidenceScore": 82.5,
  "totalQuestions": 78,
  "averageQuestionsPerEntity": 1.56,
  "questionsByPriority": {
    "critical": 5,
    "high": 23,
    "medium": 35,
    "low": 15
  }
}
```

---

## Code-Beispiele

### Backend: Document Processing mit Staging

```csharp
// FileService.cs - ProcessDocumentAsync
var intent = await _intentService.AnalyzeDocumentIntentAsync(extractedText, document.DocumentCategory.ToString());

// Statt direkt TODOs zu erstellen, nutzen wir Staging:
if (intent != null && options.EnableIntentAnalysis)
{
    // Stage ALL extracted entities
    var stagedEntities = await _validationService.StageEntitiesAsync(
        intent,
        document.UserId,
        documentId
    );

    _logger.LogInformation($"Staged {stagedEntities.Count} entities (Confidence: {intent.ConfidenceScore}%, Questions: {intent.Questions.Count})");

    // Optional: Auto-promote sehr sichere Entitäten
    if (options.AutoPromoteHighConfidence)
    {
        foreach (var staged in stagedEntities.Where(s => s.ConfidenceScore >= 95 && s.Questions.Count == 0))
        {
            await _validationService.ConfirmAndPromoteAsync(staged.Id, document.UserId, "Auto-promoted");
        }
    }
}
```

### AI Prompt (Claude Sonnet 4.5)

Der erweiterte Prompt generiert jetzt Confidence Scores und Fragen:

```
Für jede extrahierte Entität (Meeting, TODO, Projekt):
- Gib einen ConfidenceScore (0-100) an:
  * 90-100: Sehr sicher, alle Daten klar
  * 70-89: Unsicher, einige Daten fehlen
  * 0-69: Sehr unklar, kritische Daten fehlen

Wenn Daten unklar/fehlen (ConfidenceScore < 90):
- Erstelle Klärungsfragen im "questions"-Array

Beispiel:
{
  "meeting": {
    "personName": "Paulina",
    "purpose": "Matheprojekt besprechen",
    "suggestedDate": null,
    "confidenceScore": 65
  },
  "questions": [
    {
      "fieldName": "meeting.suggestedDate",
      "questionText": "Wann möchtest du Paulina treffen?",
      "suggestedAnswers": ["Montag 14:00", "Mittwoch 16:00", "Freitag 10:00"],
      "priority": "high",
      "answerType": "datetime"
    }
  ]
}
```

### Frontend: Staging Confirmation UI

```typescript
// Beispiel Vue Component
const pendingEntities = await api.get('/api/validation/pending');

for (const entity of pendingEntities.entities) {
  // Zeige Entität mit Fragen
  console.log(`${entity.entityType} (Confidence: ${entity.confidenceScore}%)`);

  for (const question of entity.questions) {
    const answer = await showQuestionDialog(question);
    answers[question.fieldName] = answer;
  }

  // Beantworte Fragen
  await api.post(`/api/validation/${entity.id}/answer`, { answers });

  // Bestätige Entität
  await api.post(`/api/validation/${entity.id}/confirm`);
}
```

---

## Workflow

### End-to-End Beispiel

1. **User lädt Dokument hoch:**
   ```
   "Treffe Paulina nächste Woche für Matheprojekt"
   ```

2. **AI analysiert (IntentAnalysisService):**
   ```json
   {
     "primaryIntent": "schedule_meeting",
     "confidenceScore": 65,
     "meeting": {
       "personName": "Paulina",
       "purpose": "Matheprojekt besprechen",
       "suggestedDate": null,
       "suggestedTime": null,
       "confidenceScore": 65
     },
     "questions": [
       {
         "fieldName": "meeting.suggestedDate",
         "questionText": "Wann möchtest du Paulina treffen?",
         "suggestedAnswers": ["Montag 14:00", "Mittwoch 16:00", "Freitag 10:00", "Nächste Woche"],
         "priority": "high",
         "answerType": "datetime"
       },
       {
         "fieldName": "meeting.suggestedTime",
         "questionText": "Um welche Uhrzeit ungefähr?",
         "suggestedAnswers": ["10:00", "14:00", "16:00", "18:00"],
         "priority": "medium",
         "answerType": "time"
       }
     ]
   }
   ```

3. **Staging (ValidationService.StageEntitiesAsync):**
   - Erstellt `StagedEntity` mit ID 1
   - Erstellt 2 `AIQuestions` (Datum + Uhrzeit)
   - Status: `pending_review`

4. **User wird benachrichtigt:**
   - Dashboard zeigt: "1 neue Entität benötigt Bestätigung"
   - Confidence Badge: 65% (Orange)
   - Fragen: 2 offen

5. **User beantwortet Fragen:**
   ```http
   POST /api/validation/1/answer
   {
     "answers": {
       "meeting.suggestedDate": "Mittwoch 16:00",
       "meeting.suggestedTime": "16:00"
     }
   }
   ```

6. **ValidationService aktualisiert EntityData:**
   - Fragen werden als `IsAnswered = true` markiert
   - EntityData wird mit Antworten ergänzt
   - Status bleibt `pending_review` (wartet auf finale Bestätigung)

7. **User bestätigt:**
   ```http
   POST /api/validation/1/confirm
   ```

8. **ValidationService.ConfirmAndPromoteAsync:**
   - Erstellt `CalendarEvent` in Produktiv-DB
   - Markiert StagedEntity als `IsPromoted = true`
   - Speichert `PromotedEntityId = 42`

9. **User sieht:**
   - Neues Meeting im Kalender: "Meeting mit Paulina" am Mittwoch 16:00
   - ✅ Alle Daten vollständig und validiert

---

## Configuration

### ProcessingOptions

```csharp
// Default: Alle Entitäten gehen durch Staging
ProcessingOptions.Default.AutoPromoteHighConfidence = false;

// Fast: Auto-Promote sehr sichere Entitäten (>= 95% Confidence)
ProcessingOptions.Fast.AutoPromoteHighConfidence = true;

// Full: Manuelle Review für wichtige Dokumente
ProcessingOptions.Full.AutoPromoteHighConfidence = false;
```

---

## Monitoring & Metrics

### Dashboard-Metriken

- **Staging Queue**: Anzahl ausstehender Entitäten
- **Average Confidence Score**: Durchschnittliche AI-Sicherheit
- **Questions per Entity**: Durchschnittliche Fragen pro Entität
- **Acceptance Rate**: % bestätigter vs. abgelehnter Entitäten
- **Response Time**: Zeit bis User Fragen beantwortet

### Logging

```csharp
_logger.LogInformation($"Staged {stagedEntities.Count} entities for user {userId} from document {documentId}");
_logger.LogInformation($"Auto-promoted {staged.EntityType} {promotedId} (Confidence: {staged.ConfidenceScore}%)");
_logger.LogInformation($"User answered {answers.Count} questions for staged entity {stagedEntityId}");
```

---

## Best Practices

### Für AI-Prompts

1. **Confidence Scores ehrlich berechnen:**
   - Nicht künstlich erhöhen
   - Fehlende Daten = niedrigerer Score

2. **Gute Fragen formulieren:**
   - Klar und verständlich
   - Relevante Antwortoptionen
   - Richtige Priority setzen

3. **SuggestedAnswers nutzen:**
   - Erleichtert User-Eingabe
   - Reduziert Fehler
   - Schnellere Beantwortung

### Für Backend-Entwickler

1. **Staging immer verwenden** außer:
   - System-generierte Daten (z.B. Logs)
   - 100% sichere Daten (z.B. API-Responses)

2. **Auto-Promote sparsam einsetzen:**
   - Nur bei >= 95% Confidence
   - Nur wenn 0 offene Fragen
   - Bei kritischen Daten: Immer manuell

3. **Questions richtig priorisieren:**
   - `critical`: Ohne Antwort ist Entität wertlos
   - `high`: Stark empfohlen für Qualität
   - `medium`: Nice-to-have
   - `low`: Optional

### Für Frontend-Entwickler

1. **User Experience:**
   - Staging-Queue prominent anzeigen
   - Fragen als Dialog/Wizard
   - Batch-Bestätigung für einfache Fälle

2. **Visualisierung:**
   - Confidence-Badge mit Farbcode
   - Anzahl offener Fragen
   - Priorität der Entität

3. **Bulk-Actions:**
   - "Alle mit 95%+ bestätigen"
   - "Alle ablehnen"
   - Filter nach EntityType

---

## Migration & Rollout

### Status

- ✅ Database Schema erstellt (StagedEntities, AIQuestions)
- ✅ ValidationService implementiert
- ✅ IntentAnalysisService erweitert (Confidence + Questions)
- ✅ FileService angepasst (nutzt Staging)
- ✅ API Controller erstellt (ValidationController)
- ✅ Services registriert (Program.cs)
- ✅ Migration auf Server ausgeführt (10.01.2026)
- ⏳ Frontend: Confirmation UI (TODO)

### Backward Compatibility

Das alte System (UserInteractions) läuft weiterhin parallel für Kompatibilität:

```csharp
// Legacy: UserInteractions werden noch erstellt
if (intent != null && options.GenerateInteractions)
{
    var interactions = await _intentService.GenerateInteractionsAsync(...);
    _context.UserInteractions.AddRange(interactions);
}
```

---

## Troubleshooting

### Problem: Entität kann nicht bestätigt werden

**Fehler:** `"Entität konnte nicht bestätigt werden"`

**Lösung:** Prüfe, ob alle `critical`-Fragen beantwortet sind:
```sql
SELECT * FROM AIQuestions
WHERE StagedEntityId = {id}
AND Priority = 'critical'
AND IsAnswered = FALSE;
```

### Problem: Staging-Queue wächst zu stark

**Ursache:** Auto-Promote ist deaktiviert und User bestätigt nicht schnell genug

**Lösung:**
1. Bulk-Bestätigung aktivieren: `POST /api/validation/bulk-confirm?minConfidence=95`
2. Auto-Promote aktivieren: `options.AutoPromoteHighConfidence = true`

### Problem: Zu viele Fragen

**Ursache:** AI ist zu unsicher (niedrige Confidence)

**Lösung:**
1. AI-Prompt verbessern (klarere Anweisungen)
2. Training-Daten verbessern
3. Question-Threshold erhöhen (nur bei < 80% Fragen stellen)

---

## Roadmap

### Phase 1 (✅ Abgeschlossen)
- Backend-Implementierung
- Database Schema
- API Endpoints
- Migration

### Phase 2 (⏳ In Arbeit)
- Frontend Confirmation UI
- Dashboard Integration
- Notifications

### Phase 3 (Geplant)
- ML-basierte Confidence-Verbesserung
- User-Feedback-Loop (AI lernt aus Korrekturen)
- Smart Bulk-Actions
- Mobile App Integration

---

## Fazit

Das AI Staging System stellt sicher, dass die Datenbank **immer sauber und validiert** bleibt. Die AI kann gezielt **Rückfragen** stellen, wenn Informationen unklar sind, und der User hat **volle Kontrolle** über alle AI-Extraktionen.

**Ergebnis: 100% Datenqualität** ✨

---

**Version:** 1.0
**Datum:** 10.01.2026
**Autor:** Claude Sonnet 4.5
**Status:** Produktiv

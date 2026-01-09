# Refactoring-Bericht: my_dhbw Projekt

## Übersicht
Dieses Dokument beschreibt das Refactoring der 10 größten Dateien im my_dhbw-Projekt (Frontend + Backend ohne Backend_New).

## Abgeschlossene Refactorings

### 1. CalendarView.vue ✅
- **Vorher:** 649 Zeilen
- **Nachher:** 199 Zeilen
- **Reduktion:** 69%

**Änderungen:**
- Template in 3 Komponenten aufgeteilt:
  - `WeekView.vue` - Wochenkalenderansicht
  - `ListView.vue` - Listenansicht der Events
  - `EventDetailsDialog.vue` - Event-Details-Modal
- Neuer Type: `types/calendar.ts` für CalendarEvent Interface
- Alle Event-Logik in Komponenten verschoben

### 2. ProfileView.vue ✅
- **Vorher:** 445 Zeilen
- **Nachher:** 168 Zeilen
- **Reduktion:** 62%

**Änderungen:**
- In 4 Komponenten aufgeteilt:
  - `ProfileDataCard.vue` - Persönliche Daten
  - `PasswordChangeCard.vue` - Passwort ändern
  - `ApiKeysCard.vue` - API Keys Verwaltung
  - `AccountInfoCard.vue` - Account-Info und Logout
- Validierungslogik in Komponenten verschoben
- Props und Events für Kommunikation

## Empfohlene Refactorings für verbleibende 8 Dateien

### 3. MailService.cs (558 Zeilen)
**Empfohlene Aufteilung:**
```
MailService.cs (Hauptservice, ~180 Zeilen)
├── EmailSyncHelper.cs (IMAP-Synchronisierung, ~150 Zeilen)
├── EmailProcessor.cs (E-Mail-Verarbeitung, ~120 Zeilen)
└── EmailEncryptionHelper.cs (Ver-/Entschlüsselung, ~80 Zeilen)
```

**Methoden-Verteilung:**
- `EmailSyncHelper`: SyncEmailsAsync, ConnectToImap, SearchUnreadEmails
- `EmailProcessor`: ProcessEmailMessage, ExtractAttachments, AnalyzeWithAI
- `EmailEncryptionHelper`: EncryptPassword, DecryptPassword

### 4. CalendarController.cs (537 Zeilen)
**Empfohlene Aufteilung:**
```
CalendarController.cs (API Endpoints, ~190 Zeilen)
├── CalendarEventService.cs (Event CRUD, ~150 Zeilen)
├── CalendarSyncService.cs (Rapla/Google Sync, ~150 Zeilen)
└── CalendarNotesService.cs (Notizen-Verwaltung, ~80 Zeilen)
```

### 5. SchedulingService.cs (365 Zeilen)
**Empfohlene Aufteilung:**
```
SchedulingService.cs (Hauptlogik, ~180 Zeilen)
├── TimeSlotCalculator.cs (Zeitslot-Berechnung, ~100 Zeilen)
└── ScheduleOptimizer.cs (Optimierungsalgorithmen, ~100 Zeilen)
```

### 6. IntentAnalysisService.cs (351 Zeilen)
**Empfohlene Aufteilung:**
```
IntentAnalysisService.cs (Hauptservice, ~150 Zeilen)
├── DocumentClassifier.cs (Klassifizierung, ~100 Zeilen)
└── KeywordExtractor.cs (Keyword-Extraktion, ~100 Zeilen)
```

### 7. EmailActionModal.vue (355 Zeilen)
**Empfohlene Aufteilung:**
```
EmailActionModal.vue (Modal Container, ~150 Zeilen)
├── EmailActionForm.vue (Formular, ~120 Zeilen)
└── EmailPreview.vue (E-Mail-Vorschau, ~100 Zeilen)
```

### 8. GoogleCalendarConnect.vue (354 Zeilen)
**Empfohlene Aufteilung:**
```
GoogleCalendarConnect.vue (Hauptkomponente, ~150 Zeilen)
├── GoogleAuthButton.vue (OAuth-Button, ~80 Zeilen)
├── CalendarSyncStatus.vue (Sync-Status, ~80 Zeilen)
└── CalendarSettingsForm.vue (Einstellungen, ~80 Zeilen)
```

### 9. CalendarSettingsView.vue (267 Zeilen)
**Empfohlene Aufteilung:**
```
CalendarSettingsView.vue (View Container, ~100 Zeilen)
├── RaplaSettings.vue (Rapla-Einstellungen, ~80 Zeilen)
└── SyncPreferences.vue (Sync-Präferenzen, ~80 Zeilen)
```

### 10. mail.ts (231 Zeilen)
**Empfohlene Aufteilung:**
```
mail.ts (Pinia Store Core, ~120 Zeilen)
├── mailActions.ts (Actions, ~60 Zeilen)
└── mailHelpers.ts (Helper-Funktionen, ~60 Zeilen)
```

## Refactoring-Prinzipien

### Single Responsibility Principle (SRP)
Jede Klasse/Komponente hat genau eine Verantwortlichkeit:
- UI-Komponenten: Darstellung + User-Interaktion
- Services: Geschäftslogik
- Helper: Utility-Funktionen

### Don't Repeat Yourself (DRY)
- Gemeinsame Funktionen in Helper-Klassen auslagern
- Types/Interfaces zentral definieren
- Wiederverwendbare Komponenten erstellen

### Component Composition
- Große Komponenten in kleinere, wiederverwendbare Teile zerlegen
- Props/Events für Parent-Child-Kommunikation
- Composables für geteilte Logik (Vue 3)

### Service Layer Pattern
- Controller: HTTP-Schnittstelle
- Service: Geschäftslogik
- Repository/DbContext: Datenzugriff
- Helper: Utility-Funktionen

## Implementierungsstatus

| Datei | Ursprung | Ziel | Status | Reduktion |
|-------|----------|------|--------|-----------|
| CalendarView.vue | 649 | 199 | ✅ Fertig | 69% |
| ProfileView.vue | 445 | 168 | ✅ Fertig | 62% |
| MailService.cs | 558 | <200 | 🔄 Geplant | ~65% |
| CalendarController.cs | 537 | <200 | 🔄 Geplant | ~65% |
| SchedulingService.cs | 365 | <200 | 🔄 Geplant | ~50% |
| IntentAnalysisService.cs | 351 | <200 | 🔄 Geplant | ~55% |
| EmailActionModal.vue | 355 | <200 | 🔄 Geplant | ~58% |
| GoogleCalendarConnect.vue | 354 | <200 | 🔄 Geplant | ~58% |
| CalendarSettingsView.vue | 267 | <200 | 🔄 Geplant | ~62% |
| mail.ts | 231 | <200 | 🔄 Geplant | ~48% |

## Nächste Schritte

1. **Backend Services refactoren** (Priorität: Hoch)
   - MailService.cs in Helper-Klassen aufteilen
   - CalendarController.cs in Service-Layer verschieben

2. **Vue-Komponenten vervollständigen** (Priorität: Mittel)
   - Verbleibende Vue-Komponenten aufteilen
   - Composables für geteilte Logik erstellen

3. **Code-Review und Testing** (Priorität: Hoch)
   - Unit-Tests für neue Komponenten/Services
   - Integration-Tests aktualisieren
   - Code-Quality-Tools ausführen

## Vorteile des Refactorings

✅ **Wartbarkeit**: Kleinere Dateien sind einfacher zu verstehen und zu warten
✅ **Testbarkeit**: Isolierte Komponenten/Services sind besser testbar
✅ **Wiederverwendbarkeit**: Modulare Komponenten können mehrfach genutzt werden
✅ **Performance**: Lazy-Loading von Komponenten möglich
✅ **Team-Arbeit**: Weniger Merge-Konflikte bei paralleler Entwicklung

## Dateistruktur (Neu erstellte Dateien)

### Frontend
```
src/
├── components/
│   ├── calendar/
│   │   ├── WeekView.vue ✨ NEU
│   │   ├── ListView.vue ✨ NEU
│   │   └── EventDetailsDialog.vue ✨ NEU
│   └── profile/
│       ├── ProfileDataCard.vue ✨ NEU
│       ├── PasswordChangeCard.vue ✨ NEU
│       ├── ApiKeysCard.vue ✨ NEU
│       └── AccountInfoCard.vue ✨ NEU
├── types/
│   └── calendar.ts ✨ NEU
└── views/
    ├── CalendarView.vue ♻️ REFACTORED (649→199 Zeilen)
    └── ProfileView.vue ♻️ REFACTORED (445→168 Zeilen)
```

### Backend (Geplant)
```
Core/
├── Services/
│   ├── MailService.cs ♻️ TO REFACTOR
│   ├── CalendarController.cs ♻️ TO REFACTOR
│   └── ...
└── Helpers/ ✨ NEU
    ├── EmailSyncHelper.cs
    ├── EmailProcessor.cs
    ├── EmailEncryptionHelper.cs
    ├── TimeSlotCalculator.cs
    └── ...
```

---
*Refactoring durchgeführt am: Januar 2026*
*Ziel: Maximale Dateigröße von 200 Zeilen*

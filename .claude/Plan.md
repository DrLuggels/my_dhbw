# Wissenschaftliche Analyse & Fusionsplan: Omnifunktionales Lernsystem

## Executive Summary

Basierend auf einer umfassenden Analyse aller existierenden Subsysteme wird ein **vereinheitlichtes adaptives Lernsystem** vorgeschlagen, das die **Learning Engine als Kernarchitektur** verwendet und alle anderen Systeme integriert.

---

## Teil I: Wissenschaftliche Systemanalyse

### 1. Identifizierte Lernsysteme

| System | Zweck | Algorithmus | Stärken | Schwächen |
|--------|-------|-------------|---------|-----------|
| **LearningEngineService** | Adaptive Wissensextraktion & Fragen | FSRS (vereinfacht) + Bloom's Taxonomy | Claude-basierte Entitätsextraktion, 12 Entitätstypen, 13 Beziehungstypen, Evidence-Tracking | Kein Zeitverfall, vereinfachte FSRS-Implementierung |
| **InteractiveExerciseService** | Brilliant.org-Style Übungen | SM-2 Spaced Repetition | 6 Komponententypen, Multi-Step-Progression, Hint-System | Gemini statt Claude, keine Bloom-Integration |
| **KnowledgeNetworkService** | Wissensvisualisierung | Semantische Ähnlichkeit (0.8 Threshold) | Flexible Entity-Types, User-Feedback-Loop, 2D-Projektion | Keine standardisierten Beziehungstypen, kein Zeitverfall |
| **UnifiedLearningService** | Fusion (in Entwicklung) | FSRS (vollständig W0-W16) + Decay | Kombiniert AKGLS + LearningEngine | Unvollständig implementiert |
| **AdaptiveDifficultyService** | 20/40/40-Regel | Schwierigkeitsverteilung | Vygotsky's Zone of Proximal Development | Isoliert, nicht integriert |
| **PrerequisiteService** | Lernreihenfolge | Abhängigkeitsgraph | Strenge/Weiche Prerequisites | Keine Zykluserkennung |

### 2. Identifizierte Dateianalysesysteme

| System | Input | Output | KI-Modell |
|--------|-------|--------|-----------|
| **FileService** | IFormFile (PDF/DOCX/Image) | Document + Metadaten | Orchestrator |
| **DocumentParsingService** | Dateistream | Extrahierter Text | iText7 + Gemini OCR |
| **ChunkingService** | Text | Semantische Chunks | Claude Sonnet 4.5 |
| **EmbeddingService** | Text | 1536D Vektoren | OpenAI text-embedding-3-small |
| **LearningEngineService** | Chunks | KgEntity + KgRelationship | Claude Sonnet 4.5 |
| **IntentAnalysisService** | Dokument | Meetings/TODOs/Projekte/Fehler | Claude Sonnet 4.5 |
| **PdfImageExtractionService** | PDF | Bildanalyse + OCR | Gemini 2.0-Flash |

### 3. Kritische Redundanzen & Inkonsistenzen

```
┌─────────────────────────────────────────────────────────────────────────┐
│ REDUNDANZ 1: Duale Wissensrepräsentation                               │
│                                                                         │
│ UserKnowledgeNode (AKGLS)     vs.    KgEntity (Learning Engine)        │
│ - Subject/Topic/Subtopic             - 12 strukturierte Typen          │
│ - Einfacher Decay                    - Evidence-basierte Beziehungen   │
│ - Easy/Medium/Hard Stats             - Chunk-Traceability              │
│                                                                         │
│ → LÖSUNG: UnifiedKnowledgeEntity als kanonisches Modell                │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ REDUNDANZ 2: Mehrfache Spaced-Repetition-Implementierungen             │
│                                                                         │
│ GeneratedExercise: SM-2 (EaseFactor, ReviewCount)                      │
│ InteractiveExercise: SM-2 (EaseFactor, ReviewCount)                    │
│ UserEntityPerformance: FSRS vereinfacht (Stability, Difficulty, State) │
│ UnifiedKnowledgeEntity: FSRS vollständig (W0-W16) + Decay              │
│                                                                         │
│ → LÖSUNG: Ein FSRS-Algorithmus mit vollständigen Parametern            │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ REDUNDANZ 3: Fragmentierte Übungsquellen                               │
│                                                                         │
│ DeficitsTab → GeneratedExercise (Text-basiert)                         │
│ InteractiveTab → InteractiveExercise (Multi-Step)                      │
│ LearningEngineTab → LearningQuestion (Adaptive)                        │
│                                                                         │
│ → LÖSUNG: Unified Exercise Queue mit einheitlichem Scoring             │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ REDUNDANZ 4: Inkonsistente Beziehungstypen                             │
│                                                                         │
│ KnowledgeLink: 7 freie Typen (related, prerequisite, ...)              │
│ KgRelationship: 13 definierte Typen (is_a, part_of, requires, ...)     │
│ UnifiedKnowledgeRelationship: 13 Typen + Decay + Strictness            │
│                                                                         │
│ → LÖSUNG: Standardisierte Taxonomie mit 13 Kerntypen                   │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Teil II: Architektur des Omnifunktionalen Systems

### Designprinzipien

1. **Learning Engine als Kern** - Alle anderen Systeme werden in die Learning Engine integriert
2. **Single Source of Truth** - Ein kanonisches Datenmodell (UnifiedKnowledgeEntity)
3. **Plug-and-Play Komponenten** - Modulare Services mit klaren Schnittstellen
4. **Adaptive Intelligence** - FSRS + Decay + Bloom's Taxonomy durchgängig
5. **Seamless UX** - Einheitliche Lern-Queue über alle Übungstypen

### Systemarchitektur

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        OMNIFUNKTIONALES LERNSYSTEM                      │
│                     (Learning Engine als Kernarchitektur)               │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         │                          │                          │
         ▼                          ▼                          ▼
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│  INPUT LAYER    │      │  CORE LAYER     │      │  OUTPUT LAYER   │
│                 │      │                 │      │                 │
│ • FileService   │      │ OmniLearning-   │      │ • Unified       │
│ • MoodleSync    │      │   Engine        │      │   ExerciseQueue │
│ • NextcloudSync │      │                 │      │ • Knowledge     │
│ • EmailAnalysis │      │ (Fusion aller   │      │   Visualization │
│                 │      │  Learning-      │      │ • Progress      │
│                 │      │  Services)      │      │   Dashboard     │
└────────┬────────┘      └────────┬────────┘      └────────┬────────┘
         │                        │                        │
         └────────────────────────┼────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         UNIFIED DATA LAYER                              │
│                                                                         │
│  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐   │
│  │ UnifiedKnowledge- │  │ UnifiedKnowledge- │  │ UnifiedLearning-  │   │
│  │ Entity            │  │ Relationship      │  │ Priority          │   │
│  │                   │  │                   │  │                   │   │
│  │ • FSRS (W0-W16)   │  │ • 13 Typen        │  │ • Deadline        │   │
│  │ • Decay Model     │  │ • Decay-Stärke    │  │ • Mastery Gap     │   │
│  │ • Bloom (1-6)     │  │ • Prerequisites   │  │ • Topic Relevance │   │
│  │ • 20/40/40 Stats  │  │ • Evidence        │  │ • Decay Amount    │   │
│  └───────────────────┘  └───────────────────┘  └───────────────────┘   │
│                                                                         │
│  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐   │
│  │ UnifiedExercise   │  │ DocumentChunk     │  │ QdrantEmbedding   │   │
│  │                   │  │                   │  │                   │   │
│  │ • Text/Interactive│  │ • Semantic Split  │  │ • 1536D Vectors   │   │
│  │ • Multi-Step      │  │ • Topic Labels    │  │ • Cross-Entity    │   │
│  │ • Unified Scoring │  │ • Entity Links    │  │ • Semantic Search │   │
│  └───────────────────┘  └───────────────────┘  └───────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Teil III: Komponenten des OmniLearningEngine

### 3.1 Unified Entity Model

```csharp
// NEUES MODELL: Fusioniert UserKnowledgeNode + KgEntity + UnifiedKnowledgeEntity
public class OmniKnowledgeEntity
{
    // === IDENTIFIKATION ===
    public int Id { get; set; }
    public int UserId { get; set; }

    // === KLASSIFIKATION (von KgEntity) ===
    public string EntityType { get; set; }  // 12 Typen: concept, definition, formula, ...
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public string? Description { get; set; }

    // === TAXONOMIE (von UserKnowledgeNode) ===
    public string Subject { get; set; }      // Pflicht
    public string Topic { get; set; }        // Pflicht
    public string? Subtopic { get; set; }    // Optional

    // === QUELLENTRACKING (von KgEntity) ===
    public int? SourceDocumentId { get; set; }
    public int? SourceChunkId { get; set; }
    public string? Evidence { get; set; }

    // === KONFIDENZ (von KgEntity) ===
    public double ConfidenceScore { get; set; }  // 0.0-1.0 (KI-Extraktion)
    public double ImportanceScore { get; set; }  // 0.0-1.0 (Connectivity-based)
    public int OccurrenceCount { get; set; }
    public bool IsVerified { get; set; }

    // === FSRS VOLLSTÄNDIG (von UnifiedKnowledgeEntity) ===
    public double Stability { get; set; } = 1.0;
    public double Difficulty { get; set; } = 0.3;
    public int ElapsedDays { get; set; }
    public int ScheduledDays { get; set; }
    public int Reps { get; set; }
    public int Lapses { get; set; }
    public FsrsState FsrsState { get; set; } = FsrsState.New;
    public DateTime? NextReview { get; set; }

    // === DECAY MODEL (von UserKnowledgeNode) ===
    public double BaseStrength { get; set; } = 1.0;
    public double DecayRate { get; set; } = 0.05;  // 5% pro Tag
    public DateTime LastInteraction { get; set; }

    // Berechnete Eigenschaft
    public double EffectiveStrength =>
        BaseStrength * Math.Exp(-DecayRate * (DateTime.UtcNow - LastInteraction).TotalDays);

    // === BLOOM'S TAXONOMY ===
    public int CurrentBloomLevel { get; set; } = 1;  // 1-6
    public string BloomPerformanceJson { get; set; } = "{}";  // {"1": {attempts, correct}, ...}

    // === 20/40/40 SCHWIERIGKEITSVERTEILUNG ===
    public int EasyCorrect { get; set; }
    public int EasyTotal { get; set; }
    public int MediumCorrect { get; set; }
    public int MediumTotal { get; set; }
    public int HardCorrect { get; set; }
    public int HardTotal { get; set; }

    // === AGGREGIERTE STATISTIKEN ===
    public int TotalAttempts { get; set; }
    public int TotalCorrect { get; set; }
    public double AverageResponseTimeSeconds { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    // === EMBEDDING ===
    public bool HasEmbedding { get; set; }
    public string? QdrantPointId { get; set; }

    // === BEZIEHUNGEN ===
    public ICollection<OmniKnowledgeRelationship> OutgoingRelationships { get; set; }
    public ICollection<OmniKnowledgeRelationship> IncomingRelationships { get; set; }

    // === BERECHNETE MASTERY ===
    public double MasteryScore => CalculateMastery();

    private double CalculateMastery()
    {
        double successRate = TotalAttempts > 0 ? (double)TotalCorrect / TotalAttempts : 0;
        double fsrsMastery = (successRate * 0.6) + ((1 - Difficulty) * 0.2) + (Math.Min(Stability / 30, 0.2));
        double decayFactor = EffectiveStrength / BaseStrength;
        return fsrsMastery * (0.4 + 0.6 * decayFactor) * BaseStrength;
    }
}
```

### 3.2 Unified Relationship Model

```csharp
public class OmniKnowledgeRelationship
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // === VERBINDUNG ===
    public int SourceEntityId { get; set; }
    public int TargetEntityId { get; set; }
    public OmniKnowledgeEntity SourceEntity { get; set; }
    public OmniKnowledgeEntity TargetEntity { get; set; }

    // === TYPISIERUNG (13 standardisierte Typen) ===
    public RelationshipType Type { get; set; }
    // is_a, part_of, relates_to, requires, contradicts, example_of,
    // defines, uses, precedes, derives_from, extends, implements, similar_to

    // === QUELLENTRACKING ===
    public int? ExtractedFromChunkId { get; set; }
    public int? ExtractedFromDocumentId { get; set; }
    public string? Evidence { get; set; }
    public string? Description { get; set; }

    // === STÄRKE MIT DECAY ===
    public double InitialStrength { get; set; } = 1.0;
    public double DecayRate { get; set; } = 0.03;  // Langsamer als Entitäten
    public DateTime LastReinforced { get; set; }
    public int ReinforcementCount { get; set; }
    public int WeakeningCount { get; set; }

    public double CurrentStrength =>
        InitialStrength * Math.Exp(-DecayRate * (DateTime.UtcNow - LastReinforced).TotalDays);

    // === PREREQUISITE-LOGIK ===
    public double RequiredMasteryLevel { get; set; } = 0.6;
    public bool IsStrict { get; set; }  // Muss erfüllt sein vor Fortschritt

    // === METADATEN ===
    public double ConfidenceScore { get; set; } = 1.0;
    public bool IsAutoExtracted { get; set; }
    public bool IsVerified { get; set; }
    public bool IsBidirectional { get; set; }
    public bool IsActive { get; set; } = true;
}
```

### 3.3 Unified Exercise Model

```csharp
public class OmniExercise
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // === KLASSIFIKATION ===
    public ExerciseFormat Format { get; set; }  // Text, Interactive, Adaptive
    public string ExerciseType { get; set; }    // mc, fill_blank, drag_drop, code, ...
    public string Difficulty { get; set; }       // easy, medium, hard

    // === INHALT ===
    public string Subject { get; set; }
    public string Topic { get; set; }
    public string Question { get; set; }
    public string? ExerciseContentJson { get; set; }  // Für Interactive (Steps)
    public string? OptionsJson { get; set; }           // Für MC
    public string CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? Hint { get; set; }

    // === BLOOM'S TAXONOMY ===
    public int BloomLevel { get; set; } = 1;  // 1-6

    // === QUELLEN-LINK ===
    public int? EntityId { get; set; }
    public OmniKnowledgeEntity? Entity { get; set; }
    public int? ChunkId { get; set; }
    public int? DocumentId { get; set; }
    public int? DeficitId { get; set; }

    // === ANTWORT-TRACKING ===
    public string? UserAnswer { get; set; }
    public string? StepProgressJson { get; set; }  // Für Interactive
    public bool? IsCorrect { get; set; }
    public double? Score { get; set; }  // 0-100
    public DateTime? AnsweredAt { get; set; }
    public int AttemptCount { get; set; }
    public double? ResponseTimeSeconds { get; set; }

    // === SPACED REPETITION (FSRS) ===
    public DateTime? NextReviewDate { get; set; }
    public int ReviewCount { get; set; }
    public double EaseFactor { get; set; } = 2.5;
    public double Stability { get; set; } = 1.0;
    public double Difficulty { get; set; } = 0.3;

    // === EXAM MODE ===
    public ExerciseMode Mode { get; set; }  // learning, exam_prep, exam_simulation
    public int? TimeLimitSeconds { get; set; }
    public int? TimeSpentSeconds { get; set; }
}
```

### 3.4 Unified Learning Priority

```csharp
public class OmniLearningPriority
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // === VERKNÜPFUNGEN ===
    public int? EntityId { get; set; }
    public int? ExerciseId { get; set; }
    public int? MoodleAssignmentId { get; set; }
    public int? CalendarEventId { get; set; }

    // === PRIORITÄTSKOMPONENTEN ===
    public double DeadlineUrgency { get; set; }     // 0-100 (Zeit bis Deadline)
    public double TopicRelevance { get; set; }      // 0-100 (Semantische Ähnlichkeit)
    public double MasteryGap { get; set; }          // 0-100 (1 - MasteryScore)
    public double DecayAmount { get; set; }         // 0-100 (1 - EffectiveStrength)
    public double BloomGap { get; set; }            // 0-100 (Target - Current Level)

    // === GEWICHTETER SCORE ===
    // Formel: 0.30×Urgency + 0.20×Relevance + 0.25×MasteryGap + 0.15×Decay + 0.10×BloomGap
    public double CompositeScore { get; set; }

    // === BLOCKING ===
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public string? BlockingPrerequisitesJson { get; set; }

    // === METADATEN ===
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? EntityName { get; set; }
    public DateTime? Deadline { get; set; }
    public int Rank { get; set; }
    public DateTime CalculatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
```

---

## Teil IV: OmniLearningEngine Service

### 4.1 Service-Architektur

```
OmniLearningEngine/
├── OmniLearningEngineService.cs                    # Orchestrator
├── OmniLearningEngineService.DocumentPipeline.cs   # Dokument → Chunks → Entities
├── OmniLearningEngineService.EntityExtraction.cs   # KI-basierte Extraktion
├── OmniLearningEngineService.RelationshipGraph.cs  # Beziehungs-Management
├── OmniLearningEngineService.ExerciseGeneration.cs # Einheitliche Übungsgenerierung
├── OmniLearningEngineService.AdaptiveScheduling.cs # FSRS + Decay + Bloom
├── OmniLearningEngineService.PriorityQueue.cs      # Einheitliche Lern-Queue
├── OmniLearningEngineService.Visualization.cs      # Graph & Cluster
└── OmniLearningEngineService.Analytics.cs          # Fortschrittsanalyse
```

### 4.2 Kernfunktionen

```csharp
public interface IOmniLearningEngineService
{
    // === DOCUMENT PIPELINE ===
    Task<DocumentProcessingResult> ProcessDocumentAsync(int documentId, ProcessingOptions options);
    Task<BatchProcessingResult> ProcessDocumentsBatchAsync(int[] documentIds, ProcessingOptions options);

    // === ENTITY MANAGEMENT ===
    Task<OmniKnowledgeEntity> CreateEntityAsync(EntityCreateDto dto);
    Task<OmniKnowledgeEntity> MergeEntitiesAsync(int[] entityIds);
    Task<List<OmniKnowledgeEntity>> SearchEntitiesAsync(string query, EntityFilters filters);
    Task<List<OmniKnowledgeEntity>> GetRelatedEntitiesAsync(int entityId, int depth = 2);

    // === RELATIONSHIP MANAGEMENT ===
    Task<OmniKnowledgeRelationship> CreateRelationshipAsync(RelationshipCreateDto dto);
    Task<List<OmniKnowledgeRelationship>> GenerateRelationshipsAsync(int entityId);
    Task<bool> CheckPrerequisitesAsync(int userId, int targetEntityId);
    Task<List<PrerequisiteChain>> GetPrerequisiteChainAsync(int entityId);

    // === UNIFIED EXERCISE GENERATION ===
    Task<OmniExercise> GenerateExerciseAsync(ExerciseRequest request);
    Task<List<OmniExercise>> GenerateSessionAsync(SessionRequest request);
    Task<ExerciseResult> SubmitAnswerAsync(int exerciseId, AnswerSubmission submission);

    // === ADAPTIVE SCHEDULING ===
    Task<List<OmniLearningPriority>> CalculatePrioritiesAsync(int userId);
    Task<OmniExercise> GetNextExerciseAsync(int userId);
    Task<List<WeakArea>> GetWeakAreasAsync(int userId);
    Task<List<OverdueItem>> GetOverdueItemsAsync(int userId);

    // === VISUALIZATION ===
    Task<KnowledgeGraph> GetKnowledgeGraphAsync(int userId, GraphFilters filters);
    Task<ClusterVisualization> GetClusterVisualizationAsync(int userId);
    Task<NetworkGraph> GetNetworkGraphAsync(int userId);

    // === ANALYTICS ===
    Task<MasteryStats> GetMasteryStatsAsync(int userId);
    Task<LearningStreak> GetStreakAsync(int userId);
    Task<DifficultyDistribution> GetDistributionAsync(int userId);
    Task<BloomProgression> GetBloomProgressionAsync(int userId, string subject);
}
```

### 4.3 Algorithmus: Adaptive Priority Scoring

```
ALGORITHMUS: OmniPriorityCalculation

INPUT: userId, entities[], deadlines[], currentTime
OUTPUT: prioritizedQueue[]

FOR EACH entity IN entities:
    // 1. Deadline-Urgency (30% Gewicht)
    IF entity.HasDeadline:
        daysUntil = (entity.Deadline - currentTime).Days
        urgency = MAX(0, 100 × (1 - daysUntil / 30))
    ELSE:
        urgency = 0

    // 2. Topic-Relevance (20% Gewicht)
    relevance = SemanticSimilarity(entity, user.CurrentFocus) × 100

    // 3. Mastery-Gap (25% Gewicht)
    masteryGap = (1 - entity.MasteryScore) × 100

    // 4. Decay-Amount (15% Gewicht)
    decayAmount = (1 - entity.EffectiveStrength) × 100

    // 5. Bloom-Gap (10% Gewicht)
    bloomGap = MAX(0, (entity.TargetBloomLevel - entity.CurrentBloomLevel)) × 20

    // Composite Score
    score = 0.30 × urgency + 0.20 × relevance + 0.25 × masteryGap
          + 0.15 × decayAmount + 0.10 × bloomGap

    // Blocking Check
    IF entity.HasStrictPrerequisites:
        unmetPrereqs = GetUnmetPrerequisites(entity)
        IF unmetPrereqs.Count > 0:
            entity.IsBlocked = TRUE
            entity.BlockReason = "Prerequisites not met"
            score = score × 0.1  // Stark reduziert, aber sichtbar

    prioritizedQueue.Add({entity, score})

SORT prioritizedQueue BY score DESC
RETURN prioritizedQueue
```

### 4.4 Algorithmus: FSRS + Decay Hybrid

```
ALGORITHMUS: HybridSpacedRepetition

CONSTANTS:
    W[0..16] = FSRS_WEIGHTS  // Vollständige FSRS-Parameter
    DECAY_BASE = 0.05        // 5% pro Tag Grundverfall
    MIN_STABILITY = 0.1
    MAX_STABILITY = 365

FUNCTION UpdateAfterReview(entity, isCorrect, quality):
    // 1. FSRS-Update (Stability & Difficulty)
    IF isCorrect:
        entity.Reps++
        stabilityIncrease = CalculateStabilityIncrease(entity, quality)
        entity.Stability = MIN(entity.Stability × stabilityIncrease, MAX_STABILITY)
        entity.Difficulty = MAX(0, entity.Difficulty - 0.05)

        // State Transition
        IF entity.FsrsState == Learning AND entity.Reps >= 3:
            entity.FsrsState = Review
    ELSE:
        entity.Lapses++
        entity.Stability = MAX(entity.Stability × 0.5, MIN_STABILITY)
        entity.Difficulty = MIN(1, entity.Difficulty + 0.1)

        IF entity.FsrsState == Review:
            entity.FsrsState = Relearning

    // 2. Decay-Update (BaseStrength)
    entity.LastInteraction = NOW()
    IF isCorrect:
        entity.BaseStrength = MIN(1.0, entity.BaseStrength + 0.05)
    ELSE:
        entity.BaseStrength = MAX(0.1, entity.BaseStrength - 0.02)

    // 3. Next Review Calculation
    interval = CalculateInterval(entity)
    entity.NextReview = NOW() + interval
    entity.ScheduledDays = interval.Days

    RETURN entity

FUNCTION CalculateInterval(entity):
    SWITCH entity.FsrsState:
        CASE New: RETURN 1 day
        CASE Learning: RETURN MIN(entity.Reps, 3) days
        CASE Review: RETURN entity.Stability × (1 + 0.5 × (1 - entity.Difficulty)) days
        CASE Relearning: RETURN 1 day
```

---

## Teil V: Implementierungsplan

### Entscheidungen (User-Präferenzen)

| Aspekt | Entscheidung |
|--------|-------------|
| **Migrationsstrategie** | Vollständiger Ersatz (Breaking Changes akzeptiert) |
| **Übungstypen** | Alle 6 Komponenten (MC, Fill-Blank, Drag-Drop, Slider, Code-Editor, Text-Input) |
| **Sprache** | Deutsch als Primärsprache (DHBW-optimiert) |

---

### Phase 1: Datenmodell-Migration

**Dateien zu erstellen/modifizieren:**

```
Backend/Core/Models/
├── OmniKnowledgeEntity.cs         [NEU]
├── OmniKnowledgeRelationship.cs   [NEU]
├── OmniExercise.cs                [NEU]
├── OmniLearningPriority.cs        [NEU]
└── Enums/
    ├── RelationshipType.cs        [ERWEITERN]
    ├── ExerciseFormat.cs          [NEU]
    └── FsrsState.cs               [BESTEHEND]

Backend/Infrastructure/Database/
├── AppDbContext.cs                [MODIFIZIEREN: Alte DbSets entfernen, 4 neue hinzufügen]
└── Migrations/
    └── YYYYMMDD_OmniLearningReplacement.cs [NEU]
```

**Migration-Strategie (Vollständiger Ersatz):**
1. Neue Tabellen erstellen mit Migrationsskript
2. Daten aus alten Tabellen transformieren und übertragen
3. Alte Tabellen und Services in einem Schritt entfernen
4. Keine Rückwärtskompatibilität - saubere Architektur

### Phase 2: Service-Fusion

**Dateien zu erstellen:**

```
Backend/Core/Services/OmniLearning/
├── OmniLearningEngineService.cs
├── OmniLearningEngineService.DocumentPipeline.cs
├── OmniLearningEngineService.EntityExtraction.cs
├── OmniLearningEngineService.RelationshipGraph.cs
├── OmniLearningEngineService.ExerciseGeneration.cs   # Alle 6 Komponententypen
├── OmniLearningEngineService.AdaptiveScheduling.cs
├── OmniLearningEngineService.PriorityQueue.cs
├── OmniLearningEngineService.Visualization.cs
└── OmniLearningEngineService.Analytics.cs

Backend/Core/Interfaces/
└── IOmniLearningEngineService.cs
```

**Dateien zu ENTFERNEN (vollständiger Ersatz):**

```
Backend/Core/Services/
├── LearningEngine/                    [ENTFERNEN - in OmniLearning integriert]
├── InteractiveExercise/               [ENTFERNEN - in OmniLearning integriert]
├── KnowledgeNetwork/                  [ENTFERNEN - in OmniLearning integriert]
├── UnifiedLearning/                   [ENTFERNEN - ersetzt durch OmniLearning]
├── AdaptiveDifficultyService.cs       [ENTFERNEN - in AdaptiveScheduling integriert]
├── PrerequisiteService.cs             [ENTFERNEN - in RelationshipGraph integriert]
└── RagExerciseService.cs              [ENTFERNEN - in ExerciseGeneration integriert]
```

**Refactoring-Strategie (Vollständiger Ersatz):**
1. Kernlogik aus allen Services extrahieren und in OmniLearning konsolidieren
2. Deutsche Prompts für alle KI-Interaktionen
3. Alle 6 Übungskomponenten in ExerciseGeneration integrieren
4. Alte Services vollständig entfernen
5. Controller direkt auf OmniLearningEngineService umstellen

### Phase 3: API-Konsolidierung

**Neuer Controller:**

```
Backend/API/Controllers/
└── OmniLearningController.cs

Endpoints:
POST   /api/omni/dokumente/verarbeiten           # Dokument-Pipeline
GET    /api/omni/entitaeten                      # Alle Entitäten
POST   /api/omni/entitaeten/suche                # Semantische Suche
GET    /api/omni/entitaeten/{id}/graph           # Entitäts-Graph
GET    /api/omni/entitaeten/{id}/verwandt        # Verwandte Entitäten
POST   /api/omni/uebungen/generieren             # Übung erstellen
POST   /api/omni/uebungen/{id}/antwort           # Antwort einreichen
GET    /api/omni/warteschlange                   # Unified Learning Queue
GET    /api/omni/warteschlange/naechste          # Nächste Übung
GET    /api/omni/statistiken                     # Mastery-Stats
GET    /api/omni/schwachstellen                  # Weak Areas
GET    /api/omni/visualisierung/graph            # Knowledge Graph
GET    /api/omni/visualisierung/cluster          # 2D Cluster View
```

**Controller zu ENTFERNEN:**

```
Backend/API/Controllers/
├── LearningEngineController.cs          [ENTFERNEN]
├── InteractiveExerciseController.cs     [ENTFERNEN]
├── KnowledgeNetworkController.cs        [ENTFERNEN]
├── AdaptiveLearningController.cs        [ENTFERNEN]
└── PersonalKnowledgeGraphController.cs  [ENTFERNEN]
```

### Phase 4: Frontend-Integration

**Neue Dateien:**

```
Frontend/src/
├── views/
│   └── OmniLernenView.vue               [NEU: Unified Learning Hub - Deutsch]
├── components/
│   └── omniLernen/
│       ├── LernWarteschlange.vue        [NEU: Unified Queue]
│       ├── UebungsSpieler.vue           [NEU: Alle 6 Komponententypen]
│       │   ├── MultipleChoice.vue       [Integriert]
│       │   ├── LueckenText.vue          [Integriert]
│       │   ├── DragDrop.vue             [Integriert]
│       │   ├── Schieberegler.vue        [Integriert]
│       │   ├── CodeEditor.vue           [Integriert]
│       │   └── TextEingabe.vue          [Integriert]
│       ├── WissensGraph.vue             [NEU: Erweitert]
│       ├── MeisterschaftsDashboard.vue  [NEU]
│       ├── FortschrittsAnzeige.vue      [NEU]
│       └── SchwachstellenListe.vue      [NEU]
├── composables/
│   └── useOmniLernen.ts                 [NEU]
├── services/
│   └── api.ts                           [MODIFIZIEREN: Alte Endpoints entfernen]
└── types/
    └── omniLernen.ts                    [NEU]
```

**Dateien zu ENTFERNEN:**

```
Frontend/src/
├── views/
│   ├── LearningView.vue                 [ENTFERNEN]
│   └── KnowledgeNetworkView.vue         [ENTFERNEN]
├── components/
│   ├── learning/                        [ENTFERNEN - Ordner komplett]
│   ├── learningEngine/                  [ENTFERNEN - Ordner komplett]
│   ├── exercises/                       [ENTFERNEN - Ordner komplett]
│   └── network/                         [ENTFERNEN - Ordner komplett]
├── composables/
│   ├── useLearning.ts                   [ENTFERNEN]
│   ├── useLearningEngine.ts             [ENTFERNEN]
│   ├── useInteractiveExercises.ts       [ENTFERNEN]
│   ├── useNetworkGraph.ts               [ENTFERNEN]
│   ├── useNetworkSearch.ts              [ENTFERNEN]
│   ├── useNetworkTags.ts                [ENTFERNEN]
│   └── useNetworkLinks.ts               [ENTFERNEN]
└── types/
    ├── learning.ts                      [ENTFERNEN]
    ├── learningEngine.ts                [ENTFERNEN]
    └── knowledgeNetwork.ts              [ENTFERNEN]
```

### Phase 5: Cleanup & Daten-Migration

**Daten-Migrationsskript erstellen:**

```
Backend/Infrastructure/Database/Scripts/
└── MigrateToOmniLearning.cs

Migriert:
1. KgEntity + UserKnowledgeNode → OmniKnowledgeEntity
2. KgRelationship + UserKnowledgeEdge → OmniKnowledgeRelationship
3. GeneratedExercise + InteractiveExercise → OmniExercise
4. LearningPriority + UnifiedLearningPriority → OmniLearningPriority
5. UserEntityPerformance-Daten in OmniKnowledgeEntity integrieren
```

**Vollständige Cleanup-Checkliste:**

1. **Datenbank:**
   - [ ] Migrationsskript ausführen
   - [ ] Alte Tabellen löschen (nach Backup)
   - [ ] Indices für neue Tabellen optimieren

2. **Backend Services entfernen:**
   - [ ] LearningEngine/ Ordner löschen
   - [ ] InteractiveExercise/ Ordner löschen
   - [ ] KnowledgeNetwork/ Ordner löschen
   - [ ] UnifiedLearning/ Ordner löschen
   - [ ] Einzelne Service-Dateien löschen

3. **Backend Controller entfernen:**
   - [ ] 5 alte Controller löschen
   - [ ] Program.cs DI-Registrierungen aktualisieren

4. **Frontend entfernen:**
   - [ ] 4 View-Dateien löschen
   - [ ] 4 Component-Ordner löschen
   - [ ] 7 Composables löschen
   - [ ] 3 Type-Dateien löschen
   - [ ] Router aktualisieren

5. **Dokumentation:**
   - [ ] CLAUDE.md aktualisieren
   - [ ] API-Dokumentation (Swagger) aktualisieren

---

## Teil VI: Kritische Dateipfade

### Backend (zu modifizieren)

```
dhbw-automation/src/Backend/
├── Core/
│   ├── Models/
│   │   ├── KgEntity.cs                           [REFERENCE]
│   │   ├── KgRelationship.cs                     [REFERENCE]
│   │   ├── UserKnowledgeNode.cs                  [REFERENCE]
│   │   ├── UnifiedKnowledgeEntity.cs             [REFERENCE]
│   │   ├── InteractiveExercise.cs                [REFERENCE]
│   │   └── GeneratedExercise.cs                  [REFERENCE]
│   ├── Services/
│   │   ├── LearningEngine/                       [HAUPTREFERENZ]
│   │   │   ├── LearningEngineService.cs
│   │   │   ├── LearningEngineService.EntityExtraction.cs
│   │   │   ├── LearningEngineService.Questions.cs
│   │   │   └── LearningEngineService.KnowledgeGraph.cs
│   │   ├── InteractiveExercise/                  [INTEGRATION]
│   │   ├── KnowledgeNetwork/                     [INTEGRATION]
│   │   ├── UnifiedLearning/                      [BASIS]
│   │   └── OmniLearning/                         [NEU]
│   └── Interfaces/
│       └── IOmniLearningEngineService.cs         [NEU]
├── Infrastructure/
│   ├── Database/
│   │   ├── AppDbContext.cs                       [ERWEITERN]
│   │   └── Migrations/                           [NEU]
│   └── ExternalAPIs/
│       └── AnthropicClient.cs                    [REFERENCE]
└── API/Controllers/
    └── OmniLearningController.cs                 [NEU]
```

### Frontend (zu modifizieren)

```
dhbw-automation/src/Frontend/src/
├── views/
│   ├── LearningView.vue                          [REFERENCE]
│   ├── KnowledgeNetworkView.vue                  [REFERENCE]
│   └── OmniLearningView.vue                      [NEU]
├── components/
│   ├── learningEngine/                           [REFERENCE]
│   ├── exercises/                                [REFERENCE]
│   └── omniLearning/                             [NEU]
├── composables/
│   ├── useLearningEngine.ts                      [REFERENCE]
│   └── useOmniLearning.ts                        [NEU]
├── services/
│   └── api.ts                                    [ERWEITERN]
└── types/
    ├── learningEngine.ts                         [REFERENCE]
    └── omniLearning.ts                           [NEU]
```

---

## Teil VII: Verifikation & Testing

### Unit Tests

```bash
# Backend Tests
cd dhbw-automation/src/Backend
dotnet test --filter "Category=OmniLearning"
```

Test-Szenarien:
1. FSRS-Algorithmus-Korrektheit
2. Decay-Berechnung
3. Priority-Scoring
4. Entity-Merge-Logik
5. Prerequisite-Checking

### Integration Tests

```bash
# API-Integrationstests
dotnet test --filter "Category=Integration"
```

Test-Szenarien:
1. Dokument-Upload → Entity-Extraktion → Graph
2. Exercise-Generation → Submission → Score-Update
3. Priority-Queue-Berechnung mit Deadlines
4. Multi-User-Isolation

### E2E Tests (Frontend)

```bash
cd dhbw-automation/src/Frontend
npm run test:e2e
```

Test-Szenarien:
1. User Journey: Upload → Learn → Progress
2. Unified Queue Navigation
3. Graph Visualization Interaction
4. Exercise Completion Flow

### Manuelle Verifikation

1. **Dokument hochladen** → Prüfen ob Entities extrahiert werden
2. **Learning Queue öffnen** → Prüfen ob Prioritäten korrekt sortiert
3. **Übung absolvieren** → Prüfen ob Mastery-Score aktualisiert
4. **Knowledge Graph anzeigen** → Prüfen ob Beziehungen visualisiert
5. **Nach 24h wiederkehren** → Prüfen ob Decay korrekt angewendet

---

## Teil VIII: Erwartete Vorteile

| Aspekt | Vorher | Nachher |
|--------|--------|---------|
| **Datenmodelle** | 6+ separate Entities | 4 unified Entities |
| **Spaced Rep** | 3 verschiedene Algorithmen | 1 FSRS+Decay Hybrid |
| **Exercise Queue** | 3 separate Quellen | 1 unified Queue |
| **API Endpoints** | ~30 fragmentiert | ~15 konsolidiert |
| **Bloom Tracking** | Nur in LearningEngine | Durchgängig |
| **Decay Model** | Nur in AKGLS | Durchgängig |
| **Prerequisites** | Isoliert | Graph-integriert |
| **Frontend Views** | 3 separate | 1 unified Hub |

---

## Zusammenfassung

Das vorgeschlagene **OmniLernen-System** vereint die Stärken aller existierenden Subsysteme in einer sauberen, deutschen Architektur:

### Kernmerkmale

| Merkmal | Beschreibung |
|---------|--------------|
| **Learning Engine Basis** | Claude-basierte Entitätsextraktion, 12 Typen, Evidence-Tracking |
| **FSRS + Decay Hybrid** | Wissenschaftlich fundiertes Spaced Repetition mit Zeitverfall |
| **Bloom's Taxonomy** | Adaptive Schwierigkeitsprogression (Erinnern → Erschaffen) |
| **20/40/40 Regel** | Optimale Schwierigkeitsverteilung nach Vygotsky |
| **Alle 6 Komponenten** | MC, Lückentext, Drag-Drop, Schieberegler, Code-Editor, Texteingabe |
| **Unified Queue** | Eine Lern-Warteschlange für alle Übungstypen |
| **Knowledge Graph** | Visualisierung mit Prerequisite-Logik |
| **Deutsche UI/UX** | Primär auf DHBW-Studierende optimiert |

### Migrationsstrategie: Vollständiger Ersatz

```
VORHER (6+ fragmentierte Systeme):
├── LearningEngineService (5 Dateien)
├── InteractiveExerciseService (7 Dateien)
├── KnowledgeNetworkService (7 Dateien)
├── UnifiedLearningService (7 Dateien)
├── AdaptiveDifficultyService
├── PrerequisiteService
└── RagExerciseService

NACHHER (1 konsolidiertes System):
└── OmniLearningEngineService (9 Dateien)
    ├── DocumentPipeline
    ├── EntityExtraction
    ├── RelationshipGraph
    ├── ExerciseGeneration (alle 6 Typen)
    ├── AdaptiveScheduling (FSRS + Decay + Bloom)
    ├── PriorityQueue
    ├── Visualization
    └── Analytics
```

### Erwartete Vorteile

| Metrik | Vorher | Nachher | Verbesserung |
|--------|--------|---------|--------------|
| Backend Services | 30+ Dateien | 10 Dateien | -67% |
| API Endpoints | ~30 | ~13 | -57% |
| Frontend Views | 3 | 1 | -67% |
| Datenmodelle | 10+ | 4 | -60% |
| Spaced Rep Algorithmen | 3 | 1 | Konsistent |
| Übungsquellen | 3 | 1 Queue | Unified |

### Nächste Schritte

1. Plan genehmigen
2. Phase 1 starten: Datenmodelle erstellen
3. Migrationsskript für bestehende Daten
4. OmniLearningEngineService implementieren
5. Frontend neu aufbauen
6. Alte Systeme entfernen
7. Deployment auf Server

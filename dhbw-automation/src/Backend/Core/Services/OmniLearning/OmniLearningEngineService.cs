using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Core.Services.Embedding;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services.OmniLearning;

/// <summary>
/// OmniLearningEngineService - Omnifunktionales Lernsystem
///
/// Konsolidiert alle Lernfunktionen in einem einzigen Service:
/// - LearningEngineService (Entity-Extraktion, Knowledge Graph)
/// - UnifiedLearningService (FSRS + Decay, Bloom's Taxonomy)
/// - InteractiveExerciseService (6 Übungskomponenten)
/// - KnowledgeNetworkService (Visualisierung)
/// - AdaptiveDifficultyService (20/40/40-Regel)
/// - PrerequisiteService (Lernreihenfolge)
/// - DeadlinePriorityService (Prioritätsberechnung)
///
/// Kernalgorithmen:
/// - FSRS (Free Spaced Repetition Scheduler) mit vollständigen W0-W16 Parametern
/// - Exponentieller Decay für Wissensverfall
/// - Bloom's Taxonomy (6 Stufen) für Lernprogression
/// - 20/40/40-Regel für optimale Schwierigkeitsverteilung
/// - Composite Priority Scoring (Deadline + Relevanz + Mastery + Decay + Bloom)
/// </summary>
public partial class OmniLearningEngineService : IOmniLearningEngineService
{
    private readonly AppDbContext _context;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly AnthropicClient _anthropicClient;
    private readonly EncryptionHelper _encryptionHelper;
    private readonly ILogger<OmniLearningEngineService> _logger;

    // Qdrant Collections
    internal const string OmniEntitiesCollection = "dhbw_omni_entities";
    internal const string OmniExercisesCollection = "dhbw_omni_exercises";
    internal const int VectorDimension = 1536;

    #region FSRS Parameters (Free Spaced Repetition Scheduler)

    // Vollständige FSRS-4.5 Parameter
    private const double FsrsRequestRetention = 0.9;
    private const double FsrsMaximumInterval = 365.0;
    private const double FsrsMinimumInterval = 1.0;

    // W0-W16 Gewichte für FSRS-Berechnung
    private static readonly double[] FsrsWeights = new double[]
    {
        0.4,    // W0: Initial stability for Again rating
        0.6,    // W1: Initial stability for Hard rating
        2.4,    // W2: Initial stability for Good rating
        5.8,    // W3: Initial stability for Easy rating
        4.93,   // W4: Stability factor
        0.94,   // W5: Difficulty factor (Again)
        0.86,   // W6: Difficulty factor (Hard)
        0.01,   // W7: Difficulty factor (Good)
        1.49,   // W8: Difficulty factor (Easy)
        0.14,   // W9: Short-term stability decay
        0.94,   // W10: Forgetting curve decay
        2.18,   // W11: Hard penalty
        0.05,   // W12: Easy bonus
        0.34,   // W13: Relearning stability
        1.26,   // W14: Stability increase rate
        0.29,   // W15: Stability decrease rate
        2.61    // W16: Stability cap factor
    };

    #endregion

    #region Decay Parameters

    private const double DefaultEntityDecayRate = 0.05;      // 5% pro Tag
    private const double DefaultRelationshipDecayRate = 0.03; // 3% pro Tag
    private const double MinimumRetention = 0.4;              // Minimum 40% Wissen bleibt erhalten

    #endregion

    #region Bloom's Taxonomy Levels

    internal static readonly Dictionary<int, string> BloomLevels = new()
    {
        { 1, "Erinnern" },      // Remember
        { 2, "Verstehen" },     // Understand
        { 3, "Anwenden" },      // Apply
        { 4, "Analysieren" },   // Analyze
        { 5, "Bewerten" },      // Evaluate
        { 6, "Erschaffen" }     // Create
    };

    #endregion

    #region Constructor

    public OmniLearningEngineService(
        AppDbContext context,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        AnthropicClient anthropicClient,
        EncryptionHelper encryptionHelper,
        ILogger<OmniLearningEngineService> logger)
    {
        _context = context;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _anthropicClient = anthropicClient;
        _encryptionHelper = encryptionHelper;
        _logger = logger;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialisiert den OmniLearning Service
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Erstelle Qdrant Collections falls nicht vorhanden
            await _qdrantService.EnsureCollectionExistsAsync(OmniEntitiesCollection, VectorDimension);
            await _qdrantService.EnsureCollectionExistsAsync(OmniExercisesCollection, VectorDimension);

            _logger.LogInformation("OmniLearningEngineService erfolgreich initialisiert");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Initialisierung des OmniLearningEngineService");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Holt Anthropic API Key für User (oder System-Key als Fallback)
    /// </summary>
    internal async Task<string?> GetAnthropicApiKeyAsync(int? userId)
    {
        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && !string.IsNullOrEmpty(user.AnthropicApiKey))
            {
                try
                {
                    return _encryptionHelper.Decrypt(user.AnthropicApiKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Entschlüsseln des Anthropic API Keys für User {UserId}", userId);
                }
            }
        }
        return null; // AnthropicClient verwendet dann Environment Variable
    }

    /// <summary>
    /// Normalisiert Entity-Namen für Matching (lowercase, Umlaute ersetzen)
    /// </summary>
    internal static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name.ToLowerInvariant()
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue")
            .Replace("ß", "ss")
            .Replace("-", " ")
            .Replace("_", " ")
            .Trim();
    }

    /// <summary>
    /// Konvertiert UnifiedKnowledgeEntity zu DTO
    /// </summary>
    internal UnifiedEntityDto MapToDto(UnifiedKnowledgeEntity entity)
    {
        return new UnifiedEntityDto
        {
            Id = entity.Id,
            EntityType = entity.EntityType,
            Name = entity.Name,
            Description = entity.Description,
            Subject = entity.Subject,
            Topic = entity.Topic,
            Subtopic = entity.Subtopic,
            ConfidenceScore = entity.ConfidenceScore,
            ImportanceScore = entity.ImportanceScore,
            OccurrenceCount = entity.OccurrenceCount,
            IsVerified = entity.IsVerified,
            MasteryScore = entity.MasteryScore,
            EffectiveKnowledge = entity.EffectiveKnowledge,
            DecayFactor = entity.DecayFactor,
            TotalAttempts = entity.TotalAttempts,
            TotalCorrect = entity.TotalCorrect,
            SuccessRate = entity.SuccessRate,
            CurrentBloomLevel = entity.CurrentBloomLevel,
            BloomLevelName = GetBloomLevelName(entity.CurrentBloomLevel),
            EasySuccessRate = entity.EasySuccessRate,
            MediumSuccessRate = entity.MediumSuccessRate,
            HardSuccessRate = entity.HardSuccessRate,
            NextReview = entity.NextReview,
            FsrsState = entity.FsrsState,
            FsrsStateName = GetFsrsStateName(entity.FsrsState),
            SourceDocumentId = entity.SourceDocumentId,
            LastInteraction = entity.LastInteraction,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Konvertiert UnifiedKnowledgeRelationship zu DTO
    /// </summary>
    internal UnifiedRelationshipDto MapToDto(UnifiedKnowledgeRelationship relationship)
    {
        return new UnifiedRelationshipDto
        {
            Id = relationship.Id,
            SourceEntityId = relationship.SourceEntityId,
            SourceEntityName = relationship.SourceEntity?.Name ?? "",
            TargetEntityId = relationship.TargetEntityId,
            TargetEntityName = relationship.TargetEntity?.Name ?? "",
            RelationshipType = relationship.RelationshipType,
            CurrentStrength = relationship.CurrentStrength,
            Evidence = relationship.Evidence,
            Description = relationship.Description,
            IsVerified = relationship.IsVerified,
            IsBidirectional = relationship.IsBidirectional,
            IsPrerequisite = relationship.IsPrerequisite,
            RequiredMasteryLevel = relationship.IsPrerequisite ? relationship.RequiredMasteryLevel : null,
            IsStrict = relationship.IsStrict
        };
    }

    /// <summary>
    /// Gibt den FSRS-State-Namen zurück
    /// </summary>
    internal static string GetFsrsStateName(int state) => state switch
    {
        0 => "Neu",
        1 => "Lernen",
        2 => "Wiederholen",
        3 => "Erneut Lernen",
        _ => "Unbekannt"
    };

    /// <summary>
    /// Gibt den Bloom-Level-Namen zurück
    /// </summary>
    internal static string GetBloomLevelName(int level)
    {
        return BloomLevels.TryGetValue(level, out var name) ? name : "Unbekannt";
    }

    /// <summary>
    /// Berechnet effektives Wissen (FSRS + Decay Kombination)
    /// Formel: FsrsMastery × (0.4 + 0.6 × DecayFactor) × BaseStrength
    /// </summary>
    internal static double CalculateEffectiveKnowledge(
        double successRate,
        double difficulty,
        double stability,
        double baseStrength,
        double decayFactor)
    {
        // FSRS-basierte Mastery
        var successComponent = successRate * 0.6;
        var difficultyComponent = (1.0 - difficulty) * 0.2;
        var stabilityComponent = Math.Min(stability / 30.0, 0.2);
        var fsrsMastery = successComponent + difficultyComponent + stabilityComponent;

        // Kombiniert mit Decay (Minimum 40% bleibt erhalten)
        return fsrsMastery * (MinimumRetention + (1.0 - MinimumRetention) * decayFactor) * baseStrength;
    }

    /// <summary>
    /// Berechnet Decay-Faktor basierend auf Zeit seit letzter Interaktion
    /// Formel: e^(-DecayRate × Tage)
    /// </summary>
    internal static double CalculateDecayFactor(double decayRate, DateTime lastInteraction)
    {
        var daysSinceInteraction = (DateTime.UtcNow - lastInteraction).TotalDays;
        return Math.Exp(-decayRate * daysSinceInteraction);
    }

    /// <summary>
    /// Bestimmt empfohlene Schwierigkeit nach 20/40/40-Regel
    /// </summary>
    internal static string DetermineRecommendedDifficulty(int easyTotal, int mediumTotal, int hardTotal)
    {
        var total = easyTotal + mediumTotal + hardTotal;
        if (total < 5) return "easy"; // Start mit leicht für neue Lerner

        var easyRatio = (double)easyTotal / total;
        var mediumRatio = (double)mediumTotal / total;

        // Ziel: 20% leicht, 40% mittel, 40% schwer
        if (easyRatio < 0.20) return "easy";
        if (mediumRatio < 0.40) return "medium";
        return "hard";
    }

    /// <summary>
    /// Bestimmt empfohlenen Bloom-Level basierend auf Mastery
    /// </summary>
    internal static int DetermineRecommendedBloomLevel(double masteryScore, int currentBloomLevel)
    {
        // Progression durch Bloom-Level basierend auf Mastery
        // 70% Mastery benötigt für Aufstieg zum nächsten Level
        if (masteryScore >= 0.9 && currentBloomLevel < 6)
            return Math.Min(currentBloomLevel + 2, 6);
        if (masteryScore >= 0.7 && currentBloomLevel < 6)
            return currentBloomLevel + 1;
        if (masteryScore < 0.3 && currentBloomLevel > 1)
            return currentBloomLevel - 1;
        return currentBloomLevel;
    }

    /// <summary>
    /// Berechnet Farbe basierend auf Mastery-Score (für Visualisierung)
    /// </summary>
    internal static string GetMasteryColor(double mastery)
    {
        return mastery switch
        {
            >= 0.8 => "#22c55e", // Grün - Gemeistert
            >= 0.6 => "#84cc16", // Hellgrün - Gut
            >= 0.4 => "#eab308", // Gelb - In Arbeit
            >= 0.2 => "#f97316", // Orange - Schwach
            _ => "#ef4444"       // Rot - Kritisch
        };
    }

    /// <summary>
    /// Berechnet Knotengröße basierend auf Importance-Score
    /// </summary>
    internal static double GetNodeSize(double importance)
    {
        // Skaliert zwischen 10 und 50
        return 10 + (importance * 40);
    }

    #endregion

    #region FSRS Algorithm Implementation

    /// <summary>
    /// Berechnet neuen FSRS-State nach Übungsversuch
    /// </summary>
    internal FsrsUpdateResult UpdateFsrs(
        int currentState,
        double stability,
        double difficulty,
        int reps,
        int lapses,
        bool isCorrect,
        int quality) // 1=Again, 2=Hard, 3=Good, 4=Easy
    {
        var newState = currentState;
        var newStability = stability;
        var newDifficulty = difficulty;
        var newReps = reps;
        var newLapses = lapses;

        if (isCorrect)
        {
            newReps++;

            // Stability erhöhen basierend auf Quality
            var stabilityIncrease = quality switch
            {
                4 => FsrsWeights[3] * (1 + FsrsWeights[12]), // Easy
                3 => FsrsWeights[2],                          // Good
                2 => FsrsWeights[1] * FsrsWeights[11],        // Hard
                _ => FsrsWeights[0]                           // Again (aber korrekt)
            };

            newStability = Math.Min(
                stability * (1 + FsrsWeights[14] * Math.Pow(stability, -FsrsWeights[9]) * (Math.Exp((1 - difficulty) * FsrsWeights[10]) - 1) * (quality - 1 + 0.5)),
                FsrsMaximumInterval
            );

            // Difficulty anpassen
            newDifficulty = Math.Clamp(
                difficulty - FsrsWeights[7] * (quality - 3),
                0.0, 1.0
            );

            // State Transition
            if (currentState == 0 || currentState == 1) // New oder Learning
            {
                if (newReps >= 2 && quality >= 3)
                    newState = 2; // → Review
                else
                    newState = 1; // → Learning
            }
        }
        else
        {
            newLapses++;

            // Stability stark reduzieren
            newStability = Math.Max(
                stability * FsrsWeights[15] * Math.Pow(difficulty, FsrsWeights[13]),
                FsrsMinimumInterval
            );

            // Difficulty erhöhen
            newDifficulty = Math.Clamp(
                difficulty + FsrsWeights[5] * (3 - quality),
                0.0, 1.0
            );

            // State Transition
            if (currentState == 2) // Review
                newState = 3; // → Relearning
            else
                newState = 1; // → Learning
        }

        // Berechne nächstes Review-Datum
        var interval = newState switch
        {
            0 => 0,  // New - sofort
            1 => Math.Min(newReps, 3),  // Learning - 1-3 Tage
            2 => (int)Math.Round(newStability * (1 + 0.5 * (1 - newDifficulty))),  // Review
            3 => 1,  // Relearning - 1 Tag
            _ => 1
        };

        var nextReview = DateTime.UtcNow.AddDays(Math.Max(interval, 1));

        return new FsrsUpdateResult
        {
            NewState = newState,
            NewStability = newStability,
            NewDifficulty = newDifficulty,
            NewReps = newReps,
            NewLapses = newLapses,
            NextReview = nextReview,
            IntervalDays = interval
        };
    }

    internal class FsrsUpdateResult
    {
        public int NewState { get; set; }
        public double NewStability { get; set; }
        public double NewDifficulty { get; set; }
        public int NewReps { get; set; }
        public int NewLapses { get; set; }
        public DateTime NextReview { get; set; }
        public int IntervalDays { get; set; }
    }

    #endregion
}

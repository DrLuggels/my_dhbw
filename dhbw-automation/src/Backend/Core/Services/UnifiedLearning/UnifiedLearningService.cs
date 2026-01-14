using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Core.Services.Embedding;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services.UnifiedLearning;

/// <summary>
/// Unified Learning Service - combines the best features from:
/// - LearningEngineService (entity extraction, Bloom taxonomy, FSRS)
/// - PersonalKnowledgeGraphService (knowledge graph, decay)
/// - RagExerciseService (RAG-based exercise generation)
/// - DeadlinePriorityService (priority calculation)
/// - PrerequisiteService (prerequisite checking)
/// - AdaptiveDifficultyService (20/40/40 rule)
///
/// Uses combined FSRS + Decay algorithm for spaced repetition.
/// </summary>
public partial class UnifiedLearningService : IUnifiedLearningService
{
    private readonly AppDbContext _context;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly AnthropicClient _anthropicClient;
    private readonly EncryptionHelper _encryptionHelper;
    private readonly ILogger<UnifiedLearningService> _logger;

    // Qdrant collection for unified entities
    internal const string UnifiedEntitiesCollection = "dhbw_unified_entities";
    internal const int VectorDimension = 1536;

    // FSRS Parameters (Free Spaced Repetition Scheduler)
    private const double FsrsRequestRetention = 0.9;
    private const double FsrsMaximumInterval = 365.0;
    private const double FsrsW0 = 0.4;
    private const double FsrsW1 = 0.6;
    private const double FsrsW2 = 2.4;
    private const double FsrsW3 = 5.8;
    private const double FsrsW4 = 4.93;
    private const double FsrsW5 = 0.94;
    private const double FsrsW6 = 0.86;
    private const double FsrsW7 = 0.01;
    private const double FsrsW8 = 1.49;
    private const double FsrsW9 = 0.14;
    private const double FsrsW10 = 0.94;
    private const double FsrsW11 = 2.18;
    private const double FsrsW12 = 0.05;
    private const double FsrsW13 = 0.34;
    private const double FsrsW14 = 1.26;
    private const double FsrsW15 = 0.29;
    private const double FsrsW16 = 2.61;

    public UnifiedLearningService(
        AppDbContext context,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        AnthropicClient anthropicClient,
        EncryptionHelper encryptionHelper,
        ILogger<UnifiedLearningService> logger)
    {
        _context = context;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _anthropicClient = anthropicClient;
        _encryptionHelper = encryptionHelper;
        _logger = logger;
    }

    /// <summary>
    /// Initialize the Unified Learning Service (create Qdrant collections, etc.)
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _qdrantService.EnsureCollectionExistsAsync(UnifiedEntitiesCollection, VectorDimension);
            _logger.LogInformation("Unified Learning Service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Unified Learning Service");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Get Anthropic API key for a user or fall back to system key
    /// </summary>
    private async Task<string?> GetAnthropicApiKeyAsync(int? userId)
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
                    _logger.LogError(ex, "Failed to decrypt Anthropic API key for user {UserId}", userId);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Normalize entity name for matching (lowercase, remove special chars)
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
            .Trim();
    }

    /// <summary>
    /// Map UnifiedKnowledgeEntity to DTO
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
    /// Map UnifiedKnowledgeRelationship to DTO
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
    /// Get FSRS state name
    /// </summary>
    internal static string GetFsrsStateName(int state) => state switch
    {
        0 => "New",
        1 => "Learning",
        2 => "Review",
        3 => "Relearning",
        _ => "Unknown"
    };

    /// <summary>
    /// Get Bloom level name
    /// </summary>
    internal static string GetBloomLevelName(int level) => level switch
    {
        1 => "Remember",
        2 => "Understand",
        3 => "Apply",
        4 => "Analyze",
        5 => "Evaluate",
        6 => "Create",
        _ => "Unknown"
    };

    /// <summary>
    /// Calculate effective knowledge combining FSRS mastery with decay
    /// Formula: FsrsMastery × (0.4 + 0.6 × DecayFactor) × BaseStrength
    /// </summary>
    internal static double CalculateEffectiveKnowledge(
        double successRate,
        double difficulty,
        double stability,
        double baseStrength,
        double decayFactor)
    {
        // FSRS-based mastery
        var successComponent = successRate * 0.6;
        var difficultyComponent = (1.0 - difficulty) * 0.2;
        var stabilityComponent = Math.Min(stability / 30.0, 0.2);
        var fsrsMastery = successComponent + difficultyComponent + stabilityComponent;

        // Combined with decay (minimum 40% retained even with no recent interaction)
        return fsrsMastery * (0.4 + 0.6 * decayFactor) * baseStrength;
    }

    /// <summary>
    /// Calculate decay factor based on time since last interaction
    /// Formula: e^(-DecayRate × daysSinceInteraction)
    /// </summary>
    internal static double CalculateDecayFactor(double decayRate, DateTime lastInteraction)
    {
        var daysSinceInteraction = (DateTime.UtcNow - lastInteraction).TotalDays;
        return Math.Exp(-decayRate * daysSinceInteraction);
    }

    /// <summary>
    /// Determine the recommended difficulty based on 20/40/40 rule
    /// </summary>
    internal static string DetermineRecommendedDifficulty(int easyTotal, int mediumTotal, int hardTotal)
    {
        var total = easyTotal + mediumTotal + hardTotal;
        if (total < 5) return "easy"; // Start with easy for new learners

        var easyRatio = (double)easyTotal / total;
        var mediumRatio = (double)mediumTotal / total;

        // Target: 20% easy, 40% medium, 40% hard
        if (easyRatio < 0.20) return "easy";
        if (mediumRatio < 0.40) return "medium";
        return "hard";
    }

    /// <summary>
    /// Determine the recommended Bloom level based on mastery
    /// </summary>
    internal static int DetermineRecommendedBloomLevel(double masteryScore, int currentBloomLevel)
    {
        // Progress through Bloom levels based on mastery
        // Need 70% mastery to advance to next level
        if (masteryScore >= 0.9 && currentBloomLevel < 6)
            return Math.Min(currentBloomLevel + 2, 6);
        if (masteryScore >= 0.7 && currentBloomLevel < 6)
            return currentBloomLevel + 1;
        if (masteryScore < 0.3 && currentBloomLevel > 1)
            return currentBloomLevel - 1;
        return currentBloomLevel;
    }

    #endregion
}

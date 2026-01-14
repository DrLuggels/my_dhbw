using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Core.Services.Embedding;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services.LearningEngine;

/// <summary>
/// DeepTutor-style Learning Engine Service.
/// Handles document processing pipeline, knowledge graph extraction, and adaptive question generation.
/// </summary>
public partial class LearningEngineService : ILearningEngineService
{
    private readonly AppDbContext _context;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly AnthropicClient _anthropicClient;
    private readonly EncryptionHelper _encryptionHelper;
    private readonly ILogger<LearningEngineService> _logger;

    // Qdrant collection for knowledge graph entities
    internal const string KgEntitiesCollection = "dhbw_kg_entities";
    internal const int VectorDimension = 1536;

    public LearningEngineService(
        AppDbContext context,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        AnthropicClient anthropicClient,
        EncryptionHelper encryptionHelper,
        ILogger<LearningEngineService> logger)
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
    /// Initialize the Learning Engine (create Qdrant collections, etc.)
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _qdrantService.EnsureCollectionExistsAsync(KgEntitiesCollection, VectorDimension);
            _logger.LogInformation("Learning Engine initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Learning Engine");
        }
    }

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
        return null; // AnthropicClient will use environment variable
    }

    /// <summary>
    /// Normalize entity name for matching (lowercase, remove special chars)
    /// </summary>
    private static string NormalizeName(string name)
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
    /// Map KgEntity to DTO
    /// </summary>
    private KgEntityDto MapToDto(KgEntity entity, UserEntityPerformance? performance = null)
    {
        return new KgEntityDto
        {
            Id = entity.Id,
            EntityType = entity.EntityType,
            Name = entity.Name,
            Description = entity.Description,
            Subject = entity.Subject,
            Topic = entity.Topic,
            ConfidenceScore = entity.ConfidenceScore,
            ImportanceScore = entity.ImportanceScore,
            OccurrenceCount = entity.OccurrenceCount,
            IsVerified = entity.IsVerified,
            DocumentId = entity.DocumentId,
            ChunkId = entity.ChunkId,
            MasteryScore = performance?.MasteryScore,
            NextReview = performance?.NextReview
        };
    }

    /// <summary>
    /// Map KgRelationship to DTO
    /// </summary>
    private KgRelationshipDto MapToDto(KgRelationship relationship)
    {
        return new KgRelationshipDto
        {
            Id = relationship.Id,
            SourceEntityId = relationship.SourceEntityId,
            SourceEntityName = relationship.SourceEntity?.Name ?? "",
            TargetEntityId = relationship.TargetEntityId,
            TargetEntityName = relationship.TargetEntity?.Name ?? "",
            RelationshipType = relationship.RelationshipType,
            Strength = relationship.Strength,
            Evidence = relationship.Evidence,
            Description = relationship.Description,
            IsVerified = relationship.IsVerified
        };
    }
}

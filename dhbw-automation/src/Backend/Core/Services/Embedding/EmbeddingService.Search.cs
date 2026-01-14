using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.Embedding;

public partial class EmbeddingService
{
    public async Task<List<SemanticSearchResult>> SemanticSearchAsync(
        string query,
        int? userId = null,
        int topK = 10,
        double threshold = 0.0)
    {
        try
        {
            _logger.LogInformation("SemanticSearch starting: query='{Query}', userId={UserId}, topK={TopK}, threshold={Threshold}",
                query, userId, topK, threshold);

            var queryEmbedding = await GenerateEmbeddingAsync(query, userId);
            if (queryEmbedding == null)
            {
                _logger.LogWarning("SemanticSearch: Failed to generate query embedding for '{Query}'", query);
                return new List<SemanticSearchResult>();
            }

            _logger.LogInformation("SemanticSearch: Generated embedding with {Dimensions} dimensions, searching Qdrant...",
                queryEmbedding.Length);

            var results = await _qdrantService.SearchAllCollectionsAsync(queryEmbedding, topK, threshold, userId);

            _logger.LogInformation("SemanticSearch: Qdrant returned {Count} results for query '{Query}'",
                results.Count, query);

            return results.Select(r => new SemanticSearchResult
            {
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                Score = r.Score,
                UserId = r.UserId
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in semantic search for query: {Query}", query);
            return new List<SemanticSearchResult>();
        }
    }

    public async Task<List<SemanticSearchResult>> FindSimilarAsync(
        string entityType,
        int entityId,
        int? userId = null,
        int topK = 10,
        double threshold = 0.0)
    {
        try
        {
            var embedding = await _context.QdrantEmbeddings
                .FirstOrDefaultAsync(e => e.EntityType == entityType && e.EntityId == entityId);

            if (embedding == null)
            {
                _logger.LogWarning("No embedding found for {EntityType}:{EntityId}", entityType, entityId);
                return new List<SemanticSearchResult>();
            }

            string? textToEmbed = entityType switch
            {
                KnowledgeEntityTypes.Document => await GetDocumentTextAsync(entityId),
                KnowledgeEntityTypes.KnowledgeItem => await GetKnowledgeItemTextAsync(entityId),
                KnowledgeEntityTypes.JavaDocsExercise => await GetExerciseTextAsync(entityId),
                _ => null
            };

            if (string.IsNullOrEmpty(textToEmbed))
                return new List<SemanticSearchResult>();

            var queryEmbedding = await GenerateEmbeddingAsync(textToEmbed, userId);
            if (queryEmbedding == null)
                return new List<SemanticSearchResult>();

            var results = await _qdrantService.SearchAllCollectionsAsync(queryEmbedding, topK + 1, threshold, userId);

            return results
                .Where(r => !(r.EntityType == entityType && r.EntityId == entityId))
                .Take(topK)
                .Select(r => new SemanticSearchResult
                {
                    EntityType = r.EntityType,
                    EntityId = r.EntityId,
                    Score = r.Score,
                    UserId = r.UserId
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar entities for {EntityType}:{EntityId}",
                entityType, entityId);
            return new List<SemanticSearchResult>();
        }
    }
}

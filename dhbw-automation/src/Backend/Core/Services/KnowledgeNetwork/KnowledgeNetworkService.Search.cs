using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.KnowledgeNetwork;

public partial class KnowledgeNetworkService
{
    /// <summary>
    /// Find related content using both explicit links and semantic similarity
    /// </summary>
    public async Task<List<RelatedContentItem>> FindRelatedContentAsync(
        string entityType,
        int entityId,
        int? userId = null,
        int maxResults = 20,
        int depth = 1)
    {
        var results = new List<RelatedContentItem>();
        var visited = new HashSet<string> { $"{entityType}:{entityId}" };

        // 1. Get explicit links
        var explicitLinks = await GetLinksForEntityAsync(entityType, entityId, userId);

        foreach (var link in explicitLinks)
        {
            var relatedType = link.SourceType == entityType && link.SourceId == entityId
                ? link.TargetType
                : link.SourceType;
            var relatedId = link.SourceType == entityType && link.SourceId == entityId
                ? link.TargetId
                : link.SourceId;

            var key = $"{relatedType}:{relatedId}";
            if (visited.Contains(key)) continue;
            visited.Add(key);

            var entityInfo = await GetEntityInfoAsync(relatedType, relatedId);
            if (entityInfo != null)
            {
                results.Add(new RelatedContentItem
                {
                    EntityType = relatedType,
                    EntityId = relatedId,
                    Title = entityInfo.Title,
                    Description = entityInfo.Description,
                    Score = (float)link.Strength,
                    LinkType = link.LinkType,
                    IsExplicitLink = true
                });
            }
        }

        // 2. Get semantically similar content
        var semanticResults = await _embeddingService.FindSimilarAsync(
            entityType, entityId, userId, maxResults, AutoLinkThreshold);

        foreach (var result in semanticResults)
        {
            var key = $"{result.EntityType}:{result.EntityId}";
            if (visited.Contains(key)) continue;
            visited.Add(key);

            var entityInfo = await GetEntityInfoAsync(result.EntityType, result.EntityId);
            if (entityInfo != null)
            {
                results.Add(new RelatedContentItem
                {
                    EntityType = result.EntityType,
                    EntityId = result.EntityId,
                    Title = entityInfo.Title,
                    Description = entityInfo.Description,
                    Score = result.Score,
                    LinkType = KnowledgeLinkTypes.Related,
                    IsExplicitLink = false
                });
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Semantic search across all knowledge
    /// </summary>
    public async Task<List<SearchResultItem>> SearchAsync(
        string query,
        int? userId = null,
        int maxResults = 20)
    {
        var results = new List<SearchResultItem>();
        var semanticResults = await _embeddingService.SemanticSearchAsync(query, userId, maxResults);

        _logger.LogWarning(">>> SearchAsync got {Count} semantic results", semanticResults.Count);

        foreach (var result in semanticResults)
        {
            _logger.LogWarning(">>> Processing result: EntityType={EntityType}, EntityId={EntityId}, Score={Score}",
                result.EntityType, result.EntityId, result.Score);

            var entityInfo = await GetEntityInfoAsync(result.EntityType, result.EntityId);
            if (entityInfo != null)
            {
                _logger.LogWarning(">>> Found entity info: Title={Title}", entityInfo.Title);
                results.Add(new SearchResultItem
                {
                    EntityType = result.EntityType,
                    EntityId = result.EntityId,
                    Title = entityInfo.Title,
                    Description = entityInfo.Description,
                    Score = result.Score
                });
            }
            else
            {
                _logger.LogWarning(">>> GetEntityInfoAsync returned NULL for {EntityType}:{EntityId}",
                    result.EntityType, result.EntityId);
            }
        }

        _logger.LogWarning(">>> SearchAsync returning {Count} results", results.Count);

        return results;
    }
}

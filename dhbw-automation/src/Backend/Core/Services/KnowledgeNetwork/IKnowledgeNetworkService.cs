using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.KnowledgeNetwork;

/// <summary>
/// Interface for knowledge network service
/// </summary>
public interface IKnowledgeNetworkService
{
    Task<KnowledgeLink> CreateLinkAsync(
        int? userId,
        string sourceType,
        int sourceId,
        string targetType,
        int targetId,
        string linkType = "related",
        string? description = null);

    Task<bool> DeleteLinkAsync(int linkId, int? userId = null);

    Task<List<KnowledgeLink>> GetLinksForEntityAsync(
        string entityType,
        int entityId,
        int? userId = null);

    Task<List<RelatedContentItem>> FindRelatedContentAsync(
        string entityType,
        int entityId,
        int? userId = null,
        int maxResults = 20,
        int depth = 1);

    Task<int> GenerateSemanticLinksAsync(int? userId = null, double threshold = 0.85);

    Task<List<SearchResultItem>> SearchAsync(
        string query,
        int? userId = null,
        int maxResults = 20);

    Task<NetworkGraph> GetNetworkGraphAsync(int? userId = null, int maxNodes = 100);

    Task<bool> ConfirmLinkAsync(int linkId, int userId);

    Task<bool> RejectLinkAsync(int linkId, int userId);

    Task<List<KnowledgeLink>> GetPendingLinksAsync(int userId);

    Task<IndexingResult> IndexAllUserContentAsync(int userId);

    Task<ClusterVisualizationData> GetClusterVisualizationAsync(
        int userId,
        string method = "umap",
        int maxNodes = 200);
}

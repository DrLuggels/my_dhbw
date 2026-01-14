namespace DHBWAutomation.Backend.Core.Services.Embedding;

public interface IEmbeddingService
{
    Task<float[]?> GenerateEmbeddingAsync(string text, int? userId = null);
    Task<bool> ProcessDocumentEmbeddingAsync(int documentId, int? userId = null);
    Task<bool> ProcessChunkEmbeddingAsync(int chunkId, int? userId = null);
    Task<bool> ProcessKnowledgeItemEmbeddingAsync(int itemId, int? userId = null);
    Task<bool> ProcessExerciseEmbeddingAsync(int exerciseId);
    Task<bool> ProcessImageEmbeddingAsync(int imageId, int? userId = null);
    Task<List<SemanticSearchResult>> SemanticSearchAsync(string query, int? userId = null, int topK = 10, double threshold = 0.0);
    Task<List<SemanticSearchResult>> FindSimilarAsync(string entityType, int entityId, int? userId = null, int topK = 10, double threshold = 0.0);
}

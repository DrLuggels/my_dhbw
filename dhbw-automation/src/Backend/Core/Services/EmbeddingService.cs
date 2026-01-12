using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for generating and managing vector embeddings
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly IQdrantService _qdrantService;
    private readonly AppDbContext _context;
    private readonly EncryptionHelper _encryptionHelper;

    private readonly string? _openAiApiKey;

    private const string OpenAiEmbeddingEndpoint = "https://api.openai.com/v1/embeddings";
    private const string OpenAiEmbeddingModel = "text-embedding-3-small"; // 1536 dimensions, cost-effective
    private const int MaxTextLength = 8000; // ~8k tokens limit for embedding model

    public EmbeddingService(
        IHttpClientFactory httpClientFactory,
        ILogger<EmbeddingService> logger,
        IQdrantService qdrantService,
        AppDbContext context,
        EncryptionHelper encryptionHelper)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _qdrantService = qdrantService;
        _context = context;
        _encryptionHelper = encryptionHelper;

        _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    /// <summary>
    /// Get API key for a user or fall back to system key
    /// </summary>
    private async Task<string?> GetApiKeyAsync(int? userId)
    {
        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && !string.IsNullOrEmpty(user.OpenAiApiKey))
            {
                try
                {
                    var decrypted = _encryptionHelper.Decrypt(user.OpenAiApiKey);
                    if (!string.IsNullOrEmpty(decrypted))
                    {
                        var keyPreview = decrypted.Length > 12 ? decrypted.Substring(0, 12) + "..." : "***";
                        _logger.LogWarning(">>> USING USER {UserId}'s API KEY: {KeyPreview} (length={Length})",
                            userId, keyPreview, decrypted.Length);
                        return decrypted;
                    }
                    _logger.LogWarning("User {UserId} has encrypted API key but decryption returned empty", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt OpenAI API key for user {UserId}", userId);
                }
            }
            else
            {
                _logger.LogDebug("User {UserId} has no OpenAI API key configured", userId);
            }
        }

        if (!string.IsNullOrEmpty(_openAiApiKey))
        {
            var keyPreview = _openAiApiKey.Length > 12 ? _openAiApiKey.Substring(0, 12) + "..." : "***";
            _logger.LogWarning(">>> USING SYSTEM API KEY: {KeyPreview} (length={Length})",
                keyPreview, _openAiApiKey.Length);
            return _openAiApiKey;
        }

        _logger.LogWarning("No OpenAI API key available (neither user nor system)");
        return null;
    }

    /// <summary>
    /// Generate embedding for text using OpenAI
    /// </summary>
    public async Task<float[]?> GenerateEmbeddingAsync(string text, int? userId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Cannot generate embedding for empty text");
                return null;
            }

            var apiKey = await GetApiKeyAsync(userId);
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("OpenAI API Key not available for embedding generation");
                return null;
            }

            // Truncate text if too long
            var inputText = text.Length > MaxTextLength
                ? text.Substring(0, MaxTextLength)
                : text;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = OpenAiEmbeddingModel,
                input = inputText
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogWarning(">>> SENDING REQUEST TO OPENAI: {Endpoint}, model={Model}, inputLength={Length}",
                OpenAiEmbeddingEndpoint, OpenAiEmbeddingModel, inputText.Length);

            var response = await client.PostAsync(OpenAiEmbeddingEndpoint, content);

            _logger.LogWarning(">>> OPENAI RESPONSE STATUS: {StatusCode}", response.StatusCode);

            var responseJson = await response.Content.ReadAsStringAsync();

            _logger.LogWarning(">>> OPENAI RESPONSE (first 500 chars): {Response}",
                responseJson.Length > 500 ? responseJson.Substring(0, 500) : responseJson);

            response.EnsureSuccessStatusCode();

            var result = JsonDocument.Parse(responseJson);

            var embeddingArray = result.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();

            _logger.LogWarning(">>> EMBEDDING GENERATED: {Dimensions} dimensions, first 5 values: [{Values}]",
                embeddingArray.Length,
                string.Join(", ", embeddingArray.Take(5).Select(v => v.ToString("F6"))));
            return embeddingArray;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            return null;
        }
    }

    /// <summary>
    /// Process and store embedding for a document
    /// </summary>
    public async Task<bool> ProcessDocumentEmbeddingAsync(int documentId, int? userId = null)
    {
        try
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return false;
            }

            // Combine relevant text for embedding
            var textToEmbed = BuildDocumentEmbeddingText(document);
            if (string.IsNullOrWhiteSpace(textToEmbed))
            {
                _logger.LogWarning("No text available for document {DocumentId} embedding", documentId);
                return false;
            }

            var embedding = await GenerateEmbeddingAsync(textToEmbed, userId ?? document.UserId);
            if (embedding == null)
            {
                return false;
            }

            // Store in Qdrant
            var pointId = await _qdrantService.UpsertEmbeddingAsync(
                QdrantCollections.Documents,
                embedding,
                KnowledgeEntityTypes.Document,
                documentId,
                document.UserId,
                new Dictionary<string, string>
                {
                    ["filename"] = document.FileName,
                    ["category"] = document.Category ?? "",
                    ["subject"] = document.Subject ?? ""
                }
            );

            // Track in database
            var existingEmbedding = await _context.QdrantEmbeddings
                .FirstOrDefaultAsync(e => e.EntityType == KnowledgeEntityTypes.Document && e.EntityId == documentId);

            if (existingEmbedding != null)
            {
                // Delete old point
                await _qdrantService.DeletePointAsync(QdrantCollections.Documents, existingEmbedding.QdrantPointId);
                existingEmbedding.QdrantPointId = pointId;
                existingEmbedding.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.QdrantEmbeddings.Add(new QdrantEmbedding
                {
                    UserId = document.UserId,
                    EntityType = KnowledgeEntityTypes.Document,
                    EntityId = documentId,
                    QdrantPointId = pointId,
                    CollectionName = QdrantCollections.Documents,
                    EmbeddingModel = OpenAiEmbeddingModel,
                    EmbeddedTextPreview = textToEmbed.Substring(0, Math.Min(textToEmbed.Length, 1000)),
                    FullTextLength = textToEmbed.Length
                });
            }

            // Update document
            document.HasEmbedding = true;
            document.QdrantPointId = pointId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed embedding for document {DocumentId}", documentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document embedding for {DocumentId}", documentId);
            return false;
        }
    }

    /// <summary>
    /// Process and store embedding for a knowledge base item
    /// </summary>
    public async Task<bool> ProcessKnowledgeItemEmbeddingAsync(int itemId, int? userId = null)
    {
        try
        {
            var item = await _context.KnowledgeBaseItems.FindAsync(itemId);
            if (item == null)
            {
                _logger.LogWarning("KnowledgeBaseItem {ItemId} not found", itemId);
                return false;
            }

            var textToEmbed = $"{item.Subject}: {item.Topic}";
            if (!string.IsNullOrEmpty(item.Subtopic))
            {
                textToEmbed += $" - {item.Subtopic}";
            }
            if (!string.IsNullOrEmpty(item.Notes))
            {
                textToEmbed += $". {item.Notes}";
            }

            var embedding = await GenerateEmbeddingAsync(textToEmbed, userId ?? item.UserId);
            if (embedding == null)
            {
                return false;
            }

            var pointId = await _qdrantService.UpsertEmbeddingAsync(
                QdrantCollections.KnowledgeItems,
                embedding,
                KnowledgeEntityTypes.KnowledgeItem,
                itemId,
                item.UserId,
                new Dictionary<string, string>
                {
                    ["subject"] = item.Subject,
                    ["topic"] = item.Topic,
                    ["category"] = item.Category
                }
            );

            // Track in database
            var existingEmbedding = await _context.QdrantEmbeddings
                .FirstOrDefaultAsync(e => e.EntityType == KnowledgeEntityTypes.KnowledgeItem && e.EntityId == itemId);

            if (existingEmbedding != null)
            {
                await _qdrantService.DeletePointAsync(QdrantCollections.KnowledgeItems, existingEmbedding.QdrantPointId);
                existingEmbedding.QdrantPointId = pointId;
                existingEmbedding.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.QdrantEmbeddings.Add(new QdrantEmbedding
                {
                    UserId = item.UserId,
                    EntityType = KnowledgeEntityTypes.KnowledgeItem,
                    EntityId = itemId,
                    QdrantPointId = pointId,
                    CollectionName = QdrantCollections.KnowledgeItems,
                    EmbeddingModel = OpenAiEmbeddingModel,
                    EmbeddedTextPreview = textToEmbed,
                    FullTextLength = textToEmbed.Length
                });
            }

            item.HasEmbedding = true;
            item.QdrantPointId = pointId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed embedding for knowledge item {ItemId}", itemId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing knowledge item embedding for {ItemId}", itemId);
            return false;
        }
    }

    /// <summary>
    /// Process and store embedding for a Java docs exercise
    /// </summary>
    public async Task<bool> ProcessExerciseEmbeddingAsync(int exerciseId)
    {
        try
        {
            var exercise = await _context.JavaDocsExercises.FindAsync(exerciseId);
            if (exercise == null)
            {
                _logger.LogWarning("JavaDocsExercise {ExerciseId} not found", exerciseId);
                return false;
            }

            var textToEmbed = $"{exercise.Title}. Topic: {exercise.Topic}";
            if (!string.IsNullOrEmpty(exercise.Subtopic))
            {
                textToEmbed += $" - {exercise.Subtopic}";
            }
            if (!string.IsNullOrEmpty(exercise.ParsedContent))
            {
                textToEmbed += $"\n\n{exercise.ParsedContent}";
            }

            var embedding = await GenerateEmbeddingAsync(textToEmbed);
            if (embedding == null)
            {
                return false;
            }

            var pointId = await _qdrantService.UpsertEmbeddingAsync(
                QdrantCollections.Exercises,
                embedding,
                KnowledgeEntityTypes.JavaDocsExercise,
                exerciseId,
                null, // Global (not user-specific)
                new Dictionary<string, string>
                {
                    ["title"] = exercise.Title,
                    ["topic"] = exercise.Topic,
                    ["difficulty"] = exercise.Difficulty ?? "medium"
                }
            );

            // Track in database
            var existingEmbedding = await _context.QdrantEmbeddings
                .FirstOrDefaultAsync(e => e.EntityType == KnowledgeEntityTypes.JavaDocsExercise && e.EntityId == exerciseId);

            if (existingEmbedding != null)
            {
                await _qdrantService.DeletePointAsync(QdrantCollections.Exercises, existingEmbedding.QdrantPointId);
                existingEmbedding.QdrantPointId = pointId;
                existingEmbedding.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.QdrantEmbeddings.Add(new QdrantEmbedding
                {
                    UserId = null,
                    EntityType = KnowledgeEntityTypes.JavaDocsExercise,
                    EntityId = exerciseId,
                    QdrantPointId = pointId,
                    CollectionName = QdrantCollections.Exercises,
                    EmbeddingModel = OpenAiEmbeddingModel,
                    EmbeddedTextPreview = textToEmbed.Substring(0, Math.Min(textToEmbed.Length, 1000)),
                    FullTextLength = textToEmbed.Length
                });
            }

            exercise.HasEmbedding = true;
            exercise.QdrantPointId = pointId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed embedding for exercise {ExerciseId}", exerciseId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing exercise embedding for {ExerciseId}", exerciseId);
            return false;
        }
    }

    /// <summary>
    /// Process and store embedding for a document chunk
    /// </summary>
    public async Task<bool> ProcessChunkEmbeddingAsync(int chunkId, int? userId = null)
    {
        try
        {
            var chunk = await _context.DocumentChunks
                .Include(c => c.Document)
                .FirstOrDefaultAsync(c => c.Id == chunkId);

            if (chunk == null)
            {
                _logger.LogWarning("DocumentChunk {ChunkId} not found", chunkId);
                return false;
            }

            // Build embedding text with context
            var textToEmbed = BuildChunkEmbeddingText(chunk);
            if (string.IsNullOrWhiteSpace(textToEmbed))
            {
                _logger.LogWarning("No text available for chunk {ChunkId} embedding", chunkId);
                return false;
            }

            var embedding = await GenerateEmbeddingAsync(textToEmbed, userId ?? chunk.UserId);
            if (embedding == null)
            {
                return false;
            }

            // Store in Qdrant
            var pointId = await _qdrantService.UpsertEmbeddingAsync(
                QdrantCollections.Chunks,
                embedding,
                KnowledgeEntityTypes.DocumentChunk,
                chunkId,
                chunk.UserId,
                new Dictionary<string, string>
                {
                    ["document_id"] = chunk.DocumentId.ToString(),
                    ["chunk_index"] = chunk.ChunkIndex.ToString(),
                    ["topic"] = chunk.TopicLabel ?? "",
                    ["filename"] = chunk.Document.FileName
                }
            );

            // Track in database
            var existingEmbedding = await _context.QdrantEmbeddings
                .FirstOrDefaultAsync(e => e.EntityType == KnowledgeEntityTypes.DocumentChunk && e.EntityId == chunkId);

            if (existingEmbedding != null)
            {
                await _qdrantService.DeletePointAsync(QdrantCollections.Chunks, existingEmbedding.QdrantPointId);
                existingEmbedding.QdrantPointId = pointId;
                existingEmbedding.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.QdrantEmbeddings.Add(new QdrantEmbedding
                {
                    UserId = chunk.UserId,
                    EntityType = KnowledgeEntityTypes.DocumentChunk,
                    EntityId = chunkId,
                    QdrantPointId = pointId,
                    CollectionName = QdrantCollections.Chunks,
                    EmbeddingModel = OpenAiEmbeddingModel,
                    EmbeddedTextPreview = textToEmbed.Substring(0, Math.Min(textToEmbed.Length, 1000)),
                    FullTextLength = textToEmbed.Length
                });
            }

            // Update chunk
            chunk.HasEmbedding = true;
            chunk.QdrantPointId = pointId;
            chunk.Status = "embedded";
            chunk.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Processed embedding for chunk {ChunkId}", chunkId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chunk embedding for {ChunkId}", chunkId);
            return false;
        }
    }

    private string BuildChunkEmbeddingText(DocumentChunk chunk)
    {
        var sb = new StringBuilder();

        // Add document context
        sb.AppendLine($"Document: {chunk.Document.FileName}");

        if (!string.IsNullOrEmpty(chunk.Document.Subject))
            sb.AppendLine($"Subject: {chunk.Document.Subject}");

        if (!string.IsNullOrEmpty(chunk.Document.Category))
            sb.AppendLine($"Category: {chunk.Document.Category}");

        // Add chunk metadata
        if (!string.IsNullOrEmpty(chunk.TopicLabel))
            sb.AppendLine($"Topic: {chunk.TopicLabel}");

        if (!string.IsNullOrEmpty(chunk.ChunkType) && chunk.ChunkType != "mixed")
            sb.AppendLine($"Type: {chunk.ChunkType}");

        sb.AppendLine();
        sb.Append(chunk.Content);

        return sb.ToString();
    }

    /// <summary>
    /// Process embedding for a document image
    /// </summary>
    public async Task<bool> ProcessImageEmbeddingAsync(int imageId, int? userId = null)
    {
        try
        {
            var image = await _context.DocumentImages
                .Include(i => i.Document)
                .FirstOrDefaultAsync(i => i.Id == imageId);

            if (image == null)
            {
                _logger.LogWarning("DocumentImage {ImageId} not found", imageId);
                return false;
            }

            // Use the Gemini description for embedding
            if (string.IsNullOrWhiteSpace(image.GeminiDescription))
            {
                _logger.LogWarning("Image {ImageId} has no description for embedding", imageId);
                return false;
            }

            var textToEmbed = $"Image from {image.Document.FileName}, page {image.PageNumber}: {image.GeminiDescription}";
            if (!string.IsNullOrEmpty(image.ExtractedText))
            {
                textToEmbed += $"\nExtracted text: {image.ExtractedText}";
            }

            var embedding = await GenerateEmbeddingAsync(textToEmbed, userId ?? image.Document.UserId);
            if (embedding == null)
            {
                return false;
            }

            var pointId = await _qdrantService.UpsertEmbeddingAsync(
                QdrantCollections.Images,
                embedding,
                KnowledgeEntityTypes.Image,
                imageId,
                image.Document.UserId,
                new Dictionary<string, string>
                {
                    ["document_id"] = image.DocumentId.ToString(),
                    ["page"] = image.PageNumber.ToString(),
                    ["image_type"] = image.ImageType ?? "unknown"
                }
            );

            image.HasEmbedding = true;
            image.QdrantPointId = pointId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed embedding for image {ImageId}", imageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image embedding for {ImageId}", imageId);
            return false;
        }
    }

    /// <summary>
    /// Semantic search across all content
    /// </summary>
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

    /// <summary>
    /// Find similar entities to a given entity
    /// </summary>
    public async Task<List<SemanticSearchResult>> FindSimilarAsync(
        string entityType,
        int entityId,
        int? userId = null,
        int topK = 10,
        double threshold = 0.0)
    {
        try
        {
            // Get the existing embedding
            var embedding = await _context.QdrantEmbeddings
                .FirstOrDefaultAsync(e => e.EntityType == entityType && e.EntityId == entityId);

            if (embedding == null)
            {
                _logger.LogWarning("No embedding found for {EntityType}:{EntityId}", entityType, entityId);
                return new List<SemanticSearchResult>();
            }

            // We need to fetch the vector from Qdrant - for now, regenerate from text
            // In production, you might want to cache vectors or fetch from Qdrant

            string? textToEmbed = entityType switch
            {
                KnowledgeEntityTypes.Document => await GetDocumentTextAsync(entityId),
                KnowledgeEntityTypes.KnowledgeItem => await GetKnowledgeItemTextAsync(entityId),
                KnowledgeEntityTypes.JavaDocsExercise => await GetExerciseTextAsync(entityId),
                _ => null
            };

            if (string.IsNullOrEmpty(textToEmbed))
            {
                return new List<SemanticSearchResult>();
            }

            var queryEmbedding = await GenerateEmbeddingAsync(textToEmbed, userId);
            if (queryEmbedding == null)
            {
                return new List<SemanticSearchResult>();
            }

            var results = await _qdrantService.SearchAllCollectionsAsync(queryEmbedding, topK + 1, threshold, userId);

            // Exclude the source entity itself
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

    private string BuildDocumentEmbeddingText(Document document)
    {
        var sb = new StringBuilder();

        sb.Append($"Document: {document.FileName}");

        if (!string.IsNullOrEmpty(document.Category))
            sb.Append($" | Category: {document.Category}");

        if (!string.IsNullOrEmpty(document.Subject))
            sb.Append($" | Subject: {document.Subject}");

        if (!string.IsNullOrEmpty(document.Summary))
            sb.Append($"\n\nSummary: {document.Summary}");

        if (!string.IsNullOrEmpty(document.ExtractedText))
        {
            var textToAdd = document.ExtractedText.Length > MaxTextLength - sb.Length - 100
                ? document.ExtractedText.Substring(0, MaxTextLength - sb.Length - 100)
                : document.ExtractedText;
            sb.Append($"\n\nContent: {textToAdd}");
        }

        return sb.ToString();
    }

    private async Task<string?> GetDocumentTextAsync(int documentId)
    {
        var doc = await _context.Documents.FindAsync(documentId);
        return doc != null ? BuildDocumentEmbeddingText(doc) : null;
    }

    private async Task<string?> GetKnowledgeItemTextAsync(int itemId)
    {
        var item = await _context.KnowledgeBaseItems.FindAsync(itemId);
        if (item == null) return null;

        var text = $"{item.Subject}: {item.Topic}";
        if (!string.IsNullOrEmpty(item.Subtopic))
            text += $" - {item.Subtopic}";
        if (!string.IsNullOrEmpty(item.Notes))
            text += $". {item.Notes}";

        return text;
    }

    private async Task<string?> GetExerciseTextAsync(int exerciseId)
    {
        var exercise = await _context.JavaDocsExercises.FindAsync(exerciseId);
        if (exercise == null) return null;

        var text = $"{exercise.Title}. Topic: {exercise.Topic}";
        if (!string.IsNullOrEmpty(exercise.ParsedContent))
            text += $"\n\n{exercise.ParsedContent}";

        return text;
    }
}

/// <summary>
/// Result of a semantic search
/// </summary>
public class SemanticSearchResult
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public float Score { get; set; }
    public int? UserId { get; set; }
}

/// <summary>
/// Interface for embedding service
/// </summary>
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

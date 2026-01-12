using Qdrant.Client;
using Qdrant.Client.Grpc;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Infrastructure.VectorDb;

/// <summary>
/// Service for interacting with Qdrant vector database
/// </summary>
public class QdrantService : IQdrantService
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantService> _logger;
    private readonly IConfiguration _configuration;

    private const int DefaultVectorSize = 1536; // OpenAI text-embedding-3-small

    public QdrantService(ILogger<QdrantService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var host = configuration["Qdrant:Host"] ?? "localhost";
        var port = int.Parse(configuration["Qdrant:Port"] ?? "6334");

        _client = new QdrantClient(host, port);
        _logger.LogInformation("QdrantService initialized with host: {Host}:{Port}", host, port);
    }

    /// <summary>
    /// Ensures a collection exists with the proper configuration
    /// </summary>
    public async Task EnsureCollectionExistsAsync(string collectionName, int vectorSize = DefaultVectorSize)
    {
        try
        {
            var collections = await _client.ListCollectionsAsync();
            if (collections.Any(c => c == collectionName))
            {
                _logger.LogDebug("Collection {CollectionName} already exists", collectionName);
                return;
            }

            await _client.CreateCollectionAsync(
                collectionName,
                new VectorParams
                {
                    Size = (ulong)vectorSize,
                    Distance = Distance.Cosine
                }
            );

            _logger.LogInformation("Created Qdrant collection: {CollectionName} with vector size {VectorSize}",
                collectionName, vectorSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring collection exists: {CollectionName}", collectionName);
            throw;
        }
    }

    /// <summary>
    /// Initialize all required collections for the knowledge network
    /// </summary>
    public async Task InitializeCollectionsAsync()
    {
        await EnsureCollectionExistsAsync(QdrantCollections.Documents);
        await EnsureCollectionExistsAsync(QdrantCollections.Exercises);
        await EnsureCollectionExistsAsync(QdrantCollections.KnowledgeItems);
        await EnsureCollectionExistsAsync(QdrantCollections.Images);
        await EnsureCollectionExistsAsync(QdrantCollections.Chunks);

        _logger.LogInformation("All Qdrant collections initialized");
    }

    /// <summary>
    /// Upsert a vector embedding with metadata
    /// </summary>
    public async Task<string> UpsertEmbeddingAsync(
        string collectionName,
        float[] vector,
        string entityType,
        int entityId,
        int? userId = null,
        Dictionary<string, string>? additionalPayload = null)
    {
        try
        {
            var pointId = Guid.NewGuid();

            var payload = new Dictionary<string, Value>
            {
                ["entity_type"] = new Value { StringValue = entityType },
                ["entity_id"] = new Value { IntegerValue = entityId }
            };

            if (userId.HasValue)
            {
                payload["user_id"] = new Value { IntegerValue = userId.Value };
            }

            if (additionalPayload != null)
            {
                foreach (var kvp in additionalPayload)
                {
                    payload[kvp.Key] = new Value { StringValue = kvp.Value };
                }
            }

            var point = new PointStruct
            {
                Id = new PointId { Uuid = pointId.ToString() },
                Vectors = vector,
                Payload = { payload }
            };

            await _client.UpsertAsync(collectionName, new[] { point });

            _logger.LogDebug("Upserted embedding for {EntityType}:{EntityId} to {Collection}",
                entityType, entityId, collectionName);

            return pointId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting embedding for {EntityType}:{EntityId}",
                entityType, entityId);
            throw;
        }
    }

    /// <summary>
    /// Search for similar vectors
    /// </summary>
    public async Task<List<SimilarityResult>> SearchSimilarAsync(
        string collectionName,
        float[] queryVector,
        int topK = 10,
        double threshold = 0.7,
        int? userId = null)
    {
        try
        {
            Filter? filter = null;
            if (userId.HasValue)
            {
                filter = new Filter
                {
                    Should =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "user_id",
                                Match = new Match { Integer = userId.Value }
                            }
                        },
                        // Also include global items (no user_id)
                        new Condition
                        {
                            IsEmpty = new IsEmptyCondition { Key = "user_id" }
                        }
                    }
                };
            }

            var results = await _client.SearchAsync(
                collectionName,
                queryVector,
                limit: (ulong)topK,
                scoreThreshold: (float)threshold,
                filter: filter
            );

            return results.Select(r => new SimilarityResult
            {
                PointId = r.Id.Uuid,
                Score = r.Score,
                EntityType = r.Payload.TryGetValue("entity_type", out var et) ? et.StringValue : "unknown",
                EntityId = r.Payload.TryGetValue("entity_id", out var eid) ? (int)eid.IntegerValue : 0,
                UserId = r.Payload.TryGetValue("user_id", out var uid) ? (int?)uid.IntegerValue : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching similar vectors in {Collection}", collectionName);
            return new List<SimilarityResult>();
        }
    }

    /// <summary>
    /// Search across all collections
    /// </summary>
    public async Task<List<SimilarityResult>> SearchAllCollectionsAsync(
        float[] queryVector,
        int topK = 10,
        double threshold = 0.7,
        int? userId = null)
    {
        var allResults = new List<SimilarityResult>();

        var collections = new[]
        {
            QdrantCollections.Documents,
            QdrantCollections.Chunks,
            QdrantCollections.Exercises,
            QdrantCollections.KnowledgeItems,
            QdrantCollections.Images
        };

        foreach (var collection in collections)
        {
            try
            {
                var results = await SearchSimilarAsync(collection, queryVector, topK, threshold, userId);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error searching collection {Collection}, skipping", collection);
            }
        }

        // Sort by score and take top K
        return allResults
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    /// <summary>
    /// Delete a point by its ID
    /// </summary>
    public async Task DeletePointAsync(string collectionName, string pointId)
    {
        try
        {
            // Parse UUID string to Guid
            if (!Guid.TryParse(pointId, out var guid))
            {
                _logger.LogError("Invalid UUID format: {PointId}", pointId);
                throw new ArgumentException($"Invalid UUID format: {pointId}");
            }

            // Use filter-based deletion since direct ID deletion has API compatibility issues
            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "id",
                            Match = new Match { Keyword = guid.ToString() }
                        }
                    }
                }
            };

            await _client.DeleteAsync(collectionName, filter);

            _logger.LogDebug("Deleted point {PointId} from {Collection}", pointId, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting point {PointId} from {Collection}",
                pointId, collectionName);
            throw;
        }
    }

    /// <summary>
    /// Delete all points for a specific entity
    /// </summary>
    public async Task DeleteEntityPointsAsync(string collectionName, string entityType, int entityId)
    {
        try
        {
            await _client.DeleteAsync(
                collectionName,
                new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "entity_type",
                                Match = new Match { Keyword = entityType }
                            }
                        },
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "entity_id",
                                Match = new Match { Integer = entityId }
                            }
                        }
                    }
                }
            );

            _logger.LogDebug("Deleted all points for {EntityType}:{EntityId} from {Collection}",
                entityType, entityId, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity points for {EntityType}:{EntityId}",
                entityType, entityId);
            throw;
        }
    }

    /// <summary>
    /// Get collection info
    /// </summary>
    public async Task<CollectionInfo?> GetCollectionInfoAsync(string collectionName)
    {
        try
        {
            var info = await _client.GetCollectionInfoAsync(collectionName);

            return new CollectionInfo
            {
                Name = collectionName,
                PointsCount = (long)info.PointsCount,
                VectorsCount = (long)info.VectorsCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection info for {Collection}", collectionName);
            return null;
        }
    }

    /// <summary>
    /// Get points with their vectors for visualization
    /// </summary>
    public async Task<List<PointWithVector>> GetPointsWithVectorsAsync(
        string collectionName,
        int? userId = null,
        int limit = 200)
    {
        try
        {
            Filter? filter = null;
            if (userId.HasValue)
            {
                filter = new Filter
                {
                    Should =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "user_id",
                                Match = new Match { Integer = userId.Value }
                            }
                        },
                        new Condition
                        {
                            IsEmpty = new IsEmptyCondition { Key = "user_id" }
                        }
                    }
                };
            }

            // Use ScrollAsync with vectorsSelector parameter
            var scrollResult = await _client.ScrollAsync(
                collectionName,
                filter: filter,
                limit: (uint)limit,
                vectorsSelector: new WithVectorsSelector { Enable = true },
                payloadSelector: true
            );

            var results = new List<PointWithVector>();
            foreach (var point in scrollResult.Result)
            {
                var vector = point.Vectors?.Vector?.Data?.ToArray();
                if (vector == null || vector.Length == 0) continue;

                results.Add(new PointWithVector
                {
                    PointId = point.Id.Uuid,
                    Vector = vector,
                    EntityType = point.Payload.TryGetValue("entity_type", out var et) ? et.StringValue : "unknown",
                    EntityId = point.Payload.TryGetValue("entity_id", out var eid) ? (int)eid.IntegerValue : 0,
                    UserId = point.Payload.TryGetValue("user_id", out var uid) ? (int?)uid.IntegerValue : null,
                    Topic = point.Payload.TryGetValue("topic", out var topic) ? topic.StringValue : null,
                    Filename = point.Payload.TryGetValue("filename", out var fn) ? fn.StringValue : null
                });
            }

            _logger.LogInformation("Retrieved {Count} points with vectors from {Collection}", results.Count, collectionName);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting points with vectors from {Collection}", collectionName);
            return new List<PointWithVector>();
        }
    }

    /// <summary>
    /// Get points from all collections for visualization
    /// </summary>
    public async Task<List<PointWithVector>> GetAllPointsWithVectorsAsync(int? userId = null, int limitPerCollection = 50)
    {
        var allPoints = new List<PointWithVector>();

        var collections = new[]
        {
            QdrantCollections.Documents,
            QdrantCollections.Chunks,
            QdrantCollections.Exercises,
            QdrantCollections.KnowledgeItems,
            QdrantCollections.Images
        };

        foreach (var collection in collections)
        {
            try
            {
                var points = await GetPointsWithVectorsAsync(collection, userId, limitPerCollection);
                allPoints.AddRange(points);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting points from {Collection}, skipping", collection);
            }
        }

        return allPoints;
    }
}

/// <summary>
/// Result of a similarity search
/// </summary>
public class SimilarityResult
{
    public string PointId { get; set; } = string.Empty;
    public float Score { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int? UserId { get; set; }
}

/// <summary>
/// Point with its vector for visualization
/// </summary>
public class PointWithVector
{
    public string PointId { get; set; } = string.Empty;
    public float[] Vector { get; set; } = Array.Empty<float>();
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int? UserId { get; set; }
    public string? Topic { get; set; }
    public string? Filename { get; set; }
}

/// <summary>
/// Collection statistics
/// </summary>
public class CollectionInfo
{
    public string Name { get; set; } = string.Empty;
    public long PointsCount { get; set; }
    public long VectorsCount { get; set; }
}

/// <summary>
/// Interface for Qdrant service
/// </summary>
public interface IQdrantService
{
    Task EnsureCollectionExistsAsync(string collectionName, int vectorSize = 1536);
    Task InitializeCollectionsAsync();
    Task<string> UpsertEmbeddingAsync(string collectionName, float[] vector, string entityType, int entityId, int? userId = null, Dictionary<string, string>? additionalPayload = null);
    Task<List<SimilarityResult>> SearchSimilarAsync(string collectionName, float[] queryVector, int topK = 10, double threshold = 0.7, int? userId = null);
    Task<List<SimilarityResult>> SearchAllCollectionsAsync(float[] queryVector, int topK = 10, double threshold = 0.7, int? userId = null);
    Task DeletePointAsync(string collectionName, string pointId);
    Task DeleteEntityPointsAsync(string collectionName, string entityType, int entityId);
    Task<CollectionInfo?> GetCollectionInfoAsync(string collectionName);
    Task<List<PointWithVector>> GetPointsWithVectorsAsync(string collectionName, int? userId = null, int limit = 200);
    Task<List<PointWithVector>> GetAllPointsWithVectorsAsync(int? userId = null, int limitPerCollection = 50);
}

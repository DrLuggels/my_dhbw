using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for semantic document chunking using AI
/// </summary>
public class ChunkingService : IChunkingService
{
    private readonly AppDbContext _context;
    private readonly AnthropicClient _anthropicClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<ChunkingService> _logger;
    private readonly EncryptionHelper _encryptionHelper;

    public ChunkingService(
        AppDbContext context,
        AnthropicClient anthropicClient,
        IEmbeddingService embeddingService,
        ILogger<ChunkingService> logger,
        EncryptionHelper encryptionHelper)
    {
        _context = context;
        _anthropicClient = anthropicClient;
        _embeddingService = embeddingService;
        _logger = logger;
        _encryptionHelper = encryptionHelper;
    }

    /// <summary>
    /// Analyze document and create semantic chunks
    /// </summary>
    public async Task<List<int>> ChunkDocumentAsync(int documentId, ChunkingOptions? options = null)
    {
        options ??= ChunkingOptions.Default;

        var document = await _context.Documents.FindAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found", documentId);
            return new List<int>();
        }

        if (string.IsNullOrWhiteSpace(document.ExtractedText))
        {
            _logger.LogWarning("Document {DocumentId} has no extracted text", documentId);
            return new List<int>();
        }

        // Delete existing chunks
        var existingChunks = await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync();

        if (existingChunks.Any())
        {
            _context.DocumentChunks.RemoveRange(existingChunks);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted {Count} existing chunks for document {DocumentId}",
                existingChunks.Count, documentId);
        }

        // Chunk the text
        List<ChunkPreview> chunkPreviews;

        if (options.UseSemanticChunking && document.ExtractedText.Length > options.MinChunkSize * 2)
        {
            try
            {
                chunkPreviews = await SemanticChunkAsync(document.ExtractedText, options, document.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Semantic chunking failed for document {DocumentId}, falling back to simple chunking", documentId);
                chunkPreviews = SimpleChunk(document.ExtractedText, options);
            }
        }
        else
        {
            chunkPreviews = SimpleChunk(document.ExtractedText, options);
        }

        if (chunkPreviews.Count == 0)
        {
            _logger.LogWarning("No chunks created for document {DocumentId}", documentId);
            return new List<int>();
        }

        // Create chunk entities
        var chunks = new List<DocumentChunk>();
        for (int i = 0; i < chunkPreviews.Count; i++)
        {
            var preview = chunkPreviews[i];
            chunks.Add(new DocumentChunk
            {
                DocumentId = documentId,
                UserId = document.UserId,
                Content = preview.Content,
                ContentLength = preview.Content.Length,
                ChunkIndex = i,
                TotalChunks = chunkPreviews.Count,
                StartPosition = preview.StartPosition,
                EndPosition = preview.EndPosition,
                TopicLabel = preview.TopicLabel,
                Summary = preview.Summary,
                ChunkType = preview.ChunkType ?? "mixed",
                Status = "chunked"
            });
        }

        _context.DocumentChunks.AddRange(chunks);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} chunks for document {DocumentId}", chunks.Count, documentId);

        // Generate embeddings if requested
        if (options.GenerateEmbeddings)
        {
            foreach (var chunk in chunks)
            {
                try
                {
                    await _embeddingService.ProcessChunkEmbeddingAsync(chunk.Id, document.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for chunk {ChunkId}", chunk.Id);
                    chunk.Status = "failed";
                    chunk.ErrorMessage = ex.Message;
                }
            }
            await _context.SaveChangesAsync();
        }

        // Update document metadata
        document.ChunkCount = chunks.Count;
        document.IsChunked = true;
        document.ChunkedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return chunks.Select(c => c.Id).ToList();
    }

    /// <summary>
    /// Re-chunk a document (deletes existing chunks first)
    /// </summary>
    public async Task<List<int>> ReChunkDocumentAsync(int documentId, ChunkingOptions? options = null)
    {
        // ChunkDocumentAsync already handles deletion of existing chunks
        return await ChunkDocumentAsync(documentId, options);
    }

    /// <summary>
    /// Get chunks for a document
    /// </summary>
    public async Task<List<DocumentChunk>> GetDocumentChunksAsync(int documentId)
    {
        return await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync();
    }

    /// <summary>
    /// Chunk raw text without document association (for preview)
    /// </summary>
    public async Task<List<ChunkPreview>> PreviewChunksAsync(string text, ChunkingOptions? options = null, int? userId = null)
    {
        options ??= ChunkingOptions.Default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<ChunkPreview>();
        }

        if (options.UseSemanticChunking && text.Length > options.MinChunkSize * 2)
        {
            try
            {
                return await SemanticChunkAsync(text, options, userId);
            }
            catch
            {
                return SimpleChunk(text, options);
            }
        }

        return SimpleChunk(text, options);
    }

    /// <summary>
    /// Batch chunk multiple documents
    /// </summary>
    public async Task<ChunkingBatchResult> ChunkDocumentsBatchAsync(IEnumerable<int> documentIds, ChunkingOptions? options = null)
    {
        var result = new ChunkingBatchResult();
        var ids = documentIds.ToList();
        result.TotalDocuments = ids.Count;

        foreach (var documentId in ids)
        {
            try
            {
                var chunkIds = await ChunkDocumentAsync(documentId, options);
                result.SuccessCount++;
                result.TotalChunksCreated += chunkIds.Count;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add($"Document {documentId}: {ex.Message}");
                _logger.LogError(ex, "Failed to chunk document {DocumentId}", documentId);
            }

            // Rate limiting delay between documents
            await Task.Delay(500);
        }

        return result;
    }

    /// <summary>
    /// Uses Claude to identify semantic boundaries
    /// </summary>
    private async Task<List<ChunkPreview>> SemanticChunkAsync(
        string text,
        ChunkingOptions options,
        int? userId = null)
    {
        var apiKey = await GetAnthropicApiKeyAsync(userId);

        // Truncate text if too long for Claude context
        var maxTextLength = 50000; // ~12k tokens
        var textToAnalyze = text.Length > maxTextLength
            ? text.Substring(0, maxTextLength)
            : text;

        var systemPrompt = $@"Du bist ein Experte für Dokumentenanalyse und semantische Textstrukturierung.

Deine Aufgabe ist es, den gegebenen Text in semantisch zusammenhängende Abschnitte (Chunks) aufzuteilen.

REGELN:
1. Jeder Chunk sollte ein klar abgegrenztes Thema oder Konzept behandeln
2. Trenne bei Themenwechseln, neuen Konzepten, oder klaren Abschnittsübergängen
3. Halte zusammenhängende Erklärungen und Definitionen zusammen
4. Bei Notizen mit gemischten Themen: Trenne verschiedene Themen in separate Chunks
5. Zielgröße pro Chunk: ca. {options.TargetChunkSize} Zeichen (flexibel je nach Inhalt)
6. Minimum: {options.MinChunkSize} Zeichen, Maximum: {options.MaxChunkSize} Zeichen
7. Erkenne verschiedene Chunk-Typen: introduction, definition, example, exercise, conclusion, mixed

WICHTIG: Antworte NUR mit einem validen JSON-Array im folgenden Format:
[
  {{
    ""start"": 0,
    ""end"": 500,
    ""topic"": ""Kurzes Topic Label (max 50 Zeichen)"",
    ""type"": ""definition""
  }},
  {{
    ""start"": 500,
    ""end"": 1200,
    ""topic"": ""Nächstes Topic"",
    ""type"": ""example""
  }}
]

Die start/end Werte sind Zeichenpositionen im Originaltext.
Stelle sicher, dass die Chunks lückenlos sind (end eines Chunks = start des nächsten).";

        var userMessage = $"Analysiere und chunke diesen Text:\n\n{textToAnalyze}";

        try
        {
            var responseJson = await _anthropicClient.ChatJsonAsync(
                systemPrompt,
                userMessage,
                model: "claude-sonnet-4-5",
                maxTokens: 4096,
                apiKey: apiKey
            );

            var boundaries = ParseChunkBoundaries(responseJson, text.Length);

            if (boundaries.Count == 0)
            {
                _logger.LogWarning("Claude returned no valid chunk boundaries, falling back to simple chunking");
                return SimpleChunk(text, options);
            }

            return boundaries.Select((b, i) => new ChunkPreview
            {
                Index = i,
                Content = text.Substring(b.Start, Math.Min(b.End - b.Start, text.Length - b.Start)),
                TopicLabel = b.Topic,
                ChunkType = b.Type,
                StartPosition = b.Start,
                EndPosition = Math.Min(b.End, text.Length)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic chunking failed, falling back to simple chunking");
            return SimpleChunk(text, options);
        }
    }

    /// <summary>
    /// Parse Claude's JSON response into chunk boundaries
    /// </summary>
    private List<ChunkBoundary> ParseChunkBoundaries(JsonDocument json, int textLength)
    {
        var boundaries = new List<ChunkBoundary>();

        try
        {
            var root = json.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Expected JSON array from Claude");
                return boundaries;
            }

            foreach (var element in root.EnumerateArray())
            {
                var start = element.GetProperty("start").GetInt32();
                var end = element.GetProperty("end").GetInt32();
                var topic = element.TryGetProperty("topic", out var topicProp)
                    ? topicProp.GetString() ?? ""
                    : "";
                var type = element.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString() ?? "mixed"
                    : "mixed";

                // Validate boundaries
                if (start >= 0 && end > start && start < textLength)
                {
                    boundaries.Add(new ChunkBoundary
                    {
                        Start = start,
                        End = Math.Min(end, textLength),
                        Topic = topic.Length > 200 ? topic.Substring(0, 200) : topic,
                        Type = type
                    });
                }
            }

            // Sort by start position
            boundaries = boundaries.OrderBy(b => b.Start).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing chunk boundaries from Claude response");
        }

        return boundaries;
    }

    /// <summary>
    /// Fallback: Simple rule-based chunking by sentences
    /// </summary>
    private List<ChunkPreview> SimpleChunk(string text, ChunkingOptions options)
    {
        var chunks = new List<ChunkPreview>();
        var sentences = SplitIntoSentences(text);

        var currentChunk = new StringBuilder();
        var currentStart = 0;
        var position = 0;
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            // Check if adding this sentence would exceed max size
            if (currentChunk.Length + sentence.Length > options.MaxChunkSize
                && currentChunk.Length >= options.MinChunkSize)
            {
                // Save current chunk
                chunks.Add(new ChunkPreview
                {
                    Index = chunkIndex++,
                    Content = currentChunk.ToString().Trim(),
                    StartPosition = currentStart,
                    EndPosition = position,
                    ChunkType = "mixed"
                });

                // Start new chunk with overlap
                var overlapText = "";
                if (options.ChunkOverlap > 0 && chunks.Count > 0)
                {
                    var lastChunk = chunks[^1].Content;
                    if (lastChunk.Length > options.ChunkOverlap)
                    {
                        overlapText = lastChunk.Substring(lastChunk.Length - options.ChunkOverlap);
                    }
                }

                currentStart = position - overlapText.Length;
                currentChunk.Clear();
                if (!string.IsNullOrEmpty(overlapText))
                {
                    currentChunk.Append(overlapText);
                }
            }

            currentChunk.Append(sentence);
            position += sentence.Length;
        }

        // Add final chunk if it meets minimum size
        if (currentChunk.Length >= options.MinChunkSize || chunks.Count == 0)
        {
            chunks.Add(new ChunkPreview
            {
                Index = chunkIndex,
                Content = currentChunk.ToString().Trim(),
                StartPosition = currentStart,
                EndPosition = text.Length,
                ChunkType = "mixed"
            });
        }
        else if (chunks.Count > 0 && currentChunk.Length > 0)
        {
            // Append small remainder to last chunk
            var lastChunk = chunks[^1];
            chunks[^1] = lastChunk with
            {
                Content = lastChunk.Content + " " + currentChunk.ToString().Trim(),
                EndPosition = text.Length
            };
        }

        return chunks;
    }

    /// <summary>
    /// Split text into sentences
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        // Simple sentence splitting by common terminators
        var pattern = @"(?<=[.!?])\s+(?=[A-ZÄÖÜ])";
        var sentences = Regex.Split(text, pattern);

        var result = new List<string>();
        foreach (var sentence in sentences)
        {
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                result.Add(sentence.Trim() + " ");
            }
        }

        // If no sentences found (e.g., no proper punctuation), split by lines
        if (result.Count <= 1 && text.Contains('\n'))
        {
            result = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim() + "\n")
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
        }

        return result;
    }

    /// <summary>
    /// Get API key for a user or fall back to system key
    /// </summary>
    private async Task<string?> GetAnthropicApiKeyAsync(int? userId)
    {
        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && !string.IsNullOrEmpty(user.AnthropicApiKey))
            {
                return _encryptionHelper.Decrypt(user.AnthropicApiKey);
            }
        }
        return null; // AnthropicClient will use environment variable
    }

    private class ChunkBoundary
    {
        public int Start { get; set; }
        public int End { get; set; }
        public string Topic { get; set; } = "";
        public string Type { get; set; } = "mixed";
    }
}

/// <summary>
/// Interface for chunking service
/// </summary>
public interface IChunkingService
{
    Task<List<int>> ChunkDocumentAsync(int documentId, ChunkingOptions? options = null);
    Task<List<int>> ReChunkDocumentAsync(int documentId, ChunkingOptions? options = null);
    Task<List<DocumentChunk>> GetDocumentChunksAsync(int documentId);
    Task<List<ChunkPreview>> PreviewChunksAsync(string text, ChunkingOptions? options = null, int? userId = null);
    Task<ChunkingBatchResult> ChunkDocumentsBatchAsync(IEnumerable<int> documentIds, ChunkingOptions? options = null);
}

/// <summary>
/// Configuration for chunking behavior
/// </summary>
public class ChunkingOptions
{
    /// <summary>
    /// Minimum chunk size in characters (default: 200)
    /// </summary>
    public int MinChunkSize { get; set; } = 200;

    /// <summary>
    /// Maximum chunk size in characters (default: 2000)
    /// </summary>
    public int MaxChunkSize { get; set; } = 2000;

    /// <summary>
    /// Target chunk size (soft limit, AI will aim for this)
    /// </summary>
    public int TargetChunkSize { get; set; } = 1000;

    /// <summary>
    /// Overlap between chunks in characters (for context continuity)
    /// </summary>
    public int ChunkOverlap { get; set; } = 100;

    /// <summary>
    /// Use AI for semantic boundary detection (vs. simple splitting)
    /// </summary>
    public bool UseSemanticChunking { get; set; } = true;

    /// <summary>
    /// Generate summaries for each chunk
    /// </summary>
    public bool GenerateChunkSummaries { get; set; } = false;

    /// <summary>
    /// Generate topic labels for each chunk
    /// </summary>
    public bool GenerateTopicLabels { get; set; } = true;

    /// <summary>
    /// Also generate embeddings immediately after chunking
    /// </summary>
    public bool GenerateEmbeddings { get; set; } = true;

    public static ChunkingOptions Default => new();

    public static ChunkingOptions Fast => new()
    {
        UseSemanticChunking = false,
        GenerateChunkSummaries = false,
        GenerateTopicLabels = false,
        GenerateEmbeddings = true
    };

    public static ChunkingOptions Full => new()
    {
        UseSemanticChunking = true,
        GenerateChunkSummaries = true,
        GenerateTopicLabels = true,
        GenerateEmbeddings = true,
        ChunkOverlap = 150
    };
}

/// <summary>
/// Preview of a chunk before saving
/// </summary>
public record ChunkPreview
{
    public int Index { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? TopicLabel { get; init; }
    public string? Summary { get; init; }
    public string? ChunkType { get; init; }
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
}

/// <summary>
/// Result of batch chunking operation
/// </summary>
public class ChunkingBatchResult
{
    public int TotalDocuments { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalChunksCreated { get; set; }
    public List<string> Errors { get; set; } = new();
}

using System.Text;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.Embedding;

public partial class EmbeddingService
{
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

            var textToEmbed = BuildChunkEmbeddingText(chunk);
            if (string.IsNullOrWhiteSpace(textToEmbed))
            {
                _logger.LogWarning("No text available for chunk {ChunkId} embedding", chunkId);
                return false;
            }

            var embedding = await GenerateEmbeddingAsync(textToEmbed, userId ?? chunk.UserId);
            if (embedding == null) return false;

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
        sb.AppendLine($"Document: {chunk.Document.FileName}");

        if (!string.IsNullOrEmpty(chunk.Document.Subject))
            sb.AppendLine($"Subject: {chunk.Document.Subject}");

        if (!string.IsNullOrEmpty(chunk.Document.Category))
            sb.AppendLine($"Category: {chunk.Document.Category}");

        if (!string.IsNullOrEmpty(chunk.TopicLabel))
            sb.AppendLine($"Topic: {chunk.TopicLabel}");

        if (!string.IsNullOrEmpty(chunk.ChunkType) && chunk.ChunkType != "mixed")
            sb.AppendLine($"Type: {chunk.ChunkType}");

        sb.AppendLine();
        sb.Append(chunk.Content);

        return sb.ToString();
    }
}

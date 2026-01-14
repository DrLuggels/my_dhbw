using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.Embedding;

public partial class EmbeddingService
{
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
                textToEmbed += $" - {item.Subtopic}";
            if (!string.IsNullOrEmpty(item.Notes))
                textToEmbed += $". {item.Notes}";

            var embedding = await GenerateEmbeddingAsync(textToEmbed, userId ?? item.UserId);
            if (embedding == null) return false;

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
}

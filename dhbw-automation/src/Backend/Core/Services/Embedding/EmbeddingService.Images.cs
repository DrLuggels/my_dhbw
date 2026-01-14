using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.Embedding;

public partial class EmbeddingService
{
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

            if (string.IsNullOrWhiteSpace(image.GeminiDescription))
            {
                _logger.LogWarning("Image {ImageId} has no description for embedding", imageId);
                return false;
            }

            var textToEmbed = $"Image from {image.Document.FileName}, page {image.PageNumber}: {image.GeminiDescription}";
            if (!string.IsNullOrEmpty(image.ExtractedText))
                textToEmbed += $"\nExtracted text: {image.ExtractedText}";

            var embedding = await GenerateEmbeddingAsync(textToEmbed, userId ?? image.Document.UserId);
            if (embedding == null) return false;

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
}

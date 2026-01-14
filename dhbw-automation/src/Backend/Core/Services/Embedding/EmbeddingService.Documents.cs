using System.Text;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.Embedding;

public partial class EmbeddingService
{
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

            var textToEmbed = BuildDocumentEmbeddingText(document);
            if (string.IsNullOrWhiteSpace(textToEmbed))
            {
                _logger.LogWarning("No text available for document {DocumentId} embedding", documentId);
                return false;
            }

            var embedding = await GenerateEmbeddingAsync(textToEmbed, userId ?? document.UserId);
            if (embedding == null) return false;

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

            var existingEmbedding = await _context.QdrantEmbeddings
                .FirstOrDefaultAsync(e => e.EntityType == KnowledgeEntityTypes.Document && e.EntityId == documentId);

            if (existingEmbedding != null)
            {
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
}

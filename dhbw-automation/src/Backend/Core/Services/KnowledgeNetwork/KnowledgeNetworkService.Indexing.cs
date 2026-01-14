using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.KnowledgeNetwork;

public partial class KnowledgeNetworkService
{
    /// <summary>
    /// Index all existing content for a user (create embeddings)
    /// </summary>
    public async Task<IndexingResult> IndexAllUserContentAsync(int userId)
    {
        var result = new IndexingResult();

        try
        {
            await ProcessDocumentsAsync(userId, result);
            await ProcessExercisesAsync(result);
            await ProcessKnowledgeItemsAsync(userId, result);

            result.TotalProcessed = result.DocumentsProcessed +
                                    result.ExercisesProcessed +
                                    result.KnowledgeItemsProcessed;

            _logger.LogInformation(
                "Indexing completed for user {UserId}: {Total} items processed ({Docs} docs, {Ex} exercises, {KI} knowledge items), {Errors} errors",
                userId, result.TotalProcessed, result.DocumentsProcessed,
                result.ExercisesProcessed, result.KnowledgeItemsProcessed, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing content for user {UserId}", userId);
            throw;
        }
    }

    private async Task ProcessDocumentsAsync(int userId, IndexingResult result)
    {
        var documents = await _context.Documents
            .Where(d => d.UserId == userId && !d.HasEmbedding)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} documents for user {UserId}", documents.Count, userId);

        foreach (var doc in documents)
        {
            try
            {
                var success = await _embeddingService.ProcessDocumentEmbeddingAsync(doc.Id, userId);
                if (success)
                    result.DocumentsProcessed++;
                else
                    result.Errors.Add($"Failed to process document {doc.Id} ({doc.FileName})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId}", doc.Id);
                result.Errors.Add($"Error processing document {doc.Id}: {ex.Message}");
            }
        }
    }

    private async Task ProcessExercisesAsync(IndexingResult result)
    {
        var exercises = await _context.JavaDocsExercises
            .Where(e => !e.HasEmbedding)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} exercises without embeddings", exercises.Count);

        foreach (var exercise in exercises)
        {
            try
            {
                var success = await _embeddingService.ProcessExerciseEmbeddingAsync(exercise.Id);
                if (success)
                    result.ExercisesProcessed++;
                else
                    result.Errors.Add($"Failed to process exercise {exercise.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing exercise {ExerciseId}", exercise.Id);
                result.Errors.Add($"Error processing exercise {exercise.Id}: {ex.Message}");
            }
        }
    }

    private async Task ProcessKnowledgeItemsAsync(int userId, IndexingResult result)
    {
        var knowledgeItems = await _context.KnowledgeBaseItems
            .Where(k => k.UserId == userId && !k.HasEmbedding)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} knowledge items for user {UserId}",
            knowledgeItems.Count, userId);

        foreach (var item in knowledgeItems)
        {
            try
            {
                var success = await _embeddingService.ProcessKnowledgeItemEmbeddingAsync(item.Id, userId);
                if (success)
                    result.KnowledgeItemsProcessed++;
                else
                    result.Errors.Add($"Failed to process knowledge item {item.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing knowledge item {ItemId}", item.Id);
                result.Errors.Add($"Error processing knowledge item {item.Id}: {ex.Message}");
            }
        }
    }
}

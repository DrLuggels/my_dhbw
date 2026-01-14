using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.Embedding;

public partial class EmbeddingService
{
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
                textToEmbed += $" - {exercise.Subtopic}";
            if (!string.IsNullOrEmpty(exercise.ParsedContent))
                textToEmbed += $"\n\n{exercise.ParsedContent}";

            var embedding = await GenerateEmbeddingAsync(textToEmbed);
            if (embedding == null) return false;

            var pointId = await _qdrantService.UpsertEmbeddingAsync(
                QdrantCollections.Exercises,
                embedding,
                KnowledgeEntityTypes.JavaDocsExercise,
                exerciseId,
                null,
                new Dictionary<string, string>
                {
                    ["title"] = exercise.Title,
                    ["topic"] = exercise.Topic,
                    ["difficulty"] = exercise.Difficulty ?? "medium"
                }
            );

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

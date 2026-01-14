using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.KnowledgeNetwork;

public partial class KnowledgeNetworkService
{
    private async Task<EntityInfo?> GetEntityInfoAsync(string entityType, int entityId)
    {
        return entityType switch
        {
            KnowledgeEntityTypes.Document => await GetDocumentInfoAsync(entityId),
            KnowledgeEntityTypes.DocumentChunk => await GetDocumentChunkInfoAsync(entityId),
            KnowledgeEntityTypes.KnowledgeItem => await GetKnowledgeItemInfoAsync(entityId),
            KnowledgeEntityTypes.JavaDocsExercise => await GetExerciseInfoAsync(entityId),
            KnowledgeEntityTypes.Image => await GetImageInfoAsync(entityId),
            KnowledgeEntityTypes.MoodleResource => await GetMoodleResourceInfoAsync(entityId),
            _ => null
        };
    }

    private async Task<EntityInfo?> GetDocumentInfoAsync(int id)
    {
        var doc = await _context.Documents.FindAsync(id);
        return doc == null ? null : new EntityInfo
        {
            Title = doc.FileName,
            Description = doc.Summary ?? doc.Subject
        };
    }

    private async Task<EntityInfo?> GetDocumentChunkInfoAsync(int id)
    {
        var chunk = await _context.DocumentChunks
            .Include(c => c.Document)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chunk == null) return null;

        var title = chunk.TopicLabel ?? chunk.SectionHeading ??
                    $"Chunk {chunk.ChunkIndex + 1} von {chunk.Document?.FileName ?? "Dokument"}";
        var description = chunk.Summary ??
                          (chunk.Content.Length > 200 ? chunk.Content.Substring(0, 200) + "..." : chunk.Content);

        return new EntityInfo { Title = title, Description = description };
    }

    private async Task<EntityInfo?> GetKnowledgeItemInfoAsync(int id)
    {
        var item = await _context.KnowledgeBaseItems.FindAsync(id);
        return item == null ? null : new EntityInfo
        {
            Title = $"{item.Subject}: {item.Topic}",
            Description = item.Notes
        };
    }

    private async Task<EntityInfo?> GetExerciseInfoAsync(int id)
    {
        var exercise = await _context.JavaDocsExercises.FindAsync(id);
        return exercise == null ? null : new EntityInfo
        {
            Title = exercise.Title,
            Description = $"{exercise.Topic} - {exercise.Difficulty}"
        };
    }

    private async Task<EntityInfo?> GetImageInfoAsync(int id)
    {
        var image = await _context.DocumentImages
            .Include(i => i.Document)
            .FirstOrDefaultAsync(i => i.Id == id);

        return image == null ? null : new EntityInfo
        {
            Title = $"Image from {image.Document.FileName} (Page {image.PageNumber})",
            Description = image.GeminiDescription
        };
    }

    private async Task<EntityInfo?> GetMoodleResourceInfoAsync(int id)
    {
        var resource = await _context.MoodleResources.FindAsync(id);
        return resource == null ? null : new EntityInfo
        {
            Title = resource.Title,
            Description = resource.CourseName
        };
    }
}

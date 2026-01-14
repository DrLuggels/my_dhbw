using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.LearningAnalytics;

public partial class LearningAnalyticsService
{
    public async Task AnalyzeDocumentErrorsAsync(int documentId)
    {
        try
        {
            _logger.LogInformation($"Analyzing errors for document {documentId}");

            var document = await _context.Documents.FindAsync(documentId);
            if (document == null || string.IsNullOrEmpty(document.DetectedErrors))
            {
                _logger.LogWarning($"Document {documentId} not found or has no errors");
                return;
            }

            var errors = JsonSerializer.Deserialize<List<DetectedError>>(document.DetectedErrors);
            if (errors == null || errors.Count == 0) return;

            foreach (var error in errors)
            {
                var deficit = await _context.LearningDeficits
                    .FirstOrDefaultAsync(d =>
                        d.UserId == document.UserId &&
                        d.Subject == error.Subject &&
                        d.Topic == error.Topic &&
                        d.ErrorType == error.ErrorType);

                if (deficit == null)
                {
                    deficit = new LearningDeficit
                    {
                        UserId = document.UserId,
                        Subject = error.Subject,
                        Topic = error.Topic,
                        ErrorType = error.ErrorType,
                        ErrorDescription = error.Explanation,
                        OccurrenceCount = 1,
                        FirstOccurrence = DateTime.UtcNow,
                        LastOccurrence = DateTime.UtcNow,
                        Severity = error.Severity,
                        NeedsTutoring = true,
                        RelatedDocumentIds = JsonSerializer.Serialize(new[] { documentId })
                    };
                    _context.LearningDeficits.Add(deficit);
                    _logger.LogInformation($"Created new learning deficit (needs tutoring): {error.Subject} - {error.Topic}");
                }
                else
                {
                    deficit.OccurrenceCount++;
                    deficit.LastOccurrence = DateTime.UtcNow;

                    if (deficit.OccurrenceCount >= 1) deficit.NeedsTutoring = true;

                    if (deficit.OccurrenceCount >= 3)
                    {
                        deficit.Severity = "high";
                        _logger.LogWarning($"Deficit escalated to HIGH: {error.Subject} - {error.Topic} (occurred {deficit.OccurrenceCount} times)");
                    }
                    else if (deficit.OccurrenceCount >= 2)
                    {
                        deficit.Severity = "medium";
                    }

                    var docIds = JsonSerializer.Deserialize<List<int>>(deficit.RelatedDocumentIds) ?? new List<int>();
                    if (!docIds.Contains(documentId))
                    {
                        docIds.Add(documentId);
                        deficit.RelatedDocumentIds = JsonSerializer.Serialize(docIds);
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Processed {errors.Count} errors from document {documentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing document errors for document {documentId}");
            throw;
        }
    }

    public async Task<List<LearningDeficit>> GetActiveDeficitsAsync(int userId)
    {
        return await _context.LearningDeficits
            .Where(d => d.UserId == userId && d.ResolvedAt == null)
            .OrderByDescending(d => d.Severity)
            .ThenByDescending(d => d.OccurrenceCount)
            .ThenByDescending(d => d.LastOccurrence)
            .ToListAsync();
    }

    public async Task<bool> ShouldScheduleTutoringAsync(int userId, string subject)
    {
        var highPriorityDeficits = await _context.LearningDeficits
            .Where(d => d.UserId == userId &&
                       d.Subject == subject &&
                       d.Severity == "high" &&
                       d.ResolvedAt == null)
            .CountAsync();

        return highPriorityDeficits > 0;
    }
}

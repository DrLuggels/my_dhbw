using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DHBWAutomation.Backend.Core.Services;

public class ValidationService : IValidationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(AppDbContext context, ILogger<ValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<StagedEntity>> StageEntitiesAsync(DocumentIntent intent, int userId, int? documentId = null)
    {
        var stagedEntities = new List<StagedEntity>();

        try
        {
            // 1. Stage Meeting (wenn vorhanden)
            if (intent.Meeting != null)
            {
                var staged = await StageMeetingAsync(intent.Meeting, intent.Questions, userId, documentId);
                stagedEntities.Add(staged);
            }

            // 2. Stage TODOs (wenn vorhanden)
            for (int i = 0; i < intent.Todos.Count; i++)
            {
                var todo = intent.Todos[i];
                var todoQuestions = intent.Questions.Where(q => q.EntityIndex == i && q.FieldName.StartsWith("todo")).ToList();
                var staged = await StageTodoAsync(todo, todoQuestions, userId, documentId, i);
                stagedEntities.Add(staged);
            }

            // 3. Stage Project (wenn vorhanden)
            if (intent.Project != null)
            {
                var projectQuestions = intent.Questions.Where(q => q.FieldName.StartsWith("project")).ToList();
                var staged = await StageProjectAsync(intent.Project, projectQuestions, userId, documentId);
                stagedEntities.Add(staged);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Staged {stagedEntities.Count} entities for user {userId} from document {documentId}");
            return stagedEntities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error staging entities for user {userId}");
            throw;
        }
    }

    private async Task<StagedEntity> StageMeetingAsync(
        ExtractedMeeting meeting,
        List<ExtractedQuestion> questions,
        int userId,
        int? documentId)
    {
        var staged = new StagedEntity
        {
            UserId = userId,
            SourceDocumentId = documentId,
            EntityType = "meeting",
            EntityData = JsonSerializer.Serialize(meeting),
            ConfidenceScore = meeting.ConfidenceScore,
            Status = meeting.ConfidenceScore < 90 ? "pending_review" : "confirmed",
            Priority = meeting.ConfidenceScore < 70 ? "high" : meeting.ConfidenceScore < 90 ? "medium" : "low",
            CreatedAt = DateTime.UtcNow
        };

        _context.StagedEntities.Add(staged);
        await _context.SaveChangesAsync(); // Save to get ID

        // Add questions
        var meetingQuestions = questions.Where(q => q.FieldName.StartsWith("meeting")).ToList();
        foreach (var q in meetingQuestions)
        {
            var aiQuestion = new AIQuestion
            {
                StagedEntityId = staged.Id,
                FieldName = q.FieldName,
                QuestionText = q.QuestionText,
                SuggestedAnswers = JsonSerializer.Serialize(q.SuggestedAnswers),
                Priority = q.Priority,
                AnswerType = q.AnswerType,
                CreatedAt = DateTime.UtcNow
            };
            _context.AIQuestions.Add(aiQuestion);
        }

        return staged;
    }

    private async Task<StagedEntity> StageTodoAsync(
        ExtractedTodo todo,
        List<ExtractedQuestion> questions,
        int userId,
        int? documentId,
        int index)
    {
        var staged = new StagedEntity
        {
            UserId = userId,
            SourceDocumentId = documentId,
            EntityType = "todo",
            EntityData = JsonSerializer.Serialize(todo),
            ConfidenceScore = todo.ConfidenceScore,
            Status = todo.ConfidenceScore < 90 ? "pending_review" : "confirmed",
            Priority = todo.ConfidenceScore < 70 ? "high" : todo.ConfidenceScore < 90 ? "medium" : "low",
            CreatedAt = DateTime.UtcNow
        };

        _context.StagedEntities.Add(staged);
        await _context.SaveChangesAsync(); // Save to get ID

        // Add questions
        foreach (var q in questions)
        {
            var aiQuestion = new AIQuestion
            {
                StagedEntityId = staged.Id,
                FieldName = q.FieldName,
                QuestionText = q.QuestionText,
                SuggestedAnswers = JsonSerializer.Serialize(q.SuggestedAnswers),
                Priority = q.Priority,
                AnswerType = q.AnswerType,
                CreatedAt = DateTime.UtcNow
            };
            _context.AIQuestions.Add(aiQuestion);
        }

        return staged;
    }

    private async Task<StagedEntity> StageProjectAsync(
        ExtractedProject project,
        List<ExtractedQuestion> questions,
        int userId,
        int? documentId)
    {
        var staged = new StagedEntity
        {
            UserId = userId,
            SourceDocumentId = documentId,
            EntityType = "project",
            EntityData = JsonSerializer.Serialize(project),
            ConfidenceScore = project.ConfidenceScore,
            Status = project.ConfidenceScore < 90 ? "pending_review" : "confirmed",
            Priority = project.ConfidenceScore < 70 ? "high" : project.ConfidenceScore < 90 ? "medium" : "low",
            CreatedAt = DateTime.UtcNow
        };

        _context.StagedEntities.Add(staged);
        await _context.SaveChangesAsync(); // Save to get ID

        // Add questions
        foreach (var q in questions)
        {
            var aiQuestion = new AIQuestion
            {
                StagedEntityId = staged.Id,
                FieldName = q.FieldName,
                QuestionText = q.QuestionText,
                SuggestedAnswers = JsonSerializer.Serialize(q.SuggestedAnswers),
                Priority = q.Priority,
                AnswerType = q.AnswerType,
                CreatedAt = DateTime.UtcNow
            };
            _context.AIQuestions.Add(aiQuestion);
        }

        return staged;
    }

    public async Task<List<StagedEntity>> GetPendingStagedEntitiesAsync(int userId, string? status = null)
    {
        var query = _context.StagedEntities
            .AsNoTracking() // Prevent loading navigation properties that cause circular references
            .Include(s => s.Questions)
            // Removed .Include(s => s.SourceDocument) to prevent circular reference (User -> StagedEntities -> Document -> User)
            .Where(s => s.UserId == userId && !s.IsPromoted);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }

        return await query
            .OrderByDescending(s => s.Priority == "urgent" ? 4 : s.Priority == "high" ? 3 : s.Priority == "medium" ? 2 : 1)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AnswerQuestionsAsync(int stagedEntityId, int userId, Dictionary<string, string> answers)
    {
        try
        {
            var staged = await _context.StagedEntities
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == stagedEntityId && s.UserId == userId);

            if (staged == null)
            {
                _logger.LogWarning($"Staged entity {stagedEntityId} not found for user {userId}");
                return false;
            }

            foreach (var (fieldName, answer) in answers)
            {
                var question = staged.Questions.FirstOrDefault(q => q.FieldName == fieldName);
                if (question != null)
                {
                    question.UserAnswer = answer;
                    question.IsAnswered = true;
                    question.AnsweredAt = DateTime.UtcNow;
                }
            }

            // Update entity data with answers
            await UpdateEntityDataWithAnswersAsync(staged);

            // Check if all critical/high questions are answered
            var unansweredCritical = staged.Questions.Count(q => !q.IsAnswered && (q.Priority == "critical" || q.Priority == "high"));
            if (unansweredCritical == 0)
            {
                staged.Status = "confirmed";
                staged.ReviewedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Answered {answers.Count} questions for staged entity {stagedEntityId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error answering questions for staged entity {stagedEntityId}");
            return false;
        }
    }

    private async Task UpdateEntityDataWithAnswersAsync(StagedEntity staged)
    {
        // Parse entity data
        var jsonDoc = JsonDocument.Parse(staged.EntityData);
        var dataDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(staged.EntityData) ?? new();

        // Update with user answers
        foreach (var question in staged.Questions.Where(q => q.IsAnswered))
        {
            var fieldPath = question.FieldName.Split('.');
            if (fieldPath.Length >= 2)
            {
                var fieldName = fieldPath[1];

                // Convert answer based on answer type
                object? value = question.AnswerType switch
                {
                    "date" => DateTime.TryParse(question.UserAnswer, out var date) ? date : (DateTime?)null,
                    "time" => question.UserAnswer,
                    "datetime" => DateTime.TryParse(question.UserAnswer, out var dt) ? dt : (DateTime?)null,
                    "number" => int.TryParse(question.UserAnswer, out var num) ? num : (int?)null,
                    _ => question.UserAnswer
                };

                if (value != null)
                {
                    dataDict[fieldName] = JsonSerializer.SerializeToElement(value);
                }
            }
        }

        // Update staged entity data
        staged.EntityData = JsonSerializer.Serialize(dataDict);
    }

    public async Task<int?> ConfirmAndPromoteAsync(int stagedEntityId, int userId, string? userNotes = null)
    {
        try
        {
            var staged = await _context.StagedEntities
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == stagedEntityId && s.UserId == userId);

            if (staged == null)
            {
                _logger.LogWarning($"Staged entity {stagedEntityId} not found for user {userId}");
                return null;
            }

            // Check if critical questions are answered
            var unansweredCritical = staged.Questions.Count(q => !q.IsAnswered && q.Priority == "critical");
            if (unansweredCritical > 0)
            {
                _logger.LogWarning($"Cannot promote staged entity {stagedEntityId}: {unansweredCritical} critical questions unanswered");
                return null;
            }

            int? promotedEntityId = null;

            // Promote to production DB based on entity type
            switch (staged.EntityType)
            {
                case "todo":
                    promotedEntityId = await PromoteTodoAsync(staged, userId);
                    break;
                case "meeting":
                    promotedEntityId = await PromoteMeetingAsync(staged, userId);
                    break;
                case "project":
                    promotedEntityId = await PromoteProjectAsync(staged, userId);
                    break;
                default:
                    _logger.LogWarning($"Unknown entity type: {staged.EntityType}");
                    return null;
            }

            // Mark as promoted
            staged.IsPromoted = true;
            staged.PromotedEntityId = promotedEntityId;
            staged.PromotedAt = DateTime.UtcNow;
            staged.Status = "confirmed";
            staged.ReviewedAt = DateTime.UtcNow;
            staged.UserNotes = userNotes;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Promoted staged entity {stagedEntityId} to {staged.EntityType} {promotedEntityId}");
            return promotedEntityId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error promoting staged entity {stagedEntityId}");
            return null;
        }
    }

    private async Task<int?> PromoteTodoAsync(StagedEntity staged, int userId)
    {
        var extractedTodo = JsonSerializer.Deserialize<ExtractedTodo>(staged.EntityData);
        if (extractedTodo == null) return null;

        var todo = new Todo
        {
            UserId = userId,
            Title = extractedTodo.Title,
            Description = extractedTodo.Description,
            Priority = extractedTodo.Priority,
            Category = extractedTodo.Category,
            DueDate = extractedTodo.SuggestedDeadline,
            Status = "pending",
            RelatedDocumentId = staged.SourceDocumentId,
            CreatedAt = DateTime.UtcNow,
            AiSuggestion = $"Automatisch extrahiert (Confidence: {extractedTodo.ConfidenceScore}%)"
        };

        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();

        return todo.Id;
    }

    private async Task<int?> PromoteMeetingAsync(StagedEntity staged, int userId)
    {
        var extractedMeeting = JsonSerializer.Deserialize<ExtractedMeeting>(staged.EntityData);
        if (extractedMeeting == null || extractedMeeting.SuggestedDate == null) return null;

        var calendarEvent = new CalendarEvent
        {
            UserId = userId,
            Title = $"Meeting mit {extractedMeeting.PersonName}",
            Description = extractedMeeting.Purpose,
            StartTime = extractedMeeting.SuggestedDate.Value,
            EndTime = extractedMeeting.SuggestedDate.Value.AddMinutes(extractedMeeting.EstimatedDurationMinutes),
            Location = "",
            Source = "ai_extracted",
            CreatedAt = DateTime.UtcNow
        };

        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        return calendarEvent.Id;
    }

    private async Task<int?> PromoteProjectAsync(StagedEntity staged, int userId)
    {
        var extractedProject = JsonSerializer.Deserialize<ExtractedProject>(staged.EntityData);
        if (extractedProject == null) return null;

        var project = new Project
        {
            UserId = userId,
            Name = extractedProject.Name,
            Description = extractedProject.Description,
            Priority = extractedProject.EstimatedPriority,
            Status = "planning",
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return project.Id;
    }

    public async Task<bool> RejectStagedEntityAsync(int stagedEntityId, int userId, string? reason = null)
    {
        try
        {
            var staged = await _context.StagedEntities
                .FirstOrDefaultAsync(s => s.Id == stagedEntityId && s.UserId == userId);

            if (staged == null)
            {
                _logger.LogWarning($"Staged entity {stagedEntityId} not found for user {userId}");
                return false;
            }

            staged.Status = "rejected";
            staged.ReviewedAt = DateTime.UtcNow;
            staged.UserNotes = reason;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Rejected staged entity {stagedEntityId}: {reason}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error rejecting staged entity {stagedEntityId}");
            return false;
        }
    }

    public async Task<bool> ModifyStagedEntityAsync(int stagedEntityId, int userId, string modifiedData)
    {
        try
        {
            var staged = await _context.StagedEntities
                .FirstOrDefaultAsync(s => s.Id == stagedEntityId && s.UserId == userId);

            if (staged == null)
            {
                _logger.LogWarning($"Staged entity {stagedEntityId} not found for user {userId}");
                return false;
            }

            staged.EntityData = modifiedData;
            staged.Status = "modified";
            staged.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Modified staged entity {stagedEntityId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error modifying staged entity {stagedEntityId}");
            return false;
        }
    }

    public async Task<StagingStatistics> GetStagingStatisticsAsync(int userId, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var entities = await _context.StagedEntities
            .Include(s => s.Questions)
            .Where(s => s.UserId == userId && s.CreatedAt >= since)
            .ToListAsync();

        var questions = await _context.AIQuestions
            .Where(q => q.StagedEntity.UserId == userId && q.CreatedAt >= since)
            .ToListAsync();

        return new StagingStatistics
        {
            TotalStaged = entities.Count,
            TotalConfirmed = entities.Count(e => e.Status == "confirmed" && e.IsPromoted),
            TotalRejected = entities.Count(e => e.Status == "rejected"),
            TotalModified = entities.Count(e => e.Status == "modified"),
            AverageConfidenceScore = entities.Any() ? entities.Average(e => e.ConfidenceScore) : 0,
            TotalQuestions = questions.Count,
            AverageQuestionsPerEntity = entities.Any() ? (double)questions.Count / entities.Count : 0,
            QuestionsByPriority = questions.GroupBy(q => q.Priority).ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

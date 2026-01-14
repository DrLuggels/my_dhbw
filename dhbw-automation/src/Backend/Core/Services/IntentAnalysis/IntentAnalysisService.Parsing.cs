using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.IntentAnalysis;

public partial class IntentAnalysisService
{
    private DocumentIntent ParseIntentFromJsonDocument(JsonDocument doc)
    {
        try
        {
            var root = doc.RootElement;

            var intent = new DocumentIntent
            {
                PrimaryIntent = TryGetString(root, "primaryIntent") ?? "unknown",
                ActionRequired = TryGetString(root, "actionRequired") ?? "none",
                Urgency = TryGetString(root, "urgency") ?? "low",
                ConfidenceScore = TryGetInt32(root, "confidenceScore") ?? 100
            };

            ParseSecondaryIntents(root, intent);
            ParseMeetings(root, intent);
            ParseTodos(root, intent);
            ParseProject(root, intent);
            ParseQuestions(root, intent);
            ParseErrors(root, intent);
            ParseLearningInfo(root, intent);

            return intent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON from Claude response");
            return new DocumentIntent { PrimaryIntent = "unknown", ActionRequired = "none" };
        }
    }

    private void ParseSecondaryIntents(JsonElement root, DocumentIntent intent)
    {
        if (root.TryGetProperty("secondaryIntents", out var secondaryIntents) && secondaryIntents.ValueKind == JsonValueKind.Array)
        {
            intent.SecondaryIntents = secondaryIntents.EnumerateArray()
                .Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList();
        }
    }

    private void ParseMeetings(JsonElement root, DocumentIntent intent)
    {
        if (root.TryGetProperty("meetings", out var meetings) && meetings.ValueKind == JsonValueKind.Array)
        {
            foreach (var m in meetings.EnumerateArray().Where(m => m.ValueKind == JsonValueKind.Object))
            {
                var meeting = new ExtractedMeeting
                {
                    PersonName = TryGetString(m, "personName") ?? "",
                    Purpose = TryGetString(m, "purpose") ?? "",
                    EstimatedDurationMinutes = TryGetInt32(m, "estimatedDurationMinutes") ?? 60,
                    ConfidenceScore = TryGetInt32(m, "confidenceScore") ?? 100,
                    SuggestedTime = TryGetString(m, "suggestedTime")
                };
                if (DateTime.TryParse(TryGetString(m, "suggestedDate"), out var date))
                    meeting.SuggestedDate = date;
                intent.Meetings.Add(meeting);
            }
        }
        else if (root.TryGetProperty("meeting", out var single) && single.ValueKind == JsonValueKind.Object)
        {
            var meeting = new ExtractedMeeting
            {
                PersonName = TryGetString(single, "personName") ?? "",
                Purpose = TryGetString(single, "purpose") ?? "",
                EstimatedDurationMinutes = TryGetInt32(single, "estimatedDurationMinutes") ?? 60,
                ConfidenceScore = TryGetInt32(single, "confidenceScore") ?? 100,
                SuggestedTime = TryGetString(single, "suggestedTime")
            };
            if (DateTime.TryParse(TryGetString(single, "suggestedDate"), out var date))
                meeting.SuggestedDate = date;
            intent.Meetings.Add(meeting);
        }
    }

    private void ParseTodos(JsonElement root, DocumentIntent intent)
    {
        if (root.TryGetProperty("todos", out var todos) && todos.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in todos.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.Object))
            {
                var todo = new ExtractedTodo
                {
                    Title = TryGetString(t, "title") ?? "",
                    Description = TryGetString(t, "description"),
                    Priority = TryGetString(t, "priority") ?? "medium",
                    Category = TryGetString(t, "category") ?? "general",
                    ConfidenceScore = TryGetInt32(t, "confidenceScore") ?? 100
                };
                if (DateTime.TryParse(TryGetString(t, "suggestedDeadline"), out var deadline))
                    todo.SuggestedDeadline = deadline;
                intent.Todos.Add(todo);
            }
        }
    }

    private void ParseProject(JsonElement root, DocumentIntent intent)
    {
        if (root.TryGetProperty("project", out var project) && project.ValueKind == JsonValueKind.Object)
        {
            intent.Project = new ExtractedProject
            {
                Name = TryGetString(project, "name") ?? "",
                Description = TryGetString(project, "description") ?? "",
                EstimatedPriority = TryGetString(project, "estimatedPriority") ?? "medium",
                ConfidenceScore = TryGetInt32(project, "confidenceScore") ?? 100
            };
            if (project.TryGetProperty("requirements", out var reqs) && reqs.ValueKind == JsonValueKind.Array)
                intent.Project.Requirements = reqs.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList();
            if (project.TryGetProperty("ideas", out var ideas) && ideas.ValueKind == JsonValueKind.Array)
                intent.Project.Ideas = ideas.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList();
        }
    }

    private void ParseQuestions(JsonElement root, DocumentIntent intent)
    {
        if (root.TryGetProperty("questions", out var questions) && questions.ValueKind == JsonValueKind.Array)
        {
            foreach (var q in questions.EnumerateArray().Where(q => q.ValueKind == JsonValueKind.Object))
            {
                var question = new ExtractedQuestion
                {
                    FieldName = TryGetString(q, "fieldName") ?? "",
                    QuestionText = TryGetString(q, "questionText") ?? "",
                    Priority = TryGetString(q, "priority") ?? "medium",
                    AnswerType = TryGetString(q, "answerType") ?? "text",
                    EntityIndex = TryGetInt32(q, "entityIndex")
                };
                if (q.TryGetProperty("suggestedAnswers", out var answers) && answers.ValueKind == JsonValueKind.Array)
                    question.SuggestedAnswers = answers.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList();
                intent.Questions.Add(question);
            }
        }
    }

    private void ParseErrors(JsonElement root, DocumentIntent intent)
    {
        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in errors.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object))
            {
                intent.Errors.Add(new DetectedError
                {
                    ErrorType = TryGetString(e, "errorType") ?? "concept",
                    Subject = TryGetString(e, "subject") ?? "",
                    Topic = TryGetString(e, "topic") ?? "",
                    Original = TryGetString(e, "original") ?? "",
                    Corrected = TryGetString(e, "corrected") ?? "",
                    Explanation = TryGetString(e, "explanation") ?? "",
                    Severity = TryGetString(e, "severity") ?? "low"
                });
            }
        }
    }

    private void ParseLearningInfo(JsonElement root, DocumentIntent intent)
    {
        if (root.TryGetProperty("learningInfo", out var info) && info.ValueKind == JsonValueKind.Object)
        {
            intent.LearningInfo = new LearningContent
            {
                Subject = TryGetString(info, "subject") ?? "",
                Topic = TryGetString(info, "topic") ?? "",
                ComprehensionLevel = TryGetString(info, "comprehensionLevel") ?? "partial",
                NeedsMoreStudy = TryGetBool(info, "needsMoreStudy") ?? false
            };
            if (info.TryGetProperty("keyConcepts", out var concepts) && concepts.ValueKind == JsonValueKind.Array)
                intent.LearningInfo.KeyConcepts = concepts.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList();
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;

    private static int? TryGetInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : null;

    private static bool? TryGetBool(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.True) return true;
            if (prop.ValueKind == JsonValueKind.False) return false;
        }
        return null;
    }
}

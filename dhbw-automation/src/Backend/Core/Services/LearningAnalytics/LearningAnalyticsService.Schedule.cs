using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Services.LearningAnalytics;

public partial class LearningAnalyticsService
{
    public async Task<List<LearningSession>> PlanLearningScheduleAsync(int userId)
    {
        try
        {
            _logger.LogInformation($"Planning learning schedule for user {userId}");

            var sessions = new List<LearningSession>();
            var deficits = await GetActiveDeficitsAsync(userId);

            foreach (var deficit in deficits)
            {
                var session = new LearningSession
                {
                    Subject = deficit.Subject,
                    Topic = deficit.Topic,
                    Start = DateTime.MinValue,
                    End = DateTime.MinValue,
                    PriorityScore = deficit.Severity switch
                    {
                        "high" => 90,
                        "medium" => 70,
                        _ => 50
                    }
                };

                sessions.Add(session);
            }

            _logger.LogInformation($"Planned {sessions.Count} learning sessions");
            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error planning learning schedule");
            return new List<LearningSession>();
        }
    }

    private int EstimateLearningDuration(string subject, string topic, string severity)
    {
        var baseTime = severity switch
        {
            "high" => 120,
            "medium" => 90,
            "low" => 60,
            _ => 60
        };

        if (subject.Contains("Mathematik", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("Programmierung", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("Algorithmen", StringComparison.OrdinalIgnoreCase))
        {
            baseTime += 30;
        }

        return baseTime;
    }
}

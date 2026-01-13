using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;
using Microsoft.EntityFrameworkCore;

// Type aliases
using MoodleCalendarEventModel = DHBWAutomation.Backend.Core.Models.MoodleCalendarEvent;
using MoodleApiCalendarEvent = DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle.MoodleCalendarEvent;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Calendar Events Synchronisation
/// </summary>
public partial class MoodleSyncService
{
    public async Task<MoodleSyncResult> SyncCalendarEventsAsync(int userId)
    {
        var result = new MoodleSyncResult { EntityType = "CalendarEvents" };

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.MoodleSyncEnabled || string.IsNullOrEmpty(user.MoodleToken))
            {
                result.ErrorMessage = "Moodle-Sync nicht aktiviert";
                return result;
            }

            var token = _encryptionHelper.Decrypt(user.MoodleToken);
            _moodleClient.SetToken(token);

            var now = DateTimeOffset.UtcNow;
            var timeStart = now.ToUnixTimeSeconds();
            var timeEnd = now.AddDays(90).ToUnixTimeSeconds();

            var eventsResponse = await _moodleClient.GetCalendarEventsAsync(timeStart, timeEnd);
            var events = eventsResponse.Events ?? new List<MoodleApiCalendarEvent>();

            var upcomingEvents = await _moodleClient.GetUpcomingEventsAsync(100);
            events.AddRange(upcomingEvents);

            events = events.DistinctBy(e => e.Id).ToList();

            var existingEvents = await _context.MoodleCalendarEvents
                .Where(e => e.UserId == userId)
                .ToDictionaryAsync(e => e.MoodleEventId);

            foreach (var moodleEvent in events)
            {
                if (existingEvents.TryGetValue(moodleEvent.Id, out var existing))
                {
                    existing.Name = moodleEvent.Name;
                    existing.Description = moodleEvent.Description;
                    existing.EventType = moodleEvent.Eventtype;
                    existing.ModuleName = moodleEvent.Modulename;
                    existing.TimeStart = DateTimeOffset.FromUnixTimeSeconds(moodleEvent.Timestart).UtcDateTime;
                    existing.TimeDuration = moodleEvent.Timeduration;
                    existing.CourseId = moodleEvent.Courseid > 0 ? moodleEvent.Courseid : null;
                    existing.CourseName = moodleEvent.Course?.Fullname;
                    existing.SyncedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.Updated++;
                }
                else
                {
                    var newEvent = new MoodleCalendarEventModel
                    {
                        UserId = userId,
                        MoodleEventId = moodleEvent.Id,
                        Name = moodleEvent.Name,
                        Description = moodleEvent.Description,
                        EventType = moodleEvent.Eventtype,
                        ModuleName = moodleEvent.Modulename,
                        TimeStart = DateTimeOffset.FromUnixTimeSeconds(moodleEvent.Timestart).UtcDateTime,
                        TimeDuration = moodleEvent.Timeduration,
                        CourseId = moodleEvent.Courseid > 0 ? moodleEvent.Courseid : null,
                        CourseName = moodleEvent.Course?.Fullname,
                        SyncedAt = DateTime.UtcNow
                    };
                    _context.MoodleCalendarEvents.Add(newEvent);
                    result.Added++;
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Calendar events sync completed: {Added} added, {Updated} updated",
                result.Added, result.Updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing calendar events for user {UserId}", userId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

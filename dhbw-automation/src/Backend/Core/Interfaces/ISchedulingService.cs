using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface ISchedulingService
{
    /// <summary>
    /// Finds available time slots in user's calendar considering all constraints
    /// </summary>
    Task<List<TimeSlot>> FindAvailableTimeSlotsAsync(
        int userId,
        DateTime startDate,
        DateTime endDate,
        int durationMinutes,
        SchedulingConstraints constraints
    );

    /// <summary>
    /// Schedules a calendar event in an available slot
    /// </summary>
    Task<CalendarEvent> ScheduleEventAsync(
        int userId,
        string title,
        TimeSlot slot,
        string category
    );

    /// <summary>
    /// Suggests best meeting times for the next 2 weeks
    /// </summary>
    Task<List<TimeSlot>> SuggestMeetingTimesAsync(
        int userId,
        string personName,
        int durationMinutes
    );

    /// <summary>
    /// Plans multiple learning sessions distributed over time
    /// </summary>
    Task<List<LearningSession>> ScheduleLearningSessionsAsync(
        int userId,
        string subject,
        int totalMinutes,
        DateTime deadline
    );
}

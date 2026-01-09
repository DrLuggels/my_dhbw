using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services;

public class SchedulingService : ISchedulingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SchedulingService> _logger;

    public SchedulingService(AppDbContext context, ILogger<SchedulingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TimeSlot>> FindAvailableTimeSlotsAsync(
        int userId,
        DateTime startDate,
        DateTime endDate,
        int durationMinutes,
        SchedulingConstraints constraints)
    {
        try
        {
            _logger.LogInformation($"Finding available time slots for user {userId} from {startDate} to {endDate}");

            // Load all calendar events in the time range
            var existingEvents = await _context.CalendarEvents
                .Where(e => e.UserId == userId &&
                           e.StartTime >= startDate &&
                           e.EndTime <= endDate)
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            var freeSlots = new List<TimeSlot>();

            // Iterate through each day
            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                var daySlots = FindFreeSlotsForDay(currentDate, existingEvents, durationMinutes, constraints);
                freeSlots.AddRange(daySlots);
                currentDate = currentDate.AddDays(1);
            }

            // Sort by quality score (excellent > good > acceptable > poor)
            var sortedSlots = freeSlots.OrderByDescending(s =>
            {
                return s.QualityScore switch
                {
                    "excellent" => 4,
                    "good" => 3,
                    "acceptable" => 2,
                    "poor" => 1,
                    _ => 0
                };
            }).ThenBy(s => s.Start).ToList();

            _logger.LogInformation($"Found {sortedSlots.Count} available time slots");
            return sortedSlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding available time slots");
            return new List<TimeSlot>();
        }
    }

    private List<TimeSlot> FindFreeSlotsForDay(
        DateTime date,
        List<CalendarEvent> allEvents,
        int durationMinutes,
        SchedulingConstraints constraints)
    {
        var slots = new List<TimeSlot>();

        // Get events for this specific day
        var dayEvents = allEvents
            .Where(e => e.StartTime.Date == date.Date)
            .OrderBy(e => e.StartTime)
            .ToList();

        // Check if it's a university day
        var isUniversityDay = constraints.UniversityDays.Contains(date.DayOfWeek);
        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        // Determine start and end times for the day
        var dayStart = date.Date.Add(constraints.EarliestStart.ToTimeSpan());
        var dayEnd = date.Date.Add(constraints.LatestEnd.ToTimeSpan());

        // Add commute buffer for university days
        if (isUniversityDay)
        {
            // Check if there are any university events
            var hasUniEvent = dayEvents.Any(e => e.Subject?.Contains("DHBW", StringComparison.OrdinalIgnoreCase) == true ||
                                                 e.Subject?.Contains("Vorlesung", StringComparison.OrdinalIgnoreCase) == true);
            if (hasUniEvent)
            {
                // Reserve commute time before first uni event and after last uni event
                var firstUniEvent = dayEvents.FirstOrDefault(e => e.Subject?.Contains("DHBW", StringComparison.OrdinalIgnoreCase) == true);
                if (firstUniEvent != null)
                {
                    // Cannot schedule anything within commute time before uni
                    var commuteStart = firstUniEvent.StartTime.AddMinutes(-constraints.CommuteMinutes);
                    if (commuteStart < dayEnd && commuteStart > dayStart)
                    {
                        dayEvents.Add(new CalendarEvent
                        {
                            StartTime = commuteStart,
                            EndTime = firstUniEvent.StartTime,
                            Title = "(Anfahrt)"
                        });
                    }
                }
            }
        }

        // Find gaps between events
        if (dayEvents.Count == 0)
        {
            // Entire day is free
            var freeTime = (int)(dayEnd - dayStart).TotalMinutes;
            if (freeTime >= durationMinutes)
            {
                // Split into reasonable chunks (max 2-3 hours)
                var currentTime = dayStart;
                while ((dayEnd - currentTime).TotalMinutes >= durationMinutes)
                {
                    var slotEnd = currentTime.AddMinutes(Math.Min(durationMinutes, 120)); // Max 2h slots
                    if (slotEnd <= dayEnd)
                    {
                        var slot = new TimeSlot
                        {
                            Start = currentTime,
                            End = slotEnd
                        };
                        slot.QualityScore = ScoreTimeSlot(slot, isWeekend, isUniversityDay);
                        slots.Add(slot);
                    }
                    currentTime = currentTime.AddMinutes(90); // Move by 1.5h for next slot
                }
            }
        }
        else
        {
            // Check gap from day start to first event
            var firstEvent = dayEvents.First();
            if ((firstEvent.StartTime - dayStart).TotalMinutes >= durationMinutes + constraints.BufferBetweenEventsMinutes)
            {
                var slot = new TimeSlot
                {
                    Start = dayStart,
                    End = firstEvent.StartTime.AddMinutes(-constraints.BufferBetweenEventsMinutes)
                };
                if (slot.DurationMinutes >= durationMinutes)
                {
                    slot.QualityScore = ScoreTimeSlot(slot, isWeekend, isUniversityDay);
                    slots.Add(slot);
                }
            }

            // Check gaps between consecutive events
            for (int i = 0; i < dayEvents.Count - 1; i++)
            {
                var currentEvent = dayEvents[i];
                var nextEvent = dayEvents[i + 1];

                var gapStart = currentEvent.EndTime.AddMinutes(constraints.BufferBetweenEventsMinutes);
                var gapEnd = nextEvent.StartTime.AddMinutes(-constraints.BufferBetweenEventsMinutes);

                if ((gapEnd - gapStart).TotalMinutes >= durationMinutes)
                {
                    var slot = new TimeSlot
                    {
                        Start = gapStart,
                        End = gapEnd
                    };
                    slot.QualityScore = ScoreTimeSlot(slot, isWeekend, isUniversityDay);
                    slots.Add(slot);
                }
            }

            // Check gap from last event to day end
            var lastEvent = dayEvents.Last();
            if ((dayEnd - lastEvent.EndTime).TotalMinutes >= durationMinutes + constraints.BufferBetweenEventsMinutes)
            {
                var slot = new TimeSlot
                {
                    Start = lastEvent.EndTime.AddMinutes(constraints.BufferBetweenEventsMinutes),
                    End = dayEnd
                };
                if (slot.DurationMinutes >= durationMinutes)
                {
                    slot.QualityScore = ScoreTimeSlot(slot, isWeekend, isUniversityDay);
                    slots.Add(slot);
                }
            }
        }

        return slots;
    }

    private string ScoreTimeSlot(TimeSlot slot, bool isWeekend, bool isUniversityDay)
    {
        var startHour = slot.Start.Hour;

        // Morning (8-12): Excellent for learning
        if (startHour >= 8 && startHour < 12)
        {
            slot.Reason = "Vormittag - ideal zum Lernen";
            return "excellent";
        }

        // Afternoon (14-17): Good for meetings and learning
        if (startHour >= 14 && startHour < 17)
        {
            slot.Reason = isWeekend ? "Nachmittag am Wochenende" : "Nachmittag - gut für Termine";
            return "good";
        }

        // Evening (18-20): Acceptable
        if (startHour >= 18 && startHour < 20)
        {
            slot.Reason = "Abend - okay für Projekte";
            return "acceptable";
        }

        // Late evening (20-22): Poor
        if (startHour >= 20)
        {
            slot.Reason = "Später Abend - eher ungünstig";
            return "poor";
        }

        // Lunch time (12-14): Poor
        if (startHour >= 12 && startHour < 14)
        {
            slot.Reason = "Mittagszeit";
            return "poor";
        }

        return "good";
    }

    public async Task<CalendarEvent> ScheduleEventAsync(
        int userId,
        string title,
        TimeSlot slot,
        string category)
    {
        try
        {
            var calendarEvent = new CalendarEvent
            {
                UserId = userId,
                Title = title,
                StartTime = slot.Start,
                EndTime = slot.End,
                EventType = category,
                Source = "ai_scheduled",
                CreatedAt = DateTime.UtcNow
            };

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Scheduled event '{title}' at {slot.Start}");
            return calendarEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling event");
            throw;
        }
    }

    public async Task<List<TimeSlot>> SuggestMeetingTimesAsync(
        int userId,
        string personName,
        int durationMinutes)
    {
        try
        {
            _logger.LogInformation($"Suggesting meeting times for {personName}");

            var nextDay = DateTime.UtcNow.Date.AddDays(1);
            var twoWeeksFromNow = nextDay.AddDays(14);

            var constraints = new SchedulingConstraints();
            var allSlots = await FindAvailableTimeSlotsAsync(
                userId,
                nextDay,
                twoWeeksFromNow,
                durationMinutes,
                constraints
            );

            // Prefer afternoon slots for meetings (14-17)
            var afternoonSlots = allSlots
                .Where(s => s.Start.Hour >= 14 && s.Start.Hour < 17)
                .Where(s => s.Start.DayOfWeek != DayOfWeek.Saturday && s.Start.DayOfWeek != DayOfWeek.Sunday)
                .Take(5)
                .ToList();

            // If not enough afternoon slots, add some morning/evening slots
            if (afternoonSlots.Count < 5)
            {
                var additionalSlots = allSlots
                    .Where(s => !afternoonSlots.Contains(s))
                    .Where(s => s.QualityScore != "poor")
                    .Take(7 - afternoonSlots.Count);

                afternoonSlots.AddRange(additionalSlots);
            }

            _logger.LogInformation($"Found {afternoonSlots.Count} suitable meeting slots");
            return afternoonSlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting meeting times");
            return new List<TimeSlot>();
        }
    }

    public async Task<List<LearningSession>> ScheduleLearningSessionsAsync(
        int userId,
        string subject,
        int totalMinutes,
        DateTime deadline)
    {
        try
        {
            _logger.LogInformation($"Scheduling {totalMinutes} minutes of learning for {subject} until {deadline}");

            var sessions = new List<LearningSession>();

            // Calculate number of sessions needed (45-60 min each)
            var sessionDuration = 60; // 60 minutes per session
            var numberOfSessions = (int)Math.Ceiling((double)totalMinutes / sessionDuration);

            // Find available slots until deadline
            var constraints = new SchedulingConstraints();
            var availableSlots = await FindAvailableTimeSlotsAsync(
                userId,
                DateTime.UtcNow.Date.AddDays(1),
                deadline,
                sessionDuration,
                constraints
            );

            // Prefer morning slots for learning (8-12)
            var morningSlots = availableSlots
                .Where(s => s.Start.Hour >= 8 && s.Start.Hour < 12)
                .Take(numberOfSessions)
                .ToList();

            // Distribute sessions evenly
            var selectedSlots = DistributeSessionsEvenly(morningSlots, numberOfSessions, deadline);

            foreach (var slot in selectedSlots.Take(numberOfSessions))
            {
                var session = new LearningSession
                {
                    Start = slot.Start,
                    End = slot.Start.AddMinutes(sessionDuration),
                    Subject = subject,
                    Topic = "",
                    PriorityScore = 80 // High priority for learning
                };
                sessions.Add(session);
            }

            _logger.LogInformation($"Scheduled {sessions.Count} learning sessions");
            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling learning sessions");
            return new List<LearningSession>();
        }
    }

    private List<TimeSlot> DistributeSessionsEvenly(List<TimeSlot> slots, int numberOfSessions, DateTime deadline)
    {
        // Try to spread sessions out evenly over available days
        var result = new List<TimeSlot>();

        if (slots.Count == 0) return result;

        // Group slots by day
        var slotsByDay = slots.GroupBy(s => s.Start.Date).ToList();

        // Try to take one slot from each day
        var dayIndex = 0;
        while (result.Count < numberOfSessions && result.Count < slots.Count)
        {
            var dayGroup = slotsByDay[dayIndex % slotsByDay.Count];
            var availableInDay = dayGroup.Where(s => !result.Contains(s)).ToList();

            if (availableInDay.Any())
            {
                result.Add(availableInDay.First());
            }

            dayIndex++;

            // Avoid infinite loop
            if (dayIndex > slotsByDay.Count * 3)
                break;
        }

        return result;
    }
}

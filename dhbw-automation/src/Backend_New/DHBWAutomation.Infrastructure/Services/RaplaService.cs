using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using DHBWAutomation.Core.Interfaces;
using DHBWAutomation.Infrastructure.Database;
using DHBWAutomation.Core.Models;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace DHBWAutomation.Infrastructure.Services;

public class RaplaService : IRaplaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RaplaService> _logger;

    public RaplaService(
        IHttpClientFactory httpClientFactory,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<RaplaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SyncCalendarAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Starting Rapla calendar sync for user {UserId}", userId);

            var rawData = await GetRawCalendarDataAsync();
            var events = ParseICalData(rawData);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return false;
            }

            // Lösche ALLE alten Rapla-Events und synchronisiere neu
            var oldEvents = await _context.CalendarEvents
                .Where(e => e.UserId == userId && e.Source == "rapla")
                .ToListAsync();

            _context.CalendarEvents.RemoveRange(oldEvents);
            await _context.SaveChangesAsync(); // Erst löschen speichern!
            _logger.LogInformation("Deleted {Count} old Rapla events", oldEvents.Count);

            // Füge neue Events hinzu
            int addedCount = 0;
            foreach (var raplaEvent in events)
            {
                var calendarEvent = new CalendarEvent
                {
                    UserId = userId,
                    Title = raplaEvent.Title ?? "Unbekannte Veranstaltung",
                    Description = raplaEvent.Description,
                    Location = raplaEvent.Location,
                    StartTime = raplaEvent.StartTime,
                    EndTime = raplaEvent.EndTime,
                    Source = "rapla",
                    ExternalId = $"rapla_{raplaEvent.StartTime:yyyyMMddHHmm}_{raplaEvent.Title}",
                    IsAllDay = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CalendarEvents.Add(calendarEvent);
                addedCount++;
            }

            var savedCount = await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully synced {ParsedCount} Rapla events, {AddedCount} added to context, {SavedCount} saved to DB for user {UserId}", 
                events.Count(), addedCount, savedCount, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Rapla calendar for user {UserId}", userId);
            return false;
        }
    }

    public async Task<IEnumerable<RaplaEvent>> GetWeekScheduleAsync(DateTime weekStart)
    {
        try
        {
            var rawData = await GetRawCalendarDataAsync();
            var allEvents = ParseICalData(rawData);

            var weekEnd = weekStart.AddDays(7);
            return allEvents.Where(e => e.StartTime >= weekStart && e.StartTime < weekEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting week schedule from Rapla");
            return Enumerable.Empty<RaplaEvent>();
        }
    }

    public async Task<string> GetRawCalendarDataAsync()
    {
        try
        {
            var baseUrl = _configuration["RAPLA_BASE_URL"] ?? "https://rapla-ravensburg.dhbw.de/rapla";
            var user = _configuration["RAPLA_USER"] ?? "Daurer";
            var file = _configuration["RAPLA_FILE"] ?? "WDS125+1.+Sem";

            // Use page=ical to get iCal format instead of HTML
            var url = $"{baseUrl}?page=ical&user={user}&file={file}";

            _logger.LogInformation("Fetching Rapla calendar from: {Url}", url);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Successfully fetched Rapla calendar data ({Length} bytes)", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Rapla calendar data");
            throw;
        }
    }

    private IEnumerable<RaplaEvent> ParseICalData(string icalData)
    {
        var events = new List<RaplaEvent>();

        try
        {
            // Rapla liefert iCal-Format
            var lines = icalData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            RaplaEvent? currentEvent = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("BEGIN:VEVENT"))
                {
                    currentEvent = new RaplaEvent();
                }
                else if (line.StartsWith("END:VEVENT") && currentEvent != null)
                {
                    events.Add(currentEvent);
                    currentEvent = null;
                }
                else if (currentEvent != null)
                {
                    if (line.StartsWith("SUMMARY:"))
                    {
                        currentEvent.Title = line.Substring(8);
                    }
                    else if (line.StartsWith("DESCRIPTION:"))
                    {
                        currentEvent.Description = line.Substring(12).Replace("\\n", "\n");
                    }
                    else if (line.StartsWith("LOCATION:"))
                    {
                        currentEvent.Location = line.Substring(9);
                    }
                    else if (line.StartsWith("DTSTART"))
                    {
                        currentEvent.StartTime = ParseICalDateTime(line);
                    }
                    else if (line.StartsWith("DTEND"))
                    {
                        currentEvent.EndTime = ParseICalDateTime(line);
                    }
                }
            }

            _logger.LogInformation("Parsed {Count} events from iCal data", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing iCal data");
        }

        return events;
    }

    private DateTime ParseICalDateTime(string icalLine)
    {
        // iCal Format:
        // DTSTART:20260114T080000Z (UTC)
        // DTSTART;TZID=Europe/Berlin:20260114T080000 (lokale Zeit)
        try
        {
            string dateTimeValue;
            bool isUtc = false;
            bool hasTimezone = false;

            // Prüfe ob TZID angegeben ist
            if (icalLine.Contains(";TZID="))
            {
                // Format: DTSTART;TZID=Europe/Berlin:20260114T080000
                var colonIndex = icalLine.LastIndexOf(':');
                dateTimeValue = icalLine.Substring(colonIndex + 1);
                hasTimezone = true;
            }
            else if (icalLine.Contains(":"))
            {
                // Format: DTSTART:20260114T080000Z oder DTSTART:20260114T080000
                var colonIndex = icalLine.IndexOf(':');
                dateTimeValue = icalLine.Substring(colonIndex + 1);
                isUtc = dateTimeValue.EndsWith("Z");
            }
            else
            {
                dateTimeValue = icalLine;
                isUtc = dateTimeValue.EndsWith("Z");
            }

            // Bereinige das Datum
            dateTimeValue = dateTimeValue.Replace(":", "").Replace("-", "").TrimEnd('Z');
            
            var parsedDateTime = DateTime.ParseExact(
                dateTimeValue,
                "yyyyMMddTHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            // Wenn UTC Zeit, dann +1 Stunde für lokale Zeit (UTC+1)
            if (isUtc)
            {
                return parsedDateTime.AddHours(1);
            }
            else if (hasTimezone)
            {
                // Bereits lokale Zeit (Europe/Berlin)
                return parsedDateTime;
            }
            else
            {
                // Annahme: lokale Zeit
                return parsedDateTime;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing iCal datetime: {DateTime}", icalLine);
            return DateTime.Now;
        }
    }
}

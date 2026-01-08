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

            // Lösche alte Rapla-Events (älter als gestern)
            var yesterday = DateTime.UtcNow.AddDays(-1);
            var oldEvents = await _context.CalendarEvents
                .Where(e => e.UserId == userId && e.Source == "rapla" && e.StartTime < yesterday)
                .ToListAsync();

            _context.CalendarEvents.RemoveRange(oldEvents);

            // Füge neue Events hinzu
            foreach (var raplaEvent in events)
            {
                // Prüfe ob Event bereits existiert
                var exists = await _context.CalendarEvents
                    .AnyAsync(e => e.UserId == userId 
                        && e.Source == "rapla" 
                        && e.Title == raplaEvent.Title
                        && e.StartTime == raplaEvent.StartTime);

                if (!exists)
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
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully synced {Count} Rapla events for user {UserId}", events.Count(), userId);

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

            var today = DateTime.Now;
            var url = $"{baseUrl}?page=calendar&user={user}&file={Uri.EscapeDataString(file)}&day={today.Day}&month={today.Month}&year={today.Year}";

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
                    else if (line.StartsWith("DTSTART:"))
                    {
                        currentEvent.StartTime = ParseICalDateTime(line.Substring(8));
                    }
                    else if (line.StartsWith("DTEND:"))
                    {
                        currentEvent.EndTime = ParseICalDateTime(line.Substring(6));
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

    private DateTime ParseICalDateTime(string icalDateTime)
    {
        // iCal Format: 20260114T080000Z oder 20260114T080000
        try
        {
            icalDateTime = icalDateTime.Replace(":", "").Replace("-", "");
            
            if (icalDateTime.EndsWith("Z"))
            {
                // UTC Zeit
                return DateTime.ParseExact(
                    icalDateTime.TrimEnd('Z'),
                    "yyyyMMddTHHmmss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            }
            else
            {
                // Lokale Zeit
                return DateTime.ParseExact(
                    icalDateTime,
                    "yyyyMMddTHHmmss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing iCal datetime: {DateTime}", icalDateTime);
            return DateTime.UtcNow;
        }
    }
}

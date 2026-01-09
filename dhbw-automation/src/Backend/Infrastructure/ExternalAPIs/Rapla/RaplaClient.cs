using Ical.Net;
using Ical.Net.CalendarComponents;
using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Rapla;

/// <summary>
/// Client für den DHBW Rapla-Kalender
/// Holt Stundenplan-Daten als iCalendar und konvertiert sie in CalendarEvents
/// </summary>
public class RaplaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RaplaClient> _logger;
    private readonly string _baseUrl;
    private readonly string _raplaUser;
    private readonly string _raplaFile;

    public RaplaClient(HttpClient httpClient, ILogger<RaplaClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Lade Konfiguration aus Environment Variables oder appsettings
        _baseUrl = Environment.GetEnvironmentVariable("RAPLA_BASE_URL")
                   ?? configuration["Rapla:BaseUrl"]
                   ?? "https://rapla-ravensburg.dhbw.de/rapla";

        _raplaUser = Environment.GetEnvironmentVariable("RAPLA_USER")
                     ?? configuration["Rapla:User"]
                     ?? "Daurer";

        _raplaFile = Environment.GetEnvironmentVariable("RAPLA_FILE")
                     ?? configuration["Rapla:File"]
                     ?? "WDS125+1.+Sem";
    }

    /// <summary>
    /// Baut die Rapla iCalendar-URL
    /// Format: https://rapla-ravensburg.dhbw.de/rapla?page=ical&user=USERNAME&file=FILENAME
    /// Hinweis: Der Rapla-Server erwartet + für Leerzeichen, daher keine vollständige URI-Kodierung
    /// </summary>
    private string BuildRaplaUrl()
    {
        // Rapla erwartet + für Leerzeichen in den Parametern
        // Uri.EscapeDataString würde + zu %2B konvertieren, was der Server nicht akzeptiert
        var userParam = _raplaUser.Replace(" ", "+");
        var fileParam = _raplaFile.Replace(" ", "+");
        return $"{_baseUrl}?page=ical&user={userParam}&file={fileParam}";
    }

    /// <summary>
    /// Holt den iCalendar-String vom Rapla-Server
    /// </summary>
    private async Task<string> FetchICalendarDataAsync()
    {
        try
        {
            var url = BuildRaplaUrl();
            _logger.LogInformation($"Fetching Rapla calendar from: {url}");

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var icalData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Successfully fetched iCalendar data ({icalData.Length} bytes)");

            return icalData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching Rapla calendar");
            throw new Exception($"Fehler beim Abrufen des Rapla-Kalenders: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching Rapla calendar");
            throw;
        }
    }

    /// <summary>
    /// Parst iCalendar-Daten und konvertiert sie in CalendarEvents
    /// </summary>
    private List<DHBWAutomation.Backend.Core.Models.CalendarEvent> ParseICalendarToEvents(string icalData, int userId)
    {
        try
        {
            var calendar = Calendar.Load(icalData);
            var events = new List<DHBWAutomation.Backend.Core.Models.CalendarEvent>();

            foreach (var calEvent in calendar.Events)
            {
                var calendarEvent = new DHBWAutomation.Backend.Core.Models.CalendarEvent
                {
                    UserId = userId,
                    Title = calEvent.Summary ?? "Unbenannter Termin",
                    Description = calEvent.Description,
                    StartTime = calEvent.Start.AsSystemLocal,
                    EndTime = calEvent.End.AsSystemLocal,
                    Location = calEvent.Location,
                    Source = "rapla",
                    ExternalId = calEvent.Uid,
                    IsAllDay = calEvent.IsAllDay,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                };

                // Extrahiere zusätzliche Informationen aus Description oder Summary
                ExtractAdditionalInfo(calendarEvent, calEvent);

                events.Add(calendarEvent);
            }

            _logger.LogInformation($"Parsed {events.Count} events from iCalendar");
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing iCalendar data");
            throw new Exception($"Fehler beim Parsen der iCalendar-Daten: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extrahiert zusätzliche Informationen wie Fach, Professor, Veranstaltungstyp
    /// </summary>
    private void ExtractAdditionalInfo(DHBWAutomation.Backend.Core.Models.CalendarEvent calendarEvent, Ical.Net.CalendarComponents.CalendarEvent sourceEvent)
    {
        var summary = calendarEvent.Title ?? "";
        var description = calendarEvent.Description ?? "";

        // Typische Rapla-Formate:
        // "Mathematik I (Vorlesung)" oder "Programmieren (Übung)"
        // Versuche Fach und Typ zu extrahieren

        var match = System.Text.RegularExpressions.Regex.Match(summary, @"(.+?)\s*\((.+?)\)");
        if (match.Success)
        {
            calendarEvent.Subject = match.Groups[1].Value.Trim();
            calendarEvent.EventType = match.Groups[2].Value.Trim();
        }
        else
        {
            calendarEvent.Subject = summary;
        }

        // Versuche Professor aus Description zu extrahieren
        var profMatch = System.Text.RegularExpressions.Regex.Match(description, @"Prof\.\s+(.+?)[\n\r]");
        if (profMatch.Success)
        {
            calendarEvent.Professor = profMatch.Groups[1].Value.Trim();
        }
    }

    /// <summary>
    /// Holt alle Events vom Rapla-Server und konvertiert sie
    /// </summary>
    /// <param name="userId">Die User-ID für die Events</param>
    /// <returns>Liste von CalendarEvents</returns>
    public async Task<List<DHBWAutomation.Backend.Core.Models.CalendarEvent>> FetchEventsAsync(int userId)
    {
        try
        {
            _logger.LogInformation($"Starting Rapla sync for user {userId}");

            var icalData = await FetchICalendarDataAsync();
            var events = ParseICalendarToEvents(icalData, userId);

            _logger.LogInformation($"Successfully synced {events.Count} events from Rapla");
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to fetch Rapla events for user {userId}");
            throw;
        }
    }

    /// <summary>
    /// Testet die Verbindung zum Rapla-Server
    /// </summary>
    public async Task<RaplaConnectionTestResult> TestConnectionAsync()
    {
        var result = new RaplaConnectionTestResult
        {
            Url = BuildRaplaUrl(),
            User = _raplaUser,
            File = _raplaFile
        };

        try
        {
            var response = await _httpClient.GetAsync(result.Url);
            result.IsConnected = response.IsSuccessStatusCode;
            result.StatusCode = (int)response.StatusCode;

            if (result.IsConnected)
            {
                var content = await response.Content.ReadAsStringAsync();
                result.ContentLength = content.Length;
                result.Message = "Verbindung erfolgreich";

                // Versuche zu parsen
                try
                {
                    var calendar = Calendar.Load(content);
                    result.EventCount = calendar.Events.Count;
                    result.Message = $"Verbindung erfolgreich. {result.EventCount} Events gefunden.";
                }
                catch
                {
                    result.Message = "Verbindung erfolgreich, aber Parsing fehlgeschlagen";
                }
            }
            else
            {
                result.Message = $"Verbindung fehlgeschlagen: HTTP {result.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            result.IsConnected = false;
            result.Message = $"Fehler: {ex.Message}";
            _logger.LogError(ex, "Rapla connection test failed");
        }

        return result;
    }
}

/// <summary>
/// Ergebnis des Rapla-Verbindungstests
/// </summary>
public class RaplaConnectionTestResult
{
    public bool IsConnected { get; set; }
    public string Url { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int ContentLength { get; set; }
    public int EventCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

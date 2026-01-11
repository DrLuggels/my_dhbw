using Ical.Net;
using Ical.Net.CalendarComponents;
using DHBWAutomation.Backend.Core.Models;
using System.Text.RegularExpressions;

namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Rapla;

/// <summary>
/// Datenstruktur für aus HTML extrahierte Rauminformationen
/// </summary>
public class RaplaHtmlEventInfo
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public string? Professor { get; set; }
}

/// <summary>
/// Client für den DHBW Rapla-Kalender
/// Holt Stundenplan-Daten als iCalendar und konvertiert sie in CalendarEvents
/// Nutzt Hybrid-Ansatz: iCalendar für Events + HTML für Räume
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
    /// Baut die Rapla HTML-Kalender-URL für eine bestimmte Woche
    /// Format: https://rapla-ravensburg.dhbw.de/rapla?page=calendar&user=USERNAME&file=FILENAME&day=DD&month=MM&year=YYYY
    /// </summary>
    private string BuildHtmlCalendarUrl(DateTime date)
    {
        var userParam = _raplaUser.Replace(" ", "+");
        var fileParam = _raplaFile.Replace(" ", "+");
        return $"{_baseUrl}?page=calendar&user={userParam}&file={fileParam}&day={date.Day}&month={date.Month}&year={date.Year}";
    }

    /// <summary>
    /// Holt die HTML-Kalenderseite für eine bestimmte Woche
    /// </summary>
    private async Task<string> FetchHtmlCalendarDataAsync(DateTime date)
    {
        var url = BuildHtmlCalendarUrl(date);
        _logger.LogDebug($"Fetching HTML calendar from: {url}");

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Parst die HTML-Kalenderseite und extrahiert Event-Infos mit Räumen
    /// </summary>
    private List<RaplaHtmlEventInfo> ParseHtmlForRoomInfo(string html)
    {
        var events = new List<RaplaHtmlEventInfo>();

        // HTML in eine Zeile um Multiline-Matching zu vereinfachen
        var singleLineHtml = html.Replace("\n", " ").Replace("\r", " ");

        // Regex für Tooltip-Blöcke: <span class="tooltip">...</span></a>
        var tooltipPattern = @"<span class=""tooltip"">(.*?)</span></a>";
        var tooltipMatches = Regex.Matches(singleLineHtml, tooltipPattern, RegexOptions.Singleline);

        foreach (Match tooltipMatch in tooltipMatches)
        {
            var tooltipContent = tooltipMatch.Groups[1].Value;

            var eventInfo = new RaplaHtmlEventInfo();

            // Datum und Zeit extrahieren: "Mo 12.01.26 09:00-12:15"
            var dateTimePattern = @"([A-Za-z]{2})\s+(\d{2})\.(\d{2})\.(\d{2})\s+(\d{2}):(\d{2})-(\d{2}):(\d{2})";
            var dateTimeMatch = Regex.Match(tooltipContent, dateTimePattern);
            if (dateTimeMatch.Success)
            {
                var day = int.Parse(dateTimeMatch.Groups[2].Value);
                var month = int.Parse(dateTimeMatch.Groups[3].Value);
                var year = 2000 + int.Parse(dateTimeMatch.Groups[4].Value);
                var startHour = int.Parse(dateTimeMatch.Groups[5].Value);
                var startMinute = int.Parse(dateTimeMatch.Groups[6].Value);
                var endHour = int.Parse(dateTimeMatch.Groups[7].Value);
                var endMinute = int.Parse(dateTimeMatch.Groups[8].Value);

                eventInfo.StartTime = new DateTime(year, month, day, startHour, startMinute, 0);
                eventInfo.EndTime = new DateTime(year, month, day, endHour, endMinute, 0);
            }

            // Titel extrahieren: <td class="label"...>Titel:</td> <td class="value"...>TITLE</td>
            var titlePattern = @"<td class=""label""[^>]*>Titel:</td>\s*<td class=""value""[^>]*>([^<]+)</td>";
            var titleMatch = Regex.Match(tooltipContent, titlePattern);
            if (titleMatch.Success)
            {
                eventInfo.Title = titleMatch.Groups[1].Value.Trim();
            }

            // Ressourcen/Räume extrahieren: <td class="label"...>Ressourcen:</td> <td class="value"...>RV-WDS125,MP124 Hörsaal</td>
            var resourcePattern = @"<td class=""label""[^>]*>Ressourcen:</td>\s*<td class=""value""[^>]*>([^<]+)</td>";
            var resourceMatch = Regex.Match(tooltipContent, resourcePattern);
            if (resourceMatch.Success)
            {
                var resources = resourceMatch.Groups[1].Value.Trim();
                // Filtere Kursgruppen raus (RV-WDS...), behalte nur Räume (MP...)
                var parts = resources.Split(',').Select(p => p.Trim()).ToList();
                var rooms = parts.Where(p => !p.StartsWith("RV-")).ToList();
                if (rooms.Any())
                {
                    eventInfo.Location = string.Join(", ", rooms);
                }
            }

            // Personen/Professor extrahieren
            var personPattern = @"<td class=""label""[^>]*>Personen:</td>\s*<td class=""value""[^>]*>([^<]+)</td>";
            var personMatch = Regex.Match(tooltipContent, personPattern);
            if (personMatch.Success)
            {
                eventInfo.Professor = personMatch.Groups[1].Value.Trim();
            }

            // Nur hinzufügen wenn wir mindestens Titel und Zeit haben
            if (!string.IsNullOrEmpty(eventInfo.Title) && eventInfo.StartTime != default)
            {
                events.Add(eventInfo);
                _logger.LogDebug($"Parsed HTML event: {eventInfo.Title} @ {eventInfo.StartTime:dd.MM.yy HH:mm} - Location: {eventInfo.Location ?? "N/A"}");
            }
        }

        _logger.LogInformation($"Parsed {events.Count} events with room info from HTML");
        return events;
    }

    /// <summary>
    /// Reichert iCalendar-Events mit Rauminformationen aus HTML an
    /// Matching erfolgt über Titel (Anfang) + Startzeit
    /// </summary>
    private void EnrichEventsWithRoomInfo(List<DHBWAutomation.Backend.Core.Models.CalendarEvent> events, List<RaplaHtmlEventInfo> htmlInfos)
    {
        foreach (var evt in events)
        {
            // Suche passendes HTML-Event über Titel-Anfang und Startzeit
            // iCal-Titel: "SQ: Wissenschaftliches Arbeiten (W4DSKI_701.1) [Daurer, Prof. Dr. Stephan]"
            // HTML-Titel: "SQ: Wissenschaftliches Arbeiten (W4DSKI_701.1)"
            var htmlInfo = htmlInfos.FirstOrDefault(h =>
                evt.Title.StartsWith(h.Title, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs((evt.StartTime - h.StartTime).TotalMinutes) < 5);

            if (htmlInfo != null)
            {
                if (!string.IsNullOrEmpty(htmlInfo.Location))
                {
                    evt.Location = htmlInfo.Location;
                    _logger.LogDebug($"Enriched event '{evt.Title}' with location: {htmlInfo.Location}");
                }
                if (!string.IsNullOrEmpty(htmlInfo.Professor) && string.IsNullOrEmpty(evt.Professor))
                {
                    evt.Professor = htmlInfo.Professor;
                }
            }
        }
    }

    /// <summary>
    /// Holt Rauminformationen aus HTML für einen Zeitraum (mehrere Wochen)
    /// </summary>
    private async Task<List<RaplaHtmlEventInfo>> FetchRoomInfoForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var allHtmlInfos = new List<RaplaHtmlEventInfo>();
        var processedWeeks = new HashSet<string>();

        // Iteriere durch alle Wochen im Zeitraum
        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            // Berechne Montag der aktuellen Woche
            var daysUntilMonday = ((int)currentDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var weekStart = currentDate.AddDays(-daysUntilMonday);
            var weekKey = $"{weekStart:yyyy-MM-dd}";

            if (!processedWeeks.Contains(weekKey))
            {
                processedWeeks.Add(weekKey);

                try
                {
                    var html = await FetchHtmlCalendarDataAsync(weekStart);
                    var weekInfos = ParseHtmlForRoomInfo(html);
                    allHtmlInfos.AddRange(weekInfos);
                    _logger.LogDebug($"Fetched room info for week starting {weekStart:dd.MM.yyyy}: {weekInfos.Count} events");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to fetch HTML calendar for week {weekKey}");
                }
            }

            currentDate = currentDate.AddDays(7);
        }

        return allHtmlInfos;
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
    /// Nutzt Hybrid-Ansatz: iCalendar für Events + HTML für Räume
    /// </summary>
    /// <param name="userId">Die User-ID für die Events</param>
    /// <returns>Liste von CalendarEvents</returns>
    public async Task<List<DHBWAutomation.Backend.Core.Models.CalendarEvent>> FetchEventsAsync(int userId)
    {
        try
        {
            _logger.LogInformation($"Starting Rapla sync for user {userId} (Hybrid-Ansatz: iCal + HTML)");

            // 1. Hole Events aus iCalendar (mit UIDs)
            var icalData = await FetchICalendarDataAsync();
            var events = ParseICalendarToEvents(icalData, userId);

            if (events.Any())
            {
                // 2. Bestimme Zeitraum der Events
                var minDate = events.Min(e => e.StartTime);
                var maxDate = events.Max(e => e.EndTime);

                _logger.LogInformation($"Fetching room info from HTML for period {minDate:dd.MM.yyyy} - {maxDate:dd.MM.yyyy}");

                // 3. Hole Rauminformationen aus HTML
                var htmlInfos = await FetchRoomInfoForDateRangeAsync(minDate, maxDate);

                // 4. Reichere Events mit Räumen an
                EnrichEventsWithRoomInfo(events, htmlInfos);

                var eventsWithRooms = events.Count(e => !string.IsNullOrEmpty(e.Location));
                _logger.LogInformation($"Enriched {eventsWithRooms}/{events.Count} events with room information");
            }

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

using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly IConfiguration _configuration;
    private static readonly string[] Scopes = { CalendarService.Scope.Calendar };
    private const string ApplicationName = "DHBW Automation";

    public GoogleCalendarService(
        AppDbContext context,
        ILogger<GoogleCalendarService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> GetAuthorizationUrlAsync(int userId)
    {
        try
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _configuration["Google:ClientId"] ?? throw new Exception("Google ClientId nicht konfiguriert"),
                ClientSecret = _configuration["Google:ClientSecret"] ?? throw new Exception("Google ClientSecret nicht konfiguriert")
            };

            var redirectUri = _configuration["Google:RedirectUri"] ?? "http://localhost:5000/api/calendar/google/callback";

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = Scopes,
                DataStore = new FileDataStore("GoogleCalendarTokens")
            });

            var authUrl = flow.CreateAuthorizationCodeRequest(redirectUri);
            authUrl.State = userId.ToString();

            return authUrl.Build().ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erstellen der Authorization URL");
            throw;
        }
    }

    public async Task<bool> HandleCallbackAsync(int userId, string code)
    {
        try
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _configuration["Google:ClientId"] ?? throw new Exception("Google ClientId nicht konfiguriert"),
                ClientSecret = _configuration["Google:ClientSecret"] ?? throw new Exception("Google ClientSecret nicht konfiguriert")
            };

            var redirectUri = _configuration["Google:RedirectUri"] ?? "http://localhost:5000/api/calendar/google/callback";

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = Scopes,
                DataStore = new FileDataStore("GoogleCalendarTokens")
            });

            var token = await flow.ExchangeCodeForTokenAsync(
                userId.ToString(),
                code,
                redirectUri,
                CancellationToken.None);

            _logger.LogInformation($"Google Calendar erfolgreich für User {userId} verbunden");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Handeln des OAuth Callbacks");
            return false;
        }
    }

    public async Task<int> SyncFromGoogleAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var service = await GetCalendarServiceAsync(userId);
            if (service == null)
            {
                _logger.LogWarning($"User {userId} ist nicht mit Google Calendar verbunden");
                return 0;
            }

            var request = service.Events.List("primary");
            request.TimeMin = startDate ?? DateTime.UtcNow.AddMonths(-1);
            request.TimeMax = endDate ?? DateTime.UtcNow.AddMonths(3);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            var syncedCount = 0;

            foreach (var googleEvent in events.Items ?? Enumerable.Empty<Event>())
            {
                // Prüfe ob Event bereits existiert
                var existingEvent = await _context.CalendarEvents
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.ExternalId == googleEvent.Id);

                var calendarEvent = MapGoogleEventToCalendarEvent(googleEvent, userId);

                if (existingEvent != null)
                {
                    // Update
                    existingEvent.Title = calendarEvent.Title;
                    existingEvent.Description = calendarEvent.Description;
                    existingEvent.Location = calendarEvent.Location;
                    existingEvent.StartTime = calendarEvent.StartTime;
                    existingEvent.EndTime = calendarEvent.EndTime;
                    existingEvent.IsAllDay = calendarEvent.IsAllDay;
                    _context.CalendarEvents.Update(existingEvent);
                }
                else
                {
                    // Insert
                    await _context.CalendarEvents.AddAsync(calendarEvent);
                }

                syncedCount++;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"{syncedCount} Events von Google Calendar importiert");
            return syncedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Importieren von Google Calendar");
            throw;
        }
    }

    public async Task<int> SyncToGoogleAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var service = await GetCalendarServiceAsync(userId);
            if (service == null)
            {
                _logger.LogWarning($"User {userId} ist nicht mit Google Calendar verbunden");
                return 0;
            }

            // Hole lokale Events ohne Google-ID
            var query = _context.CalendarEvents
                .Where(e => e.UserId == userId && string.IsNullOrEmpty(e.ExternalId));

            if (startDate.HasValue)
                query = query.Where(e => e.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.EndTime <= endDate.Value);

            var localEvents = await query.ToListAsync();
            var exportedCount = 0;

            foreach (var localEvent in localEvents)
            {
                var googleEventId = await CreateEventAsync(userId, localEvent);
                if (!string.IsNullOrEmpty(googleEventId))
                {
                    localEvent.ExternalId = googleEventId;
                    localEvent.Source = "google";
                    _context.CalendarEvents.Update(localEvent);
                    exportedCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"{exportedCount} Events zu Google Calendar exportiert");
            return exportedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Exportieren zu Google Calendar");
            throw;
        }
    }

    public async Task<(int imported, int exported)> SyncBidirectionalAsync(int userId)
    {
        var imported = await SyncFromGoogleAsync(userId);
        var exported = await SyncToGoogleAsync(userId);
        return (imported, exported);
    }

    public async Task<string?> CreateEventAsync(int userId, CalendarEvent calendarEvent)
    {
        try
        {
            var service = await GetCalendarServiceAsync(userId);
            if (service == null) return null;

            var googleEvent = MapCalendarEventToGoogleEvent(calendarEvent);
            var request = service.Events.Insert(googleEvent, "primary");
            var createdEvent = await request.ExecuteAsync();

            _logger.LogInformation($"Event '{calendarEvent.Title}' in Google Calendar erstellt");
            return createdEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erstellen des Google Calendar Events");
            return null;
        }
    }

    public async Task<bool> UpdateEventAsync(int userId, CalendarEvent calendarEvent)
    {
        try
        {
            if (string.IsNullOrEmpty(calendarEvent.ExternalId))
                return false;

            var service = await GetCalendarServiceAsync(userId);
            if (service == null) return false;

            var googleEvent = MapCalendarEventToGoogleEvent(calendarEvent);
            var request = service.Events.Update(googleEvent, "primary", calendarEvent.ExternalId);
            await request.ExecuteAsync();

            _logger.LogInformation($"Event '{calendarEvent.Title}' in Google Calendar aktualisiert");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren des Google Calendar Events");
            return false;
        }
    }

    public async Task<bool> DeleteEventAsync(int userId, string googleEventId)
    {
        try
        {
            var service = await GetCalendarServiceAsync(userId);
            if (service == null) return false;

            var request = service.Events.Delete("primary", googleEventId);
            await request.ExecuteAsync();

            _logger.LogInformation($"Event mit ID '{googleEventId}' in Google Calendar gelöscht");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen des Google Calendar Events");
            return false;
        }
    }

    public async Task<bool> IsConnectedAsync(int userId)
    {
        var service = await GetCalendarServiceAsync(userId);
        return service != null;
    }

    // Helper Methods
    private async Task<CalendarService?> GetCalendarServiceAsync(int userId)
    {
        try
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _configuration["Google:ClientId"] ?? throw new Exception("Google ClientId nicht konfiguriert"),
                ClientSecret = _configuration["Google:ClientSecret"] ?? throw new Exception("Google ClientSecret nicht konfiguriert")
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = Scopes,
                DataStore = new FileDataStore("GoogleCalendarTokens")
            });

            var token = await flow.LoadTokenAsync(userId.ToString(), CancellationToken.None);
            if (token == null)
                return null;

            var credential = new UserCredential(flow, userId.ToString(), token);

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            return service;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erstellen des Calendar Service");
            return null;
        }
    }

    private CalendarEvent MapGoogleEventToCalendarEvent(Event googleEvent, int userId)
    {
        var startTime = googleEvent.Start.DateTime ?? DateTime.Parse(googleEvent.Start.Date);
        var endTime = googleEvent.End.DateTime ?? DateTime.Parse(googleEvent.End.Date);

        return new CalendarEvent
        {
            UserId = userId,
            Title = googleEvent.Summary ?? "Kein Titel",
            Description = googleEvent.Description,
            Location = googleEvent.Location,
            StartTime = startTime,
            EndTime = endTime,
            IsAllDay = googleEvent.Start.DateTime == null,
            ExternalId = googleEvent.Id,
            Source = "google",
            Notes = googleEvent.Description
        };
    }

    private Event MapCalendarEventToGoogleEvent(CalendarEvent calendarEvent)
    {
        var googleEvent = new Event
        {
            Summary = calendarEvent.Title,
            Description = calendarEvent.Description,
            Location = calendarEvent.Location
        };

        if (calendarEvent.IsAllDay)
        {
            googleEvent.Start = new EventDateTime
            {
                Date = calendarEvent.StartTime.ToString("yyyy-MM-dd")
            };
            googleEvent.End = new EventDateTime
            {
                Date = calendarEvent.EndTime.ToString("yyyy-MM-dd")
            };
        }
        else
        {
            googleEvent.Start = new EventDateTime
            {
                DateTime = calendarEvent.StartTime,
                TimeZone = "Europe/Berlin"
            };
            googleEvent.End = new EventDateTime
            {
                DateTime = calendarEvent.EndTime,
                TimeZone = "Europe/Berlin"
            };
        }

        return googleEvent;
    }
}

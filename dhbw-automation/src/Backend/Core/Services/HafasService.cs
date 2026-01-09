using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.API.DTOs;
using DHBWAutomation.Backend.Core.Interfaces;

namespace DHBWAutomation.Backend.Core.Services;

public class HafasService : ITravelService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HafasService> _logger;
    private const string HAFAS_ENDPOINT = "https://v6.db.transport.rest";

    public HafasService(HttpClient httpClient, ILogger<HafasService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(HAFAS_ENDPOINT);
    }

    public async Task<TrainConnectionResponse> GetConnectionsAsync(TrainConnectionRequest request)
    {
        try
        {
            var dateTime = request.DateTime ?? DateTime.Now;
            var url = $"/journeys?from={Uri.EscapeDataString(request.From)}&to={Uri.EscapeDataString(request.To)}&departure={dateTime:yyyy-MM-ddTHH:mm:ss}&results={request.MaxConnections}";

            _logger.LogInformation("Requesting train connections from {From} to {To}", request.From, request.To);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var hafasResponse = JsonSerializer.Deserialize<HafasJourneysResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (hafasResponse?.Journeys == null)
            {
                return new TrainConnectionResponse
                {
                    Journeys = new List<Journey>(),
                    RequestedAt = DateTime.Now
                };
            }

            var journeys = hafasResponse.Journeys.Select(MapToJourney).ToList();

            return new TrainConnectionResponse
            {
                Journeys = journeys,
                RequestedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching train connections");
            throw;
        }
    }

    private Journey MapToJourney(HafasJourney hafasJourney)
    {
        var legs = hafasJourney.Legs?.Select(leg => new Leg
        {
            From = leg.Origin?.Name ?? string.Empty,
            To = leg.Destination?.Name ?? string.Empty,
            Departure = DateTime.Parse(leg.Departure ?? DateTime.Now.ToString()),
            Arrival = DateTime.Parse(leg.Arrival ?? DateTime.Now.ToString()),
            Line = leg.Line?.Name,
            Direction = leg.Direction,
            Platform = leg.DeparturePlatform,
            Delay = leg.DepartureDelay,
            Cancelled = leg.Cancelled
        }).ToList() ?? new List<Leg>();

        var departure = DateTime.Parse(hafasJourney.Legs?.FirstOrDefault()?.Departure ?? DateTime.Now.ToString());
        var arrival = DateTime.Parse(hafasJourney.Legs?.LastOrDefault()?.Arrival ?? DateTime.Now.ToString());
        var duration = arrival - departure;

        return new Journey
        {
            From = hafasJourney.Legs?.FirstOrDefault()?.Origin?.Name ?? string.Empty,
            To = hafasJourney.Legs?.LastOrDefault()?.Destination?.Name ?? string.Empty,
            Departure = departure,
            Arrival = arrival,
            Duration = $"{duration.Hours}h {duration.Minutes}m",
            Transfers = (hafasJourney.Legs?.Count ?? 1) - 1,
            Legs = legs,
            Cancelled = hafasJourney.Legs?.Any(l => l.Cancelled == true),
            Delay = hafasJourney.Legs?.Max(l => l.DepartureDelay ?? 0)
        };
    }

    // HAFAS Response DTOs
    private class HafasJourneysResponse
    {
        public List<HafasJourney>? Journeys { get; set; }
    }

    private class HafasJourney
    {
        public List<HafasLeg>? Legs { get; set; }
    }

    private class HafasLeg
    {
        public HafasLocation? Origin { get; set; }
        public HafasLocation? Destination { get; set; }
        public string? Departure { get; set; }
        public string? Arrival { get; set; }
        public HafasLine? Line { get; set; }
        public string? Direction { get; set; }
        public string? DeparturePlatform { get; set; }
        public int? DepartureDelay { get; set; }
        public int? ArrivalDelay { get; set; }
        public bool? Cancelled { get; set; }
    }

    private class HafasLocation
    {
        public string? Name { get; set; }
    }

    private class HafasLine
    {
        public string? Name { get; set; }
    }
}

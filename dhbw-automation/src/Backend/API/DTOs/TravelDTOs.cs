namespace DHBWAutomation.Backend.API.DTOs;

public class TrainConnectionRequest
{
    public string From { get; set; } = "Laupheim West";
    public string To { get; set; } = "Ravensburg";
    public DateTime? DateTime { get; set; }
    public int MaxConnections { get; set; } = 5;
}

public class TrainConnectionResponse
{
    public List<Journey> Journeys { get; set; } = new();
    public DateTime RequestedAt { get; set; }
}

public class Journey
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime Departure { get; set; }
    public DateTime Arrival { get; set; }
    public string Duration { get; set; } = string.Empty;
    public int Transfers { get; set; }
    public List<Leg> Legs { get; set; } = new();
    public bool? Cancelled { get; set; }
    public int? Delay { get; set; }
}

public class Leg
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime Departure { get; set; }
    public DateTime Arrival { get; set; }
    public string? Line { get; set; }
    public string? Direction { get; set; }
    public string? Platform { get; set; }
    public int? Delay { get; set; }
    public bool? Cancelled { get; set; }
}

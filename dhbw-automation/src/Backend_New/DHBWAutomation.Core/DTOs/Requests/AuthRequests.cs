namespace DHBWAutomation.Core.DTOs.Requests;

public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MatriculationNumber { get; init; }
    public string? Course { get; init; }
}

public record ChangePasswordRequest
{
    public required string OldPassword { get; init; }
    public required string NewPassword { get; init; }
}

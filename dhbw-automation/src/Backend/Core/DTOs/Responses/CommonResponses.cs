namespace DHBWAutomation.Backend.Core.DTOs.Responses;

public record AuthResponse
{
    public required string Token { get; init; }
    public required string RefreshToken { get; init; }
    public required UserResponse User { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public record UserResponse
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? MatriculationNumber { get; init; }
    public string? Course { get; init; }
    public bool EmailVerified { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record DocumentResponse
{
    public int Id { get; init; }
    public required string FileName { get; init; }
    public required string FileType { get; init; }
    public long FileSize { get; init; }
    public string? Category { get; init; }
    public string? Subject { get; init; }
    public string? Summary { get; init; }
    public string[]? Tags { get; init; }
    public bool IsProcessed { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public string[]? Errors { get; init; }
}

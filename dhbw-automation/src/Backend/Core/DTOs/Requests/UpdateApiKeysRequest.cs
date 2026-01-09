namespace DHBWAutomation.Backend.Core.DTOs.Requests;

public class UpdateApiKeysRequest
{
    public string? OpenAiApiKey { get; set; }
    public string? AnthropicApiKey { get; set; }
    public string? GeminiApiKey { get; set; }
}

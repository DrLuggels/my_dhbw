namespace DHBWAutomation.Backend.Core.DTOs.Responses;

public class ApiKeysResponse
{
    public bool HasOpenAiKey { get; set; }
    public bool HasAnthropicKey { get; set; }
    public bool HasGeminiKey { get; set; }
    
    // Zeige nur erste und letzte 4 Zeichen zur Verifikation (falls gesetzt)
    public string? OpenAiKeyPreview { get; set; }
    public string? AnthropicKeyPreview { get; set; }
    public string? GeminiKeyPreview { get; set; }
}

using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Shared.Helpers;
using DHBWAutomation.Backend.Infrastructure.Database;

namespace DHBWAutomation.Backend.Core.Services.IntentAnalysis;

public partial class IntentAnalysisService : IIntentAnalysisService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiMetrics _aiMetrics;
    private readonly ILogger<IntentAnalysisService> _logger;
    private readonly AppDbContext _context;
    private readonly EncryptionHelper _encryptionHelper;

    private const string AnthropicModel = "claude-sonnet-4-5";
    private const string AnthropicEndpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private readonly string? _anthropicApiKey;

    public IntentAnalysisService(
        IHttpClientFactory httpClientFactory,
        AiMetrics aiMetrics,
        ILogger<IntentAnalysisService> logger,
        AppDbContext context,
        EncryptionHelper encryptionHelper)
    {
        _httpClientFactory = httpClientFactory;
        _aiMetrics = aiMetrics;
        _logger = logger;
        _context = context;
        _encryptionHelper = encryptionHelper;

        _anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }

    private async Task<string?> GetApiKeyAsync(int? userId)
    {
        _logger.LogInformation("GetApiKeyAsync called for Anthropic - UserId: {UserId}", userId);

        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && !string.IsNullOrEmpty(user.AnthropicApiKey))
            {
                _logger.LogInformation("User-specific Anthropic key found, decrypting...");
                var decrypted = _encryptionHelper.Decrypt(user.AnthropicApiKey);
                _logger.LogInformation("Decrypted Anthropic key (first 20 chars): {KeyPrefix}, Length: {Length}",
                    decrypted?.Substring(0, Math.Min(20, decrypted?.Length ?? 0)) ?? "null",
                    decrypted?.Length ?? 0);
                return decrypted;
            }
            else
            {
                _logger.LogWarning("User Anthropic key not found or empty, falling back to system key");
            }
        }
        else
        {
            _logger.LogWarning("UserId is NULL, using system fallback key for Anthropic");
        }

        _logger.LogInformation("Using system Anthropic key, exists? {Exists}", !string.IsNullOrEmpty(_anthropicApiKey));
        return _anthropicApiKey;
    }
}

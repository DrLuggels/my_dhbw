using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Shared.Helpers;
using Ganss.Xss;

namespace DHBWAutomation.Backend.Core.Services.LearningAnalytics;

public partial class LearningAnalyticsService : ILearningAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LearningAnalyticsService> _logger;
    private readonly AnthropicClient _anthropicClient;
    private readonly AiMetrics _aiMetrics;
    private readonly HtmlSanitizer _htmlSanitizer;
    private readonly EncryptionHelper _encryptionHelper;

    private const string AnthropicModel = "claude-sonnet-4-5";

    public LearningAnalyticsService(
        AppDbContext context,
        ILogger<LearningAnalyticsService> logger,
        AnthropicClient anthropicClient,
        AiMetrics aiMetrics,
        EncryptionHelper encryptionHelper)
    {
        _context = context;
        _logger = logger;
        _anthropicClient = anthropicClient;
        _aiMetrics = aiMetrics;
        _encryptionHelper = encryptionHelper;

        _htmlSanitizer = new HtmlSanitizer();
        ConfigureHtmlSanitizer();
    }

    private async Task<string?> GetApiKeyAsync(int? userId)
    {
        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && !string.IsNullOrEmpty(user.AnthropicApiKey))
            {
                return _encryptionHelper.Decrypt(user.AnthropicApiKey);
            }
        }
        return null;
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.VectorDb;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services.Embedding;

public partial class EmbeddingService : IEmbeddingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly IQdrantService _qdrantService;
    private readonly AppDbContext _context;
    private readonly EncryptionHelper _encryptionHelper;

    private readonly string? _openAiApiKey;

    internal const string OpenAiEmbeddingEndpoint = "https://api.openai.com/v1/embeddings";
    internal const string OpenAiEmbeddingModel = "text-embedding-3-small";
    internal const int MaxTextLength = 8000;

    public EmbeddingService(
        IHttpClientFactory httpClientFactory,
        ILogger<EmbeddingService> logger,
        IQdrantService qdrantService,
        AppDbContext context,
        EncryptionHelper encryptionHelper)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _qdrantService = qdrantService;
        _context = context;
        _encryptionHelper = encryptionHelper;
        _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    private async Task<string?> GetApiKeyAsync(int? userId)
    {
        if (userId.HasValue)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && !string.IsNullOrEmpty(user.OpenAiApiKey))
            {
                try
                {
                    var decrypted = _encryptionHelper.Decrypt(user.OpenAiApiKey);
                    if (!string.IsNullOrEmpty(decrypted))
                    {
                        var keyPreview = decrypted.Length > 12 ? decrypted.Substring(0, 12) + "..." : "***";
                        _logger.LogWarning(">>> USING USER {UserId}'s API KEY: {KeyPreview} (length={Length})",
                            userId, keyPreview, decrypted.Length);
                        return decrypted;
                    }
                    _logger.LogWarning("User {UserId} has encrypted API key but decryption returned empty", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt OpenAI API key for user {UserId}", userId);
                }
            }
            else
            {
                _logger.LogDebug("User {UserId} has no OpenAI API key configured", userId);
            }
        }

        if (!string.IsNullOrEmpty(_openAiApiKey))
        {
            var keyPreview = _openAiApiKey.Length > 12 ? _openAiApiKey.Substring(0, 12) + "..." : "***";
            _logger.LogWarning(">>> USING SYSTEM API KEY: {KeyPreview} (length={Length})",
                keyPreview, _openAiApiKey.Length);
            return _openAiApiKey;
        }

        _logger.LogWarning("No OpenAI API key available (neither user nor system)");
        return null;
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, int? userId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Cannot generate embedding for empty text");
                return null;
            }

            var apiKey = await GetApiKeyAsync(userId);
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("OpenAI API Key not available for embedding generation");
                return null;
            }

            var inputText = text.Length > MaxTextLength ? text.Substring(0, MaxTextLength) : text;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new { model = OpenAiEmbeddingModel, input = inputText };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogWarning(">>> SENDING REQUEST TO OPENAI: {Endpoint}, model={Model}, inputLength={Length}",
                OpenAiEmbeddingEndpoint, OpenAiEmbeddingModel, inputText.Length);

            var response = await client.PostAsync(OpenAiEmbeddingEndpoint, content);
            _logger.LogWarning(">>> OPENAI RESPONSE STATUS: {StatusCode}", response.StatusCode);

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(">>> OPENAI RESPONSE (first 500 chars): {Response}",
                responseJson.Length > 500 ? responseJson.Substring(0, 500) : responseJson);

            response.EnsureSuccessStatusCode();
            var result = JsonDocument.Parse(responseJson);

            var embeddingArray = result.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();

            _logger.LogWarning(">>> EMBEDDING GENERATED: {Dimensions} dimensions, first 5 values: [{Values}]",
                embeddingArray.Length,
                string.Join(", ", embeddingArray.Take(5).Select(v => v.ToString("F6"))));

            return embeddingArray;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            return null;
        }
    }
}

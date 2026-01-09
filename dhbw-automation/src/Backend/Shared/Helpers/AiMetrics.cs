using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Shared.Helpers;

/// <summary>
/// Metriken-Tracking für AI-Service-Aufrufe
/// </summary>
public class AiMetrics
{
    private readonly ILogger<AiMetrics> _logger;

    public AiMetrics(ILogger<AiMetrics> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Führt eine Aktion mit Metriken-Tracking aus
    /// </summary>
    /// <typeparam name="T">Rückgabetyp der Aktion</typeparam>
    /// <param name="operation">Name der Operation (z.B. "GenerateTags")</param>
    /// <param name="provider">AI-Provider (z.B. "OpenAI", "Anthropic", "Gemini")</param>
    /// <param name="model">Verwendetes Modell (z.B. "gpt-5-mini", "claude-sonnet-4.5")</param>
    /// <param name="action">Auszuführende Aktion</param>
    /// <returns>Ergebnis der Aktion</returns>
    public async Task<T> TrackAsync<T>(
        string operation, 
        string provider, 
        string model,
        Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "AI_CALL_START | CorrelationId: {CorrelationId} | Operation: {Operation} | Provider: {Provider} | Model: {Model}",
            correlationId, operation, provider, model
        );

        try
        {
            var result = await action();
            stopwatch.Stop();

            // Schätze Token-Usage basierend auf Result-Typ
            var estimatedTokens = EstimateTokens(result);

            _logger.LogInformation(
                "AI_CALL_SUCCESS | CorrelationId: {CorrelationId} | Operation: {Operation} | Provider: {Provider} | " +
                "Model: {Model} | Duration: {Duration}ms | EstimatedTokens: {Tokens}",
                correlationId, operation, provider, model, stopwatch.ElapsedMilliseconds, estimatedTokens
            );

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "AI_CALL_ERROR | CorrelationId: {CorrelationId} | Operation: {Operation} | Provider: {Provider} | " +
                "Model: {Model} | Duration: {Duration}ms | Error: {Error}",
                correlationId, operation, provider, model, stopwatch.ElapsedMilliseconds, ex.Message
            );

            throw;
        }
    }

    /// <summary>
    /// Schätzt Token-Count basierend auf Result-Typ (grobe Approximation)
    /// </summary>
    private int EstimateTokens<T>(T result)
    {
        if (result == null)
            return 0;

        if (result is string str)
        {
            // Grobe Schätzung: ~4 Zeichen pro Token
            return str.Length / 4;
        }

        if (result is Array arr)
        {
            return arr.Length * 10; // Grobe Schätzung
        }

        // Für Objekte: ToString() verwenden
        var strValue = result.ToString();
        return (strValue?.Length ?? 0) / 4;
    }

    /// <summary>
    /// Loggt Token-Usage explizit (wenn von API zurückgegeben)
    /// </summary>
    public void LogTokenUsage(
        string operation,
        string provider,
        string model,
        int inputTokens,
        int outputTokens,
        decimal cost = 0)
    {
        _logger.LogInformation(
            "AI_TOKEN_USAGE | Operation: {Operation} | Provider: {Provider} | Model: {Model} | " +
            "InputTokens: {InputTokens} | OutputTokens: {OutputTokens} | TotalTokens: {TotalTokens} | Cost: ${Cost:F6}",
            operation, provider, model, inputTokens, outputTokens, inputTokens + outputTokens, cost
        );
    }

    /// <summary>
    /// Berechnet geschätzte Kosten basierend auf Provider und Token-Count
    /// </summary>
    public static decimal CalculateCost(string provider, string model, int inputTokens, int outputTokens)
    {
        // Kosten pro 1M Tokens (Stand Januar 2026)
        var pricing = (provider.ToLower(), model.ToLower()) switch
        {
            ("openai", "gpt-5-mini") => (input: 0.30m, output: 1.20m),  // $0.30/$1.20 per 1M tokens
            ("openai", "gpt-4o") => (input: 2.50m, output: 10.00m),
            ("anthropic", var m) when m.Contains("claude-sonnet") => (input: 3.00m, output: 15.00m),
            ("anthropic", var m) when m.Contains("claude-haiku") => (input: 0.25m, output: 1.25m),
            ("gemini", "gemini-3-flash") => (input: 0.075m, output: 0.30m),
            _ => (input: 0m, output: 0m)
        };

        var inputCost = (inputTokens / 1_000_000m) * pricing.input;
        var outputCost = (outputTokens / 1_000_000m) * pricing.output;

        return inputCost + outputCost;
    }
}

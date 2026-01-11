using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Shared.Helpers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;
using System.Text;
using System.Text.Json;
// TODO: Restore PdfPig when version conflicts are resolved - using itext7 instead
// using UglyToad.PdfPig;
// using UglyToad.PdfPig.Content;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DHBWAutomation.Backend.Core.Services;

public class DocumentParsingService : IDocumentParsingService
{
    private readonly IAIService _aiService;
    private readonly ILogger<DocumentParsingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiMetrics _aiMetrics;
    private readonly string? _geminiApiKey;

    private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string GeminiModel = "gemini-3-flash-preview";

    // Polly Resilience Policies for Gemini (60 requests per minute)
    private static readonly RateLimiter _geminiLimiter = new(60, TimeSpan.FromMinutes(1));
    
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _geminiRetryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && (int)r.StatusCode == 429)
        .Or<HttpRequestException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"[Gemini Retry] Attempt {retryCount} after {timespan.TotalSeconds}s");
            });

    private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _geminiCircuitBreaker = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1),
            onBreak: (outcome, duration) =>
            {
                Console.WriteLine($"[Gemini Circuit Breaker] OPEN for {duration.TotalSeconds}s");
            },
            onReset: () => Console.WriteLine("[Gemini Circuit Breaker] RESET"),
            onHalfOpen: () => Console.WriteLine("[Gemini Circuit Breaker] HALF-OPEN"));

    private static readonly AsyncPolicyWrap<HttpResponseMessage> _geminiResiliencePolicy =
        _geminiRetryPolicy.WrapAsync(_geminiCircuitBreaker);

    public DocumentParsingService(
        IAIService aiService,
        ILogger<DocumentParsingService> logger,
        IHttpClientFactory httpClientFactory,
        AiMetrics aiMetrics)
    {
        _aiService = aiService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _aiMetrics = aiMetrics;
        _geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        try
        {
            _logger.LogInformation("Starting PDF text extraction");

            // Reset stream position
            if (pdfStream.CanSeek)
            {
                pdfStream.Position = 0;
            }

            var textBuilder = new StringBuilder();

            // Extract text using iText7 (replaced PdfPig due to version conflicts)
            using (var pdfReader = new PdfReader(pdfStream))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    var page = pdfDocument.GetPage(pageNum);
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    textBuilder.AppendLine(pageText);
                }
            }

            var extractedText = textBuilder.ToString().Trim();

            // If very little text was extracted, it might be a scanned PDF
            // Fall back to Gemini OCR
            if (extractedText.Length < 100)
            {
                _logger.LogInformation("PDF contains little text, attempting OCR with Gemini");

                // Reset stream again for OCR
                if (pdfStream.CanSeek)
                {
                    pdfStream.Position = 0;
                }

                // For now, we'll just return what we have
                // In a production system, we'd convert PDF pages to images and use Gemini OCR
                _logger.LogWarning("PDF appears to be scanned but OCR fallback not fully implemented");
                return extractedText.Length > 0 ? extractedText : "Keine Textinhalte extrahiert (Scan-PDF?)";
            }

            _logger.LogInformation($"Extracted {extractedText.Length} characters from PDF");
            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF");
            throw new InvalidOperationException("Fehler beim Extrahieren von Text aus PDF", ex);
        }
    }

    public async Task<string> ExtractTextFromDocxAsync(Stream docxStream)
    {
        try
        {
            _logger.LogInformation("Starting DOCX text extraction");

            // Reset stream position
            if (docxStream.CanSeek)
            {
                docxStream.Position = 0;
            }

            var textBuilder = new StringBuilder();

            using (var document = WordprocessingDocument.Open(docxStream, false))
            {
                if (document.MainDocumentPart?.Document?.Body != null)
                {
                    var body = document.MainDocumentPart.Document.Body;

                    // Extract all text elements
                    foreach (var element in body.Descendants<Paragraph>())
                    {
                        var paragraphText = element.InnerText;
                        if (!string.IsNullOrWhiteSpace(paragraphText))
                        {
                            textBuilder.AppendLine(paragraphText);
                        }
                    }

                    // Extract text from tables
                    foreach (var table in body.Descendants<Table>())
                    {
                        foreach (var row in table.Descendants<TableRow>())
                        {
                            var rowText = string.Join(" | ", row.Descendants<TableCell>().Select(c => c.InnerText.Trim()));
                            if (!string.IsNullOrWhiteSpace(rowText))
                            {
                                textBuilder.AppendLine(rowText);
                            }
                        }
                    }
                }
            }

            var extractedText = textBuilder.ToString().Trim();
            _logger.LogInformation($"Extracted {extractedText.Length} characters from DOCX");

            return extractedText.Length > 0 ? extractedText : "Keine Textinhalte im DOCX gefunden";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from DOCX");
            throw new InvalidOperationException("Fehler beim Extrahieren von Text aus DOCX", ex);
        }
    }

    public async Task<string> ExtractTextFromImageAsync(Stream imageStream)
    {
        return await _aiMetrics.TrackAsync("OCR", "Gemini", GeminiModel, async () =>
        {
            try
            {
                _logger.LogInformation("Starting image OCR with Gemini 3 Flash");

                if (string.IsNullOrEmpty(_geminiApiKey))
                {
                    _logger.LogWarning("Gemini API Key not configured");
                    throw new InvalidOperationException("Gemini API Key nicht konfiguriert");
                }

                // Reset stream position
                if (imageStream.CanSeek)
                {
                    imageStream.Position = 0;
                }

                // Convert image to base64
                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await imageStream.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }
                var base64Image = Convert.ToBase64String(imageBytes);

                // Determine MIME type (simplified)
                var mimeType = "image/jpeg"; // Default
                if (base64Image.StartsWith("iVBOR"))
                    mimeType = "image/png";
                else if (base64Image.StartsWith("R0lGOD"))
                    mimeType = "image/gif";

                // Call Gemini Vision API for OCR with Polly resilience
                var requestUrl = $"{GeminiEndpoint}/{GeminiModel}:generateContent?key={_geminiApiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = "Extrahiere ALLEN Text aus diesem Bild. Erkenne handgeschriebenen und gedruckten Text. Gib NUR den extrahierten Text zurück, keine Beschreibung. Wenn es sich um mathematische Formeln handelt, schreibe sie in LaTeX-Syntax." },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = mimeType,
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        maxOutputTokens = 4096
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Execute with Rate Limiting + Retry + Circuit Breaker
                var response = await _geminiLimiter.ExecuteAsync(async () =>
                {
                    var client = _httpClientFactory.CreateClient("Gemini");
                    return await _geminiResiliencePolicy.ExecuteAsync(async () =>
                        await client.PostAsync(requestUrl, content));
                });

                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(responseJson);

                var extractedText = result.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Kein Text erkannt";

                _logger.LogInformation($"OCR extracted {extractedText.Length} characters from image");
                return extractedText;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Gemini Circuit Breaker is OPEN - service unavailable");
                throw new InvalidOperationException("Gemini OCR service temporarily unavailable", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from image with Gemini OCR");
                throw new InvalidOperationException("Fehler beim OCR mit Gemini", ex);
            }
        });
    }

    public async Task<(string ExtractedText, string[]? DetectedErrors)> ExtractAndAnalyzeAsync(
        Stream fileStream,
        string fileType,
        bool analyzeErrors = false)
    {
        try
        {
            _logger.LogInformation($"Extracting and analyzing file of type: {fileType}");

            string extractedText;

            // Determine extraction method based on file type
            if (fileType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            {
                extractedText = await ExtractTextFromPdfAsync(fileStream);
            }
            else if (fileType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) ||
                     fileType.Contains("docx", StringComparison.OrdinalIgnoreCase))
            {
                extractedText = await ExtractTextFromDocxAsync(fileStream);
            }
            else if (fileType.Contains("image", StringComparison.OrdinalIgnoreCase) ||
                     fileType.Contains("png", StringComparison.OrdinalIgnoreCase) ||
                     fileType.Contains("jpg", StringComparison.OrdinalIgnoreCase) ||
                     fileType.Contains("jpeg", StringComparison.OrdinalIgnoreCase))
            {
                extractedText = await ExtractTextFromImageAsync(fileStream);
            }
            else if (fileType.Contains("text", StringComparison.OrdinalIgnoreCase) ||
                     fileType.Contains("plain", StringComparison.OrdinalIgnoreCase))
            {
                // Plain text - just read directly
                _logger.LogInformation($"Extracting plain text - FileType: {fileType}");
                _logger.LogInformation($"Stream CanSeek: {fileStream.CanSeek}, Position: {fileStream.Position}, Length: {fileStream.Length}");

                if (fileStream.CanSeek)
                {
                    fileStream.Position = 0;
                    _logger.LogInformation($"Stream position reset to 0");
                }

                using var reader = new StreamReader(fileStream);
                extractedText = await reader.ReadToEndAsync();
                _logger.LogInformation($"Text extracted: {extractedText.Length} characters");
            }
            else
            {
                _logger.LogWarning($"Unsupported file type for text extraction: {fileType}");
                extractedText = "Dateityp wird nicht für Textextraktion unterstützt";
            }

            // Optionally analyze for errors (will be implemented in IntentAnalysisService)
            string[]? detectedErrors = null;
            if (analyzeErrors && !string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogInformation("Error analysis will be handled by IntentAnalysisService");
                // Error detection will be done by IntentAnalysisService with Claude Sonnet 4.5
            }

            return (extractedText, detectedErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExtractAndAnalyzeAsync");
            throw;
        }
    }
}

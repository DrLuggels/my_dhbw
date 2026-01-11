using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
// TODO: Restore PdfPig when version conflicts are resolved
// using UglyToad.PdfPig;
// using UglyToad.PdfPig.Content;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for extracting images from PDFs and analyzing them with Gemini
/// </summary>
public class PdfImageExtractionService : IPdfImageExtractionService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmbeddingService _embeddingService;
    private readonly EncryptionHelper _encryptionHelper;
    private readonly ILogger<PdfImageExtractionService> _logger;

    private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string GeminiModel = "gemini-2.0-flash"; // Vision-capable model
    private const string ImagesBucket = "dhbw-images";

    public PdfImageExtractionService(
        AppDbContext context,
        IStorageService storageService,
        IHttpClientFactory httpClientFactory,
        IEmbeddingService embeddingService,
        EncryptionHelper encryptionHelper,
        ILogger<PdfImageExtractionService> logger)
    {
        _context = context;
        _storageService = storageService;
        _httpClientFactory = httpClientFactory;
        _embeddingService = embeddingService;
        _encryptionHelper = encryptionHelper;
        _logger = logger;
    }

    /// <summary>
    /// Extract images from a PDF document
    /// </summary>
    public async Task<List<DocumentImage>> ExtractImagesFromDocumentAsync(int documentId)
    {
        var extractedImages = new List<DocumentImage>();

        try
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return extractedImages;
            }

            if (!document.FileType.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Document {DocumentId} is not a PDF", documentId);
                return extractedImages;
            }

            // Download PDF from storage (documents are stored in dhbw-files bucket)
            var pdfStream = await _storageService.DownloadFileAsync(document.FilePath, "dhbw-files");
            if (pdfStream == null)
            {
                _logger.LogWarning("Could not download PDF for document {DocumentId}", documentId);
                return extractedImages;
            }

            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // TODO: Restore PdfPig image extraction when version conflicts are resolved
            _logger.LogWarning("PDF image extraction temporarily disabled due to PdfPig version conflicts");

            /* TEMPORARILY DISABLED - PdfPig version conflict
            // Extract images using PdfPig
            using var pdfDocument = PdfDocument.Open(memoryStream);
            var imageIndex = 0;

            foreach (var page in pdfDocument.GetPages())
            {
                var pageImages = page.GetImages();

                foreach (var image in pageImages)
                {
                    try
                    {
                        var imageBytes = image.RawBytes.ToArray();
                        if (imageBytes.Length < 1000) continue; // Skip tiny images

                        // Determine format
                        var format = DetermineImageFormat(imageBytes);
                        var fileName = $"doc_{documentId}_p{page.Number}_i{imageIndex}.{format}";
                        var storagePath = $"documents/{documentId}/{fileName}";

                        // Upload to MinIO
                        using var imageStream = new MemoryStream(imageBytes);
                        await _storageService.UploadFileAsync(
                            imageStream,
                            storagePath,
                            ImagesBucket
                        );

                        var documentImage = new DocumentImage
                        {
                            DocumentId = documentId,
                            PageNumber = page.Number,
                            ImageIndex = imageIndex,
                            StoragePath = storagePath,
                            FileName = fileName,
                            ImageFormat = format,
                            Width = (int)(image.Bounds.Width),
                            Height = (int)(image.Bounds.Height),
                            FileSize = imageBytes.Length
                        };

                        _context.DocumentImages.Add(documentImage);
                        extractedImages.Add(documentImage);
                        imageIndex++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error extracting image {Index} from page {Page}",
                            imageIndex, page.Number);
                    }
                }
            }
            */

            // Update document
            document.ImageCount = extractedImages.Count;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Extracted {Count} images from document {DocumentId}",
                extractedImages.Count, documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting images from document {DocumentId}", documentId);
        }

        return extractedImages;
    }

    /// <summary>
    /// Analyze an image with Gemini Vision
    /// </summary>
    public async Task<ImageAnalysisResult> AnalyzeImageWithGeminiAsync(int imageId, int? userId = null)
    {
        var result = new ImageAnalysisResult();

        try
        {
            var image = await _context.DocumentImages
                .Include(i => i.Document)
                .FirstOrDefaultAsync(i => i.Id == imageId);

            if (image == null)
            {
                result.Error = "Image not found";
                return result;
            }

            // Get Gemini API key
            var geminiKey = await GetGeminiApiKeyAsync(userId ?? image.Document.UserId);
            if (string.IsNullOrEmpty(geminiKey))
            {
                result.Error = "Gemini API key not configured";
                return result;
            }

            // Download image from storage
            var imageStream = await _storageService.DownloadFileAsync(image.StoragePath, ImagesBucket);
            if (imageStream == null)
            {
                result.Error = "Could not download image";
                return result;
            }

            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            // Call Gemini Vision API
            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{GeminiEndpoint}/{GeminiModel}:generateContent?key={geminiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                text = @"Analysiere dieses Bild aus einem Studiendokument. Beschreibe:
1. Was zeigt das Bild? (Diagramm, Chart, Foto, Screenshot, Formel, Tabelle, etc.)
2. Was ist der Hauptinhalt/die Hauptaussage?
3. Extrahiere jeglichen Text der sichtbar ist.
4. Welche Konzepte oder Themen werden dargestellt?

Antworte auf Deutsch und strukturiert."
                            },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = $"image/{image.ImageFormat}",
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 1000
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(requestUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini API error: {Status} - {Content}",
                    response.StatusCode, errorContent);
                result.Error = $"Gemini API error: {response.StatusCode}";
                return result;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResult = JsonDocument.Parse(responseJson);

            var analysisText = geminiResult.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Parse the response
            result.Description = analysisText;
            result.ImageType = DetermineImageType(analysisText);
            result.ExtractedText = ExtractTextFromAnalysis(analysisText);

            // Update database
            image.GeminiDescription = result.Description;
            image.ExtractedText = result.ExtractedText;
            image.ImageType = result.ImageType;
            image.IsProcessed = true;
            image.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate embedding for the description
            try
            {
                await _embeddingService.ProcessImageEmbeddingAsync(imageId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for image {ImageId}", imageId);
            }

            result.Success = true;
            _logger.LogInformation("Analyzed image {ImageId} with Gemini", imageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image {ImageId}", imageId);
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Process all unprocessed images for a document
    /// </summary>
    public async Task<int> ProcessDocumentImagesAsync(int documentId, int? userId = null)
    {
        var processedCount = 0;

        try
        {
            // First extract images if not done
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null) return 0;

            if (!document.ImagesProcessed && document.ImageCount == 0)
            {
                await ExtractImagesFromDocumentAsync(documentId);
            }

            // Get unprocessed images
            var unprocessedImages = await _context.DocumentImages
                .Where(i => i.DocumentId == documentId && !i.IsProcessed)
                .ToListAsync();

            foreach (var image in unprocessedImages)
            {
                var result = await AnalyzeImageWithGeminiAsync(image.Id, userId);
                if (result.Success)
                {
                    processedCount++;
                }

                // Rate limiting - wait a bit between requests
                await Task.Delay(500);
            }

            // Mark document as processed
            document.ImagesProcessed = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed {Count} images for document {DocumentId}",
                processedCount, documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing images for document {DocumentId}", documentId);
        }

        return processedCount;
    }

    /// <summary>
    /// Get images for a document
    /// </summary>
    public async Task<List<DocumentImage>> GetDocumentImagesAsync(int documentId)
    {
        return await _context.DocumentImages
            .Where(i => i.DocumentId == documentId)
            .OrderBy(i => i.PageNumber)
            .ThenBy(i => i.ImageIndex)
            .ToListAsync();
    }

    /// <summary>
    /// Get image download URL
    /// </summary>
    public async Task<string?> GetImageDownloadUrlAsync(int imageId)
    {
        var image = await _context.DocumentImages.FindAsync(imageId);
        if (image == null) return null;

        return await _storageService.GetFileUrlAsync(image.StoragePath, ImagesBucket, 60);
    }

    private async Task<string?> GetGeminiApiKeyAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null && !string.IsNullOrEmpty(user.GeminiApiKey))
        {
            return _encryptionHelper.Decrypt(user.GeminiApiKey);
        }
        return Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    private string DetermineImageFormat(byte[] imageBytes)
    {
        // Check magic bytes
        if (imageBytes.Length >= 8)
        {
            // PNG
            if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50)
                return "png";

            // JPEG
            if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                return "jpg";

            // GIF
            if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49)
                return "gif";
        }

        return "png"; // Default
    }

    private string DetermineImageType(string analysis)
    {
        var lowerAnalysis = analysis.ToLower();

        if (lowerAnalysis.Contains("diagramm") || lowerAnalysis.Contains("diagram"))
            return "diagram";
        if (lowerAnalysis.Contains("chart") || lowerAnalysis.Contains("grafik"))
            return "chart";
        if (lowerAnalysis.Contains("tabelle") || lowerAnalysis.Contains("table"))
            return "table";
        if (lowerAnalysis.Contains("formel") || lowerAnalysis.Contains("formula") || lowerAnalysis.Contains("gleichung"))
            return "formula";
        if (lowerAnalysis.Contains("screenshot"))
            return "screenshot";
        if (lowerAnalysis.Contains("foto") || lowerAnalysis.Contains("photo"))
            return "photo";

        return "other";
    }

    private string? ExtractTextFromAnalysis(string analysis)
    {
        // Try to find text extraction section
        var textMarkers = new[] { "Text:", "Extrahierter Text:", "Sichtbarer Text:", "Erkannter Text:" };

        foreach (var marker in textMarkers)
        {
            var index = analysis.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = index + marker.Length;
                var end = analysis.IndexOf('\n', start + 50); // Look for next section
                if (end < 0) end = Math.Min(start + 500, analysis.Length);

                return analysis.Substring(start, end - start).Trim();
            }
        }

        return null;
    }
}

/// <summary>
/// Result of image analysis
/// </summary>
public class ImageAnalysisResult
{
    public bool Success { get; set; }
    public string? Description { get; set; }
    public string? ImageType { get; set; }
    public string? ExtractedText { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Interface for PDF image extraction service
/// </summary>
public interface IPdfImageExtractionService
{
    Task<List<DocumentImage>> ExtractImagesFromDocumentAsync(int documentId);
    Task<ImageAnalysisResult> AnalyzeImageWithGeminiAsync(int imageId, int? userId = null);
    Task<int> ProcessDocumentImagesAsync(int documentId, int? userId = null);
    Task<List<DocumentImage>> GetDocumentImagesAsync(int documentId);
    Task<string?> GetImageDownloadUrlAsync(int imageId);
}

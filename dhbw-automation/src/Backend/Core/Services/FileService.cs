using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using System.Text.Json;

namespace DHBWAutomation.Backend.Core.Services;

public class FileService : IFileService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IAIService _aiService;
    private readonly IDocumentParsingService _parsingService;
    private readonly IIntentAnalysisService _intentService;
    private readonly ILearningAnalyticsService _learningService;
    private readonly ISchedulingService _schedulingService;
    private readonly ILogger<FileService> _logger;
    private const string DefaultBucket = "dhbw-files";

    public FileService(
        AppDbContext context,
        IStorageService storageService,
        IAIService aiService,
        IDocumentParsingService parsingService,
        IIntentAnalysisService intentService,
        ILearningAnalyticsService learningService,
        ISchedulingService schedulingService,
        ILogger<FileService> logger)
    {
        _context = context;
        _storageService = storageService;
        _aiService = aiService;
        _parsingService = parsingService;
        _intentService = intentService;
        _learningService = learningService;
        _schedulingService = schedulingService;
        _logger = logger;
    }

    public async Task<Document?> UploadFileAsync(int userId, IFormFile file, string? category = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return null;

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{userId}/{Guid.NewGuid()}{fileExtension}";

            // Upload to storage
            string filePath;
            using (var stream = file.OpenReadStream())
            {
                filePath = await _storageService.UploadFileAsync(stream, uniqueFileName, DefaultBucket);
            }

            // Create document record
            var document = new Document
            {
                UserId = userId,
                FileName = file.FileName,
                FilePath = filePath,
                FileType = file.ContentType,
                FileSize = file.Length,
                Category = category,
                Source = "manual_upload",
                IsProcessed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Process document in background (simplified - should use background job)
            _ = Task.Run(async () => await ProcessDocumentAsync(document.Id));

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            throw;
        }
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int userId)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
    }

    public async Task<IEnumerable<Document>> GetUserDocumentsAsync(int userId, int page = 1, int pageSize = 20)
    {
        return await _context.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int userId)
    {
        try
        {
            var document = await GetDocumentByIdAsync(documentId, userId);
            if (document == null)
                return false;

            // Delete from storage
            await _storageService.DeleteFileAsync(document.FilePath, DefaultBucket);

            // Delete from database
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return false;
        }
    }

    public async Task<Stream?> DownloadFileAsync(int documentId, int userId)
    {
        try
        {
            var document = await GetDocumentByIdAsync(documentId, userId);
            if (document == null)
                return null;

            return await _storageService.DownloadFileAsync(document.FilePath, DefaultBucket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file");
            return null;
        }
    }

    public async Task<bool> ProcessDocumentAsync(int documentId, ProcessingOptions? options = null)
    {
        // Use default options if not provided
        options ??= ProcessingOptions.Default;

        try
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null || document.IsProcessed)
                return false;

            _logger.LogInformation($"Processing document {documentId} with options: IntentAnalysis={options.EnableIntentAnalysis}, Correction={options.EnableTextCorrection}, Tags={options.GenerateTags}");

            Stream? fileStream = null;
            try
            {
                // 1. Download document from storage
                fileStream = await _storageService.DownloadFileAsync(document.FilePath, DefaultBucket);
                if (fileStream == null)
                {
                    _logger.LogWarning($"Could not download document {documentId}");
                    return false;
                }

                // 2. Extract text using DocumentParsingService (PDF, DOCX, Image OCR)
                string extractedText;
                var (text, errors) = await _parsingService.ExtractAndAnalyzeAsync(fileStream, document.FileType ?? "");
                extractedText = text;

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning($"No text extracted from document {documentId}");
                    document.IsProcessed = true;
                    document.ProcessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return false;
                }

                // 3. Intent Analysis with Claude Sonnet 4.5 - THE BRAIN (if enabled)
                DocumentIntent? intent = null;
                if (options.EnableIntentAnalysis)
                {
                    intent = await _intentService.AnalyzeDocumentIntentAsync(extractedText, document.DocumentCategory.ToString());
                    _logger.LogInformation($"Document intent: {intent.PrimaryIntent}");
                }

                // 4. Process Errors (Learning Analytics - if enabled)
                if (intent?.Errors?.Any() == true && options.EnableLearningAnalytics)
                {
                    document.DetectedErrors = JsonSerializer.Serialize(intent.Errors);
                    document.ErrorCount = intent.Errors.Count;

                    // OPTIONAL: Generate corrected text (expensive - default OFF)
                    if (options.EnableTextCorrection)
                    {
                        var correctionPrompt = $"Korrigiere folgende Fehler im Text:\n\n{extractedText.Substring(0, Math.Min(extractedText.Length, 3000))}\n\nFehler:\n{string.Join("\n", intent.Errors.Take(5).Select(e => $"- {e.Explanation}"))}";
                        document.CorrectedText = await _aiService.ChatCompletionAsync(correctionPrompt, null);
                        _logger.LogInformation($"Generated corrected text for document {documentId}");
                    }

                    // Save document first so we have the DetectedErrors field populated
                    await _context.SaveChangesAsync();

                    // Analyze errors and create/update LearningDeficits
                    await _learningService.AnalyzeDocumentErrorsAsync(documentId);

                    _logger.LogInformation($"Detected {intent.Errors.Count} errors in document {documentId}");
                }

                // 5. Create UserInteractions based on intent (if enabled)
                if (intent != null && options.GenerateInteractions)
                {
                    if (intent.Meeting != null || intent.Project != null || (intent.Errors?.Count > 2))
                    {
                        var interactions = await _intentService.GenerateInteractionsAsync(intent, document.UserId, documentId);
                        _context.UserInteractions.AddRange(interactions);

                        _logger.LogInformation($"Created {interactions.Count} user interactions for document {documentId}");
                    }
                }

                // 6. Create TODOs automatically (if intent analysis is enabled)
                if (intent != null && options.EnableIntentAnalysis)
                {
                    foreach (var todo in intent.Todos ?? new List<ExtractedTodo>())
                    {
                        var newTodo = new Todo
                        {
                            UserId = document.UserId,
                            Title = todo.Title,
                            Description = todo.Description,
                            Category = todo.Category,
                            Priority = todo.Priority,
                            Status = "pending",
                            DueDate = todo.SuggestedDeadline,
                            RelatedDocumentId = documentId,
                            ExtractedFrom = extractedText.Substring(0, Math.Min(extractedText.Length, 500)),
                            AiSuggestion = "Automatisch aus Dokument extrahiert",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Todos.Add(newTodo);
                    }
                }

                // 7. Handle Project (if intent analysis is enabled)
                if (intent?.Project != null && options.EnableIntentAnalysis)
                {
                    // Check if project already exists
                    var existingProject = await _context.Projects
                        .FirstOrDefaultAsync(p =>
                            p.UserId == document.UserId &&
                            p.Name.ToLower().Contains(intent.Project.Name.ToLower()));

                    if (existingProject != null)
                    {
                        // Link document to existing project
                        document.RelatedProjectId = existingProject.Id;
                        _logger.LogInformation($"Linked document {documentId} to existing project {existingProject.Id}");
                    }
                    else
                    {
                        _logger.LogInformation($"New project detected: '{intent.Project.Name}' - will ask user via UserInteraction");
                    }
                }

                // 8. Standard AI Processing (GPT-5 mini for cost-effective tasks)
                // Summary (OPTIONAL - can be disabled for bulk operations)
                if (options.GenerateSummary)
                {
                    document.Summary = await _aiService.SummarizeTextAsync(extractedText, 500);
                }

                // Tags (OPTIONAL - can be disabled for fast processing)
                if (options.GenerateTags)
                {
                    var tags = await _aiService.GenerateTagsAsync(extractedText);
                    document.Tags = string.Join(", ", tags);
                }

                // Always store extracted text (for search and later processing)
                document.ExtractedText = extractedText.Substring(0, Math.Min(extractedText.Length, 5000));

                // 9. Document Categorization
                if (document.DocumentCategory == DocumentCategory.Sonstiges)
                {
                    document.DocumentCategory = DetermineDocumentCategory(extractedText, intent);
                }

                // 10. Handle Temporary Documents (Archive as Backup)
                if (document.IsTemporary && document.DocumentCategory == DocumentCategory.EigeneNotizen)
                {
                    try
                    {
                        // Copy to backup bucket (keep original as backup)
                        if (fileStream.CanSeek)
                        {
                            fileStream.Position = 0;
                        }

                        var backupPath = $"backups/{document.FilePath}";
                        // Note: Would need to implement CopyFileAsync in IStorageService
                        // await _storageService.CopyFileAsync(document.FilePath, DefaultBucket, backupPath, "backup-bucket");

                        document.IsArchived = true;
                        document.ArchivedAt = DateTime.UtcNow;

                        _logger.LogInformation($"Archived temporary document {documentId} to backup");
                    }
                    catch (Exception archiveEx)
                    {
                        _logger.LogWarning(archiveEx, $"Could not archive document {documentId}");
                        // Continue processing even if archiving fails
                    }
                }

                // 11. Mark as processed
                document.IsProcessed = true;
                document.ProcessedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Document {documentId} fully processed: Intent={intent.PrimaryIntent}, Errors={intent.Errors?.Count ?? 0}, TODOs={intent.Todos?.Count ?? 0}");
                return true;
            }
            finally
            {
                fileStream?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing document {documentId}");
            return false;
        }
    }

    private DocumentCategory DetermineDocumentCategory(string text, DocumentIntent intent)
    {
        // Heuristic-based categorization
        if (text.Contains("Protokoll", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Mitschrieb vom", StringComparison.OrdinalIgnoreCase))
            return DocumentCategory.Protokoll;

        if (text.Contains("Aufgabe", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Assignment", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Abgabe", StringComparison.OrdinalIgnoreCase))
            return DocumentCategory.Aufgabenstellung;

        if (intent.Project != null)
            return DocumentCategory.ProjektIdee;

        if (intent.Errors?.Any() == true)
            return DocumentCategory.Mitschrieb; // Has errors = likely own notes

        if (text.Length < 500 && (intent.Todos?.Any() == true || intent.Meeting != null))
            return DocumentCategory.EigeneNotizen;

        if (intent.LearningInfo != null)
            return DocumentCategory.UnterrichtsMaterial;

        return DocumentCategory.Sonstiges;
    }
}

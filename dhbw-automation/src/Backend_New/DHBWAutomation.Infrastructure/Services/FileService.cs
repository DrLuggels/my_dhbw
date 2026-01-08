using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DHBWAutomation.Core.Interfaces;
using DHBWAutomation.Core.Models;
using DHBWAutomation.Infrastructure.Database;

namespace DHBWAutomation.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IAIService _aiService;
    private readonly ILogger<FileService> _logger;
    private const string DefaultBucket = "dhbw-files";

    public FileService(
        AppDbContext context,
        IStorageService storageService,
        IAIService aiService,
        ILogger<FileService> logger)
    {
        _context = context;
        _storageService = storageService;
        _aiService = aiService;
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

    public async Task<bool> ProcessDocumentAsync(int documentId)
    {
        try
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null || document.IsProcessed)
                return false;

            _logger.LogInformation($"Processing document {documentId}");

            // For now, just mark as processed
            // TODO: Extract text, analyze with AI, generate tags
            document.IsProcessed = true;
            document.ProcessedAt = DateTime.UtcNow;
            document.Summary = "Dokument wurde hochgeladen und verarbeitet.";
            
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing document {documentId}");
            return false;
        }
    }
}


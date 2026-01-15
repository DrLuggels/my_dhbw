using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Download-Funktionalität für Moodle-Ressourcen
/// </summary>
public partial class MoodleSyncService
{
    /// <summary>
    /// Lädt eine einzelne Moodle-Ressource herunter
    /// </summary>
    public async Task<MoodleDownloadResult> DownloadResourceAsync(int resourceId, int userId)
    {
        var result = new MoodleDownloadResult();

        try
        {
            var resource = await _context.MoodleResources
                .FirstOrDefaultAsync(r => r.Id == resourceId && r.UserId == userId);

            if (resource == null)
            {
                result.ErrorMessage = "Ressource nicht gefunden";
                return result;
            }

            if (resource.ResourceType != "file")
            {
                result.ErrorMessage = "Nur Datei-Ressourcen können heruntergeladen werden";
                return result;
            }

            if (string.IsNullOrEmpty(resource.DownloadUrl))
            {
                result.ErrorMessage = "Keine Download-URL verfügbar";
                return result;
            }

            if (resource.IsDownloaded && resource.LocalDocumentId.HasValue)
            {
                // Verify the document still exists
                var existingDoc = await _context.Documents.FindAsync(resource.LocalDocumentId.Value);
                if (existingDoc != null)
                {
                    result.Success = true;
                    result.DocumentId = resource.LocalDocumentId;
                    result.FileName = resource.Title;
                    result.ErrorMessage = "Bereits heruntergeladen";
                    return result;
                }
                // Document was deleted, reset download status
                resource.IsDownloaded = false;
                resource.LocalDocumentId = null;
            }

            // Check for duplicate by storage path
            var expectedPath = $"moodle/{userId}/{resource.CourseId}/{SanitizeFileName(resource.Title ?? $"moodle_{resourceId}")}";
            var duplicateByPath = await _context.Documents
                .FirstOrDefaultAsync(d => d.UserId == userId &&
                                         d.Source == "moodle" &&
                                         d.FilePath != null &&
                                         d.FilePath.Contains(SanitizeFileName(resource.Title ?? "")));

            if (duplicateByPath != null)
            {
                _logger.LogInformation("Found existing document {DocumentId} for resource {ResourceId}, linking instead of re-downloading",
                    duplicateByPath.Id, resourceId);
                resource.IsDownloaded = true;
                resource.LocalDocumentId = duplicateByPath.Id;
                resource.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                result.Success = true;
                result.DocumentId = duplicateByPath.Id;
                result.FileName = duplicateByPath.FileName;
                result.ErrorMessage = "Duplikat gefunden und verknuepft";
                return result;
            }

            // Get user token
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.MoodleToken))
            {
                result.ErrorMessage = "Moodle nicht konfiguriert";
                return result;
            }

            var token = _encryptionHelper.Decrypt(user.MoodleToken);
            _moodleClient.SetToken(token);

            // Download file from Moodle
            _logger.LogInformation("Downloading resource {ResourceId}: {Title}", resourceId, resource.Title);
            var downloadResult = await _moodleClient.DownloadFileAsync(resource.DownloadUrl);

            if (!downloadResult.Success || downloadResult.Content == null || downloadResult.Content.Length == 0)
            {
                result.ErrorMessage = downloadResult.ErrorMessage ?? "Download fehlgeschlagen";
                return result;
            }

            // Create document record
            var fileName = SanitizeFileName(resource.Title ?? $"moodle_{resourceId}");
            var fileExtension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(fileExtension) && !string.IsNullOrEmpty(resource.FileType))
            {
                fileExtension = GetExtensionFromMimeType(resource.FileType);
                fileName += fileExtension;
            }

            var storagePath = $"moodle/{userId}/{resource.CourseId}/{fileName}";

            // Upload to MinIO using scope
            if (_serviceProvider != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var storageService = scope.ServiceProvider.GetService<IStorageService>();

                if (storageService != null)
                {
                    using var memoryStream = new MemoryStream(downloadResult.Content);
                    await storageService.UploadFileAsync(memoryStream, storagePath, "dhbw-files");
                }
            }

            // Create document
            var document = new Document
            {
                UserId = userId,
                FileName = fileName,
                FilePath = storagePath,
                FileType = fileExtension.TrimStart('.'),
                FileSize = downloadResult.Content.Length,
                Category = DetermineCategory(resource.CourseName, resource.Title),
                Subject = ExtractSubject(resource.CourseName),
                Source = "moodle",
                IsProcessed = false,
                IsChunked = false,
                HasEmbedding = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Update resource
            resource.IsDownloaded = true;
            resource.LocalDocumentId = document.Id;
            resource.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Downloaded resource {ResourceId} as document {DocumentId}", resourceId, document.Id);

            result.Success = true;
            result.DocumentId = document.Id;
            result.FileName = fileName;
            result.FileSize = downloadResult.Content.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading resource {ResourceId}", resourceId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Lädt alle nicht heruntergeladenen Datei-Ressourcen herunter
    /// </summary>
    public async Task<MoodleBatchDownloadResult> DownloadAllResourcesAsync(int userId, bool processAfterDownload = true)
    {
        var result = new MoodleBatchDownloadResult();

        try
        {
            // Get all file resources that haven't been downloaded
            var resources = await _context.MoodleResources
                .Where(r => r.UserId == userId &&
                           r.ResourceType == "file" &&
                           !r.IsDownloaded &&
                           r.DownloadUrl != null)
                .ToListAsync();

            result.TotalResources = resources.Count;
            _logger.LogInformation("Starting batch download of {Count} resources for user {UserId}",
                resources.Count, userId);

            foreach (var resource in resources)
            {
                try
                {
                    var downloadResult = await DownloadResourceAsync(resource.Id, userId);

                    if (downloadResult.Success)
                    {
                        result.DownloadedCount++;
                        if (downloadResult.DocumentId.HasValue)
                        {
                            result.CreatedDocumentIds.Add(downloadResult.DocumentId.Value);
                        }
                    }
                    else if (downloadResult.ErrorMessage?.Contains("Bereits") == true)
                    {
                        result.SkippedCount++;
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Errors.Add($"{resource.Title}: {downloadResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add($"{resource.Title}: {ex.Message}");
                    _logger.LogError(ex, "Error downloading resource {ResourceId}", resource.Id);
                }

                // Small delay to avoid overwhelming Moodle
                await Task.Delay(500);
            }

            // Process documents for embeddings if requested
            if (processAfterDownload && result.CreatedDocumentIds.Any() && _serviceProvider != null)
            {
                _logger.LogInformation("Processing {Count} documents for embeddings", result.CreatedDocumentIds.Count);

                using var scope = _serviceProvider.CreateScope();
                var fileService = scope.ServiceProvider.GetService<IFileService>();

                if (fileService != null)
                {
                    foreach (var docId in result.CreatedDocumentIds)
                    {
                        try
                        {
                            await fileService.ProcessDocumentAsync(docId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing document {DocumentId}", docId);
                        }
                    }
                }
            }

            result.Success = result.FailedCount == 0 || result.DownloadedCount > 0;
            _logger.LogInformation("Batch download completed: {Downloaded} downloaded, {Failed} failed, {Skipped} skipped",
                result.DownloadedCount, result.FailedCount, result.SkippedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch download for user {UserId}", userId);
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 200 ? sanitized[..200] : sanitized;
    }

    private static string GetExtensionFromMimeType(string mimeType)
    {
        return mimeType.ToLower() switch
        {
            "application/pdf" => ".pdf",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
            "application/vnd.ms-powerpoint" => ".ppt",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            "application/vnd.ms-excel" => ".xls",
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "text/plain" => ".txt",
            "text/html" => ".html",
            _ => ""
        };
    }

    private static string DetermineCategory(string? courseName, string? title)
    {
        var combined = $"{courseName} {title}".ToLower();

        if (combined.Contains("übung") || combined.Contains("aufgabe") || combined.Contains("exercise"))
            return "exercise";
        if (combined.Contains("vorlesung") || combined.Contains("lecture") || combined.Contains("skript"))
            return "lecture";
        if (combined.Contains("klausur") || combined.Contains("prüfung") || combined.Contains("exam"))
            return "exam";
        if (combined.Contains("zusammenfassung") || combined.Contains("summary"))
            return "summary";

        return "course_material";
    }

    private static string ExtractSubject(string? courseName)
    {
        if (string.IsNullOrEmpty(courseName)) return "Allgemein";

        // Extract subject from course name patterns like "WDS125 - Grundlagen DSKI"
        var parts = courseName.Split(new[] { " - ", " – " }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
            return parts[1].Trim();

        return courseName.Length > 50 ? courseName[..50] : courseName;
    }
}

using DHBWAutomation.Backend.Core.Models;
using Microsoft.AspNetCore.Http;

namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IFileService
{
    Task<Document?> UploadFileAsync(int userId, IFormFile file, string? category = null);
    Task<Document?> GetDocumentByIdAsync(int documentId, int userId);
    Task<IEnumerable<Document>> GetUserDocumentsAsync(int userId, int page = 1, int pageSize = 20);
    Task<bool> DeleteDocumentAsync(int documentId, int userId);
    Task<(int successCount, int failureCount)> BulkDeleteDocumentsAsync(IEnumerable<int> documentIds, int userId);
    Task<Stream?> DownloadFileAsync(int documentId, int userId);
    Task<bool> ProcessDocumentAsync(int documentId, ProcessingOptions? options = null);
}

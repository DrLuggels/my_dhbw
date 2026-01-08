namespace DHBWAutomation.Backend.Core.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string bucketName);
    Task<Stream> DownloadFileAsync(string filePath, string bucketName);
    Task<bool> DeleteFileAsync(string filePath, string bucketName);
    Task<bool> FileExistsAsync(string filePath, string bucketName);
    Task<string> GetFileUrlAsync(string filePath, string bucketName, int expiryMinutes = 60);
}

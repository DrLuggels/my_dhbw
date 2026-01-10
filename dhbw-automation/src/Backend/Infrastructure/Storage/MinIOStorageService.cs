using Minio;
using Minio.DataModel.Args;
using DHBWAutomation.Backend.Core.Interfaces;

namespace DHBWAutomation.Backend.Infrastructure.Storage;

public class MinIOStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinIOStorageService> _logger;

    public MinIOStorageService(ILogger<MinIOStorageService> logger)
    {
        _logger = logger;
        
        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "localhost:9000";
        var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? "minioadmin";
        var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? "minioadmin";
        var useSSL = Environment.GetEnvironmentVariable("MINIO_USE_SSL") == "true";

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSSL)
            .Build();
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string bucketName)
    {
        try
        {
            // Ensure bucket exists
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
            var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
            
            if (!bucketExists)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
                _logger.LogInformation($"Created bucket: {bucketName}");
            }

            // CRITICAL: Reset stream position to 0 before upload!
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
                _logger.LogInformation($"Reset stream position to 0 before upload. Stream length: {fileStream.Length}");
            }

            // Upload file
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length);

            await _minioClient.PutObjectAsync(putObjectArgs);
            
            _logger.LogInformation($"Uploaded file: {fileName} to bucket: {bucketName}");
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file {fileName} to MinIO");
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string filePath, string bucketName)
    {
        try
        {
            _logger.LogInformation($"Downloading file from MinIO: Bucket={bucketName}, Path={filePath}");

            // First verify the file exists and get its metadata
            var statArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(filePath);

            var stat = await _minioClient.StatObjectAsync(statArgs);
            _logger.LogInformation($"File exists in MinIO: Size={stat.Size} bytes, ContentType={stat.ContentType}");

            // Create MemoryStream to hold the downloaded data
            var memoryStream = new MemoryStream();

            // Download the file using callback (MUST be synchronous!)
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(filePath)
                .WithCallbackStream(stream =>
                {
                    _logger.LogInformation($"✓ MinIO callback invoked successfully!");
                    _logger.LogInformation($"Stream: CanRead={stream.CanRead}, CanSeek={stream.CanSeek}");

                    try
                    {
                        var length = stream.Length;
                        _logger.LogInformation($"Stream Length: {length} bytes");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Could not read stream length: {ex.Message}");
                    }

                    _logger.LogInformation("Copying stream to MemoryStream...");
                    stream.CopyTo(memoryStream);  // SYNCHRONOUS copy!
                    _logger.LogInformation($"✓ Successfully copied {memoryStream.Length} bytes to MemoryStream");
                }));

            memoryStream.Position = 0;
            _logger.LogInformation($"Download complete: Final MemoryStream Length={memoryStream.Length}");

            if (memoryStream.Length == 0)
            {
                _logger.LogWarning($"WARNING: Downloaded stream is EMPTY despite file size being {stat.Size}!");
            }

            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading file {filePath} from MinIO");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath, string bucketName)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(filePath);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            
            _logger.LogInformation($"Deleted file: {filePath} from bucket: {bucketName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file {filePath} from MinIO");
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath, string bucketName)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(filePath);

            await _minioClient.StatObjectAsync(statObjectArgs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(string filePath, string bucketName, int expiryMinutes = 60)
    {
        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(filePath)
                .WithExpiry(expiryMinutes * 60);

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating presigned URL for {filePath}");
            throw;
        }
    }
}

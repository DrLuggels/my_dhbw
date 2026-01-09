using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.DTOs.Responses;
using DHBWAutomation.Backend.API.Filters;

namespace DHBWAutomation.Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(100_000_000)] // 100 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
    [ServiceFilter(typeof(RequestLoggingFilter))]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadFile(
        [FromForm] IFormFile file,
        [FromForm] string? category = null)
    {
        _logger.LogInformation("========== FILE UPLOAD REQUEST STARTED ==========");
        _logger.LogInformation("Request ContentType: {ContentType}", Request.ContentType);
        _logger.LogInformation("Request ContentLength: {ContentLength}", Request.ContentLength);
        _logger.LogInformation("Request HasFormContentType: {HasFormContentType}", Request.HasFormContentType);
        _logger.LogInformation("Category parameter: {Category}", category ?? "null");
        
        try
        {
            _logger.LogInformation("Step 1: Checking ModelState validity");
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            _logger.LogInformation("ModelState.ErrorCount: {ErrorCount}", ModelState.ErrorCount);
            
            // Log all ModelState errors in detail
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is INVALID - Dumping all errors:");
                foreach (var entry in ModelState)
                {
                    var key = entry.Key;
                    var state = entry.Value;
                    _logger.LogWarning("  Key: {Key}, ValidationState: {ValidationState}, ErrorCount: {ErrorCount}", 
                        key, state.ValidationState, state.Errors.Count);
                    
                    foreach (var error in state.Errors)
                    {
                        _logger.LogWarning("    Error Message: {ErrorMessage}", error.ErrorMessage);
                        _logger.LogWarning("    Exception: {Exception}", error.Exception?.Message ?? "null");
                    }
                }
                
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage ?? e.Exception?.Message ?? "Unknown error")
                    .ToArray();
                    
                _logger.LogError("Model binding failed with errors: {Errors}", string.Join("; ", errorMessages));
                
                return BadRequest(new ApiResponse<DocumentResponse>
                {
                    Success = false,
                    Message = "Validierungsfehler",
                    Errors = errorMessages
                });
            }
            
            _logger.LogInformation("Step 2: Checking file parameter");
            _logger.LogInformation("File is null: {FileIsNull}", file == null);
            
            if (file == null || file.Length == 0)
            {
                _logger.LogError("UPLOAD FAILED: File is null or empty");
                _logger.LogError("  File is null: {FileIsNull}", file == null);
                _logger.LogError("  File length: {Length}", file?.Length ?? 0);
                
                return BadRequest(new ApiResponse<DocumentResponse>
                {
                    Success = false,
                    Message = "Keine Datei hochgeladen",
                    Errors = new[] { "Die Datei ist leer oder fehlt" }
                });
            }
            
            _logger.LogInformation("Step 3: File received successfully");
            _logger.LogInformation("  FileName: {FileName}", file.FileName);
            _logger.LogInformation("  ContentType: {ContentType}", file.ContentType);
            _logger.LogInformation("  Length: {Length} bytes", file.Length);
            _logger.LogInformation("  Name (form field): {Name}", file.Name);

            _logger.LogInformation("Step 4: Getting userId (currently hardcoded)");
            // TODO: Get userId from JWT token
            int userId = 1; // Temporär hardcoded
            _logger.LogInformation("  UserId: {UserId}", userId);

            _logger.LogInformation("Step 5: Calling FileService.UploadFileAsync");
            var document = await _fileService.UploadFileAsync(userId, file, category);
            _logger.LogInformation("Step 6: FileService.UploadFileAsync completed");

            if (document == null)
            {
                _logger.LogError("Step 7: FileService returned NULL document");
                return BadRequest(new ApiResponse<DocumentResponse>
                {
                    Success = false,
                    Message = "Upload fehlgeschlagen",
                    Errors = new[] { "FileService returned null" }
                });
            }

            _logger.LogInformation("Step 7: Document created successfully");
            _logger.LogInformation("  Document ID: {DocumentId}", document.Id);
            _logger.LogInformation("  Document FileName: {FileName}", document.FileName);
            
            var response = new DocumentResponse
            {
                Id = document.Id,
                FileName = document.FileName,
                FileType = document.FileType,
                FileSize = document.FileSize,
                Category = document.Category,
                Subject = document.Subject,
                Summary = document.Summary,
                IsProcessed = document.IsProcessed,
                CreatedAt = document.CreatedAt
            };

            _logger.LogInformation("Step 8: Returning success response");
            _logger.LogInformation("========== FILE UPLOAD REQUEST COMPLETED SUCCESSFULLY ==========");
            
            return Ok(new ApiResponse<DocumentResponse>
            {
                Success = true,
                Data = response,
                Message = "Datei erfolgreich hochgeladen"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "========== FILE UPLOAD REQUEST FAILED WITH EXCEPTION ==========");
            _logger.LogError("Exception Type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("Exception Message: {Message}", ex.Message);
            _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner Exception Type: {InnerExceptionType}", ex.InnerException.GetType().Name);
                _logger.LogError("Inner Exception Message: {InnerMessage}", ex.InnerException.Message);
            }
            
            return StatusCode(500, new ApiResponse<DocumentResponse>
            {
                Success = false,
                Message = "Ein Fehler ist aufgetreten",
                Errors = new[] { ex.Message, ex.InnerException?.Message ?? "" }.Where(e => !string.IsNullOrEmpty(e)).ToArray()
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DocumentResponse>>>> GetDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // TODO: Get userId from JWT token
            int userId = 1; // Temporär hardcoded

            var documents = await _fileService.GetUserDocumentsAsync(userId, page, pageSize);

            var response = documents.Select(d => new DocumentResponse
            {
                Id = d.Id,
                FileName = d.FileName,
                FileType = d.FileType,
                FileSize = d.FileSize,
                Category = d.Category,
                Subject = d.Subject,
                Summary = d.Summary,
                IsProcessed = d.IsProcessed,
                CreatedAt = d.CreatedAt
            });

            return Ok(new ApiResponse<IEnumerable<DocumentResponse>>
            {
                Success = true,
                Data = response,
                Message = $"{documents.Count()} Dokumente gefunden"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der Dokumente");
            return StatusCode(500, new ApiResponse<IEnumerable<DocumentResponse>>
            {
                Success = false,
                Message = "Ein Fehler ist aufgetreten",
                Errors = new[] { ex.Message }
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> GetDocument(int id)
    {
        try
        {
            // TODO: Get userId from JWT token
            int userId = 1; // Temporär hardcoded

            var document = await _fileService.GetDocumentByIdAsync(id, userId);

            if (document == null)
            {
                return NotFound(new ApiResponse<DocumentResponse>
                {
                    Success = false,
                    Message = "Dokument nicht gefunden"
                });
            }

            var response = new DocumentResponse
            {
                Id = document.Id,
                FileName = document.FileName,
                FileType = document.FileType,
                FileSize = document.FileSize,
                Category = document.Category,
                Subject = document.Subject,
                Summary = document.Summary,
                IsProcessed = document.IsProcessed,
                CreatedAt = document.CreatedAt
            };

            return Ok(new ApiResponse<DocumentResponse>
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen des Dokuments");
            return StatusCode(500, new ApiResponse<DocumentResponse>
            {
                Success = false,
                Message = "Ein Fehler ist aufgetreten",
                Errors = new[] { ex.Message }
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDocument(int id)
    {
        try
        {
            // TODO: Get userId from JWT token
            int userId = 1; // Temporär hardcoded

            var success = await _fileService.DeleteDocumentAsync(id, userId);

            if (!success)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Dokument nicht gefunden oder konnte nicht gelöscht werden"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Dokument erfolgreich gelöscht"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen des Dokuments");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Ein Fehler ist aufgetreten",
                Errors = new[] { ex.Message }
            });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.DTOs.Responses;

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
    public async Task<ActionResult<ApiResponse<DocumentResponse>>> UploadFile(
        [FromForm] IFormFile file,
        [FromForm] string? category = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<DocumentResponse>
                {
                    Success = false,
                    Message = "Keine Datei hochgeladen",
                    Errors = new[] { "Die Datei ist leer oder fehlt" }
                });
            }

            // TODO: Get userId from JWT token
            int userId = 1; // Temporär hardcoded

            var document = await _fileService.UploadFileAsync(userId, file, category);

            if (document == null)
            {
                return BadRequest(new ApiResponse<DocumentResponse>
                {
                    Success = false,
                    Message = "Upload fehlgeschlagen"
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
                Data = response,
                Message = "Datei erfolgreich hochgeladen"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Datei-Upload");
            return StatusCode(500, new ApiResponse<DocumentResponse>
            {
                Success = false,
                Message = "Ein Fehler ist aufgetreten",
                Errors = new[] { ex.Message }
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

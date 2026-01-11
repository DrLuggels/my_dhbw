using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Services;
using System.Security.Claims;

namespace DHBWAutomation.Backend.API.Controllers;

/// <summary>
/// Controller for document images
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImagesController : ControllerBase
{
    private readonly IPdfImageExtractionService _imageService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        IPdfImageExtractionService imageService,
        ILogger<ImagesController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get images for a document
    /// </summary>
    [HttpGet("document/{documentId}")]
    public async Task<IActionResult> GetDocumentImages(int documentId)
    {
        try
        {
            var images = await _imageService.GetDocumentImagesAsync(documentId);

            return Ok(images.Select(i => new
            {
                id = i.Id,
                pageNumber = i.PageNumber,
                imageIndex = i.ImageIndex,
                imageType = i.ImageType,
                width = i.Width,
                height = i.Height,
                isProcessed = i.IsProcessed,
                geminiDescription = i.GeminiDescription,
                extractedText = i.ExtractedText
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images for document {DocumentId}", documentId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Extract images from a document
    /// </summary>
    [HttpPost("document/{documentId}/extract")]
    public async Task<IActionResult> ExtractImages(int documentId)
    {
        try
        {
            var images = await _imageService.ExtractImagesFromDocumentAsync(documentId);

            return Ok(new
            {
                success = true,
                extractedCount = images.Count,
                images = images.Select(i => new { id = i.Id, page = i.PageNumber })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting images from document {DocumentId}", documentId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Analyze a specific image with Gemini
    /// </summary>
    [HttpPost("{imageId}/analyze")]
    public async Task<IActionResult> AnalyzeImage(int imageId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _imageService.AnalyzeImageWithGeminiAsync(imageId, userId);

            return Ok(new
            {
                success = result.Success,
                description = result.Description,
                imageType = result.ImageType,
                extractedText = result.ExtractedText,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image {ImageId}", imageId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Process all images for a document
    /// </summary>
    [HttpPost("document/{documentId}/process")]
    public async Task<IActionResult> ProcessDocumentImages(int documentId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var processedCount = await _imageService.ProcessDocumentImagesAsync(documentId, userId);

            return Ok(new
            {
                success = true,
                processedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing images for document {DocumentId}", documentId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get download URL for an image
    /// </summary>
    [HttpGet("{imageId}/download")]
    public async Task<IActionResult> GetDownloadUrl(int imageId)
    {
        try
        {
            var url = await _imageService.GetImageDownloadUrlAsync(imageId);

            if (url == null)
            {
                return NotFound(new { message = "Image not found" });
            }

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download URL for image {ImageId}", imageId);
            return BadRequest(new { message = ex.Message });
        }
    }
}

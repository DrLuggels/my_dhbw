using Microsoft.AspNetCore.Mvc;
using DHBWAutomation.Backend.Core.Services;
using DHBWAutomation.Backend.Core.DTOs.Responses;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChunksController : ControllerBase
{
    private readonly IChunkingService _chunkingService;
    private readonly AppDbContext _context;
    private readonly ILogger<ChunksController> _logger;

    public ChunksController(
        IChunkingService chunkingService,
        AppDbContext context,
        ILogger<ChunksController> logger)
    {
        _chunkingService = chunkingService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all chunks for a document
    /// </summary>
    [HttpGet("/api/documents/{documentId}/chunks")]
    public async Task<ActionResult<ApiResponse<List<DocumentChunkDto>>>> GetDocumentChunks(int documentId)
    {
        try
        {
            // TODO: Get userId from JWT
            var userId = 1;

            var document = await _context.Documents.FindAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                return NotFound(new ApiResponse<List<DocumentChunkDto>>
                {
                    Success = false,
                    Message = "Document not found"
                });
            }

            var chunks = await _chunkingService.GetDocumentChunksAsync(documentId);

            var dtos = chunks.Select(c => new DocumentChunkDto
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                Content = c.Content,
                ContentLength = c.ContentLength,
                ChunkIndex = c.ChunkIndex,
                TotalChunks = c.TotalChunks,
                TopicLabel = c.TopicLabel,
                Summary = c.Summary,
                ChunkType = c.ChunkType,
                HasEmbedding = c.HasEmbedding,
                Status = c.Status,
                CreatedAt = c.CreatedAt
            }).ToList();

            return Ok(new ApiResponse<List<DocumentChunkDto>>
            {
                Success = true,
                Data = dtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chunks for document {DocumentId}", documentId);
            return StatusCode(500, new ApiResponse<List<DocumentChunkDto>>
            {
                Success = false,
                Message = "Failed to retrieve chunks"
            });
        }
    }

    /// <summary>
    /// Re-chunk a document
    /// </summary>
    [HttpPost("/api/documents/{documentId}/rechunk")]
    public async Task<ActionResult<ApiResponse<RechunkResultDto>>> RechunkDocument(
        int documentId,
        [FromBody] ChunkingOptionsDto? options = null)
    {
        try
        {
            // TODO: Get userId from JWT
            var userId = 1;

            var document = await _context.Documents.FindAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                return NotFound(new ApiResponse<RechunkResultDto>
                {
                    Success = false,
                    Message = "Document not found"
                });
            }

            var chunkingOptions = options != null
                ? new ChunkingOptions
                {
                    MinChunkSize = options.MinChunkSize ?? 200,
                    MaxChunkSize = options.MaxChunkSize ?? 2000,
                    TargetChunkSize = options.TargetChunkSize ?? 1000,
                    UseSemanticChunking = options.UseSemanticChunking ?? true,
                    GenerateEmbeddings = options.GenerateEmbeddings ?? true
                }
                : ChunkingOptions.Default;

            var chunkIds = await _chunkingService.ReChunkDocumentAsync(documentId, chunkingOptions);

            return Ok(new ApiResponse<RechunkResultDto>
            {
                Success = true,
                Data = new RechunkResultDto
                {
                    DocumentId = documentId,
                    ChunkCount = chunkIds.Count,
                    ChunkIds = chunkIds
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rechunking document {DocumentId}", documentId);
            return StatusCode(500, new ApiResponse<RechunkResultDto>
            {
                Success = false,
                Message = "Failed to rechunk document"
            });
        }
    }

    /// <summary>
    /// Batch rechunk multiple documents
    /// </summary>
    [HttpPost("batch-rechunk")]
    public async Task<ActionResult<ApiResponse<ChunkingBatchResult>>> BatchRechunk(
        [FromBody] BatchRechunkRequest request)
    {
        try
        {
            // TODO: Get userId from JWT
            var userId = 1;

            IEnumerable<int> documentIds;

            if (request.ProcessAllUnchunked)
            {
                documentIds = await _context.Documents
                    .Where(d => d.UserId == userId && d.IsProcessed && !d.IsChunked &&
                               d.ExtractedText != null && d.ExtractedText.Length > 500)
                    .Select(d => d.Id)
                    .ToListAsync();
            }
            else if (request.DocumentIds != null && request.DocumentIds.Count > 0)
            {
                // Verify user owns all documents
                var validIds = await _context.Documents
                    .Where(d => d.UserId == userId && request.DocumentIds.Contains(d.Id))
                    .Select(d => d.Id)
                    .ToListAsync();
                documentIds = validIds;
            }
            else
            {
                return BadRequest(new ApiResponse<ChunkingBatchResult>
                {
                    Success = false,
                    Message = "Either specify documentIds or set processAllUnchunked to true"
                });
            }

            var options = request.Options != null
                ? new ChunkingOptions
                {
                    MinChunkSize = request.Options.MinChunkSize ?? 200,
                    MaxChunkSize = request.Options.MaxChunkSize ?? 2000,
                    UseSemanticChunking = request.Options.UseSemanticChunking ?? true,
                    GenerateEmbeddings = request.Options.GenerateEmbeddings ?? true
                }
                : ChunkingOptions.Default;

            var result = await _chunkingService.ChunkDocumentsBatchAsync(documentIds, options);

            return Ok(new ApiResponse<ChunkingBatchResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch rechunk");
            return StatusCode(500, new ApiResponse<ChunkingBatchResult>
            {
                Success = false,
                Message = "Failed to batch rechunk documents"
            });
        }
    }

    /// <summary>
    /// Get a single chunk by ID
    /// </summary>
    [HttpGet("{chunkId}")]
    public async Task<ActionResult<ApiResponse<DocumentChunkDto>>> GetChunk(int chunkId)
    {
        try
        {
            // TODO: Get userId from JWT
            var userId = 1;

            var chunk = await _context.DocumentChunks
                .Include(c => c.Document)
                .FirstOrDefaultAsync(c => c.Id == chunkId);

            if (chunk == null || chunk.UserId != userId)
            {
                return NotFound(new ApiResponse<DocumentChunkDto>
                {
                    Success = false,
                    Message = "Chunk not found"
                });
            }

            return Ok(new ApiResponse<DocumentChunkDto>
            {
                Success = true,
                Data = new DocumentChunkDto
                {
                    Id = chunk.Id,
                    DocumentId = chunk.DocumentId,
                    Content = chunk.Content,
                    ContentLength = chunk.ContentLength,
                    ChunkIndex = chunk.ChunkIndex,
                    TotalChunks = chunk.TotalChunks,
                    TopicLabel = chunk.TopicLabel,
                    Summary = chunk.Summary,
                    ChunkType = chunk.ChunkType,
                    HasEmbedding = chunk.HasEmbedding,
                    Status = chunk.Status,
                    CreatedAt = chunk.CreatedAt,
                    DocumentFileName = chunk.Document.FileName
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chunk {ChunkId}", chunkId);
            return StatusCode(500, new ApiResponse<DocumentChunkDto>
            {
                Success = false,
                Message = "Failed to retrieve chunk"
            });
        }
    }

    /// <summary>
    /// Delete all chunks for a document
    /// </summary>
    [HttpDelete("/api/documents/{documentId}/chunks")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteDocumentChunks(int documentId)
    {
        try
        {
            // TODO: Get userId from JWT
            var userId = 1;

            var document = await _context.Documents.FindAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Document not found"
                });
            }

            var chunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync();

            _context.DocumentChunks.RemoveRange(chunks);

            document.IsChunked = false;
            document.ChunkCount = 0;
            document.ChunkedAt = null;

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chunks for document {DocumentId}", documentId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "Failed to delete chunks"
            });
        }
    }

    /// <summary>
    /// Preview chunking for text without saving
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<List<ChunkPreview>>>> PreviewChunks(
        [FromBody] PreviewChunksRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new ApiResponse<List<ChunkPreview>>
                {
                    Success = false,
                    Message = "Text is required"
                });
            }

            // TODO: Get userId from JWT
            var userId = 1;

            var options = request.Options != null
                ? new ChunkingOptions
                {
                    MinChunkSize = request.Options.MinChunkSize ?? 200,
                    MaxChunkSize = request.Options.MaxChunkSize ?? 2000,
                    UseSemanticChunking = request.Options.UseSemanticChunking ?? true
                }
                : ChunkingOptions.Default;

            var previews = await _chunkingService.PreviewChunksAsync(request.Text, options, userId);

            return Ok(new ApiResponse<List<ChunkPreview>>
            {
                Success = true,
                Data = previews
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing chunks");
            return StatusCode(500, new ApiResponse<List<ChunkPreview>>
            {
                Success = false,
                Message = "Failed to preview chunks"
            });
        }
    }
}

// DTOs

public class DocumentChunkDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ContentLength { get; set; }
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public string? TopicLabel { get; set; }
    public string? Summary { get; set; }
    public string ChunkType { get; set; } = "mixed";
    public bool HasEmbedding { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public string? DocumentFileName { get; set; }
}

public class ChunkingOptionsDto
{
    public int? MinChunkSize { get; set; }
    public int? MaxChunkSize { get; set; }
    public int? TargetChunkSize { get; set; }
    public bool? UseSemanticChunking { get; set; }
    public bool? GenerateEmbeddings { get; set; }
}

public class RechunkResultDto
{
    public int DocumentId { get; set; }
    public int ChunkCount { get; set; }
    public List<int> ChunkIds { get; set; } = new();
}

public class BatchRechunkRequest
{
    public List<int>? DocumentIds { get; set; }
    public bool ProcessAllUnchunked { get; set; }
    public ChunkingOptionsDto? Options { get; set; }
}

public class PreviewChunksRequest
{
    public string Text { get; set; } = string.Empty;
    public ChunkingOptionsDto? Options { get; set; }
}

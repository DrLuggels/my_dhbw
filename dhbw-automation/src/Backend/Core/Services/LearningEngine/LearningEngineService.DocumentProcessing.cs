using System.Diagnostics;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.LearningEngine;

/// <summary>
/// Document processing pipeline for the Learning Engine.
/// Handles: Parse -> Chunk -> Embed -> Extract Entities -> Extract Relationships
/// </summary>
public partial class LearningEngineService
{
    /// <summary>
    /// Processes a document through the full learning engine pipeline:
    /// 1. Parse (already done) -> 2. Chunk -> 3. Embed -> 4. Extract Entities -> 5. Extract Relationships
    /// </summary>
    public async Task<LearningDocumentResult> ProcessDocumentAsync(
        int documentId,
        int userId,
        LearningProcessingOptions? options = null)
    {
        options ??= new LearningProcessingOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new LearningDocumentResult { DocumentId = documentId };

        try
        {
            // Get the document
            var document = await _context.Documents
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Document {documentId} not found for user {userId}";
                return result;
            }

            result.DocumentName = document.FileName ?? $"Document {documentId}";

            // Check if document has extracted text
            if (string.IsNullOrWhiteSpace(document.ExtractedText))
            {
                result.Success = false;
                result.ErrorMessage = "Document has no extracted text. Please process the document first.";
                return result;
            }

            _logger.LogInformation("Starting Learning Engine processing for document {DocumentId} ({DocumentName})",
                documentId, document.FileName);

            // Step 1: Chunking (if not already done or re-processing requested)
            if (!document.IsChunked || document.Chunks.Count == 0)
            {
                _logger.LogInformation("Step 1: Chunking document {DocumentId}", documentId);

                var chunkingOptions = new ChunkingOptions
                {
                    UseSemanticChunking = options.UseSemanticChunking,
                    TargetChunkSize = options.TargetChunkSize,
                    ChunkOverlap = options.ChunkOverlap,
                    GenerateEmbeddings = options.GenerateEmbeddings,
                    GenerateTopicLabels = true
                };

                var chunkIds = await _chunkingService.ChunkDocumentAsync(documentId, chunkingOptions);
                result.ChunksCreated = chunkIds.Count;

                _logger.LogInformation("Created {ChunkCount} chunks for document {DocumentId}",
                    chunkIds.Count, documentId);
            }
            else
            {
                result.ChunksCreated = document.Chunks.Count;
                _logger.LogInformation("Document {DocumentId} already has {ChunkCount} chunks",
                    documentId, document.Chunks.Count);

                // Generate embeddings for chunks that don't have them
                if (options.GenerateEmbeddings)
                {
                    var chunksWithoutEmbeddings = document.Chunks
                        .Where(c => !c.HasEmbedding)
                        .ToList();

                    foreach (var chunk in chunksWithoutEmbeddings)
                    {
                        try
                        {
                            await _embeddingService.ProcessChunkEmbeddingAsync(chunk.Id, userId);
                            result.EmbeddingsGenerated++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate embedding for chunk {ChunkId}", chunk.Id);
                            result.Warnings.Add($"Failed to embed chunk {chunk.Id}: {ex.Message}");
                        }
                    }
                }
            }

            // Step 2: Entity and Relationship Extraction
            if (options.ExtractEntities || options.ExtractRelationships)
            {
                // Reload chunks from database
                var chunks = await _context.DocumentChunks
                    .Where(c => c.DocumentId == documentId)
                    .OrderBy(c => c.ChunkIndex)
                    .ToListAsync();

                _logger.LogInformation("Step 2: Extracting entities and relationships from {ChunkCount} chunks",
                    chunks.Count);

                foreach (var chunk in chunks)
                {
                    try
                    {
                        var extractionResult = await ExtractFromChunkAsync(chunk.Id, userId);

                        if (extractionResult.Success)
                        {
                            result.EntitiesExtracted += extractionResult.Entities.Count;
                            result.RelationshipsExtracted += extractionResult.Relationships.Count;
                        }
                        else
                        {
                            result.Warnings.Add($"Chunk {chunk.Id}: {extractionResult.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract from chunk {ChunkId}", chunk.Id);
                        result.Warnings.Add($"Chunk {chunk.Id}: {ex.Message}");
                    }

                    // Rate limiting delay between API calls
                    await Task.Delay(500);
                }
            }

            // Step 3: Generate embeddings for new entities
            if (options.GenerateEmbeddings)
            {
                var entitiesWithoutEmbeddings = await _context.KgEntities
                    .Where(e => e.DocumentId == documentId && !e.HasEmbedding && e.IsActive)
                    .ToListAsync();

                foreach (var entity in entitiesWithoutEmbeddings)
                {
                    try
                    {
                        await ProcessEntityEmbeddingAsync(entity.Id, userId);
                        result.EmbeddingsGenerated++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate embedding for entity {EntityId}", entity.Id);
                    }
                }
            }

            // Step 4: Update importance scores based on relationships
            await UpdateImportanceScoresAsync(documentId);

            result.Success = true;
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;

            _logger.LogInformation(
                "Learning Engine processing completed for document {DocumentId}: " +
                "{ChunksCreated} chunks, {EntitiesExtracted} entities, {RelationshipsExtracted} relationships " +
                "in {ProcessingTime:F1}s",
                documentId, result.ChunksCreated, result.EntitiesExtracted,
                result.RelationshipsExtracted, stopwatch.Elapsed.TotalSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = stopwatch.Elapsed;
            return result;
        }
    }

    /// <summary>
    /// Processes multiple documents in batch.
    /// </summary>
    public async Task<List<LearningDocumentResult>> ProcessDocumentsBatchAsync(
        IEnumerable<int> documentIds,
        int userId,
        LearningProcessingOptions? options = null)
    {
        var results = new List<LearningDocumentResult>();

        foreach (var documentId in documentIds)
        {
            try
            {
                var result = await ProcessDocumentAsync(documentId, userId, options);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId} in batch", documentId);
                results.Add(new LearningDocumentResult
                {
                    DocumentId = documentId,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }

            // Rate limiting between documents
            await Task.Delay(1000);
        }

        return results;
    }

    /// <summary>
    /// Generate embedding for a KgEntity
    /// </summary>
    private async Task ProcessEntityEmbeddingAsync(int entityId, int userId)
    {
        var entity = await _context.KgEntities.FindAsync(entityId);
        if (entity == null) return;

        // Create text for embedding: name + description
        var textToEmbed = entity.Name;
        if (!string.IsNullOrEmpty(entity.Description))
        {
            textToEmbed += " - " + entity.Description;
        }

        var embedding = await _embeddingService.GenerateEmbeddingAsync(textToEmbed, userId);
        if (embedding == null)
        {
            _logger.LogWarning("Failed to generate embedding for entity {EntityId}", entityId);
            return;
        }

        // Store in Qdrant
        var metadata = new Dictionary<string, string>
        {
            ["kg_entity_type"] = entity.EntityType,
            ["name"] = entity.Name,
            ["subject"] = entity.Subject ?? "",
            ["topic"] = entity.Topic ?? ""
        };

        var pointId = await _qdrantService.UpsertEmbeddingAsync(
            KgEntitiesCollection,
            embedding,
            "kg_entity",
            entity.Id,
            entity.UserId,
            metadata);

        // Update entity
        entity.HasEmbedding = true;
        entity.QdrantPointId = pointId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogDebug("Generated embedding for entity {EntityId} ({EntityName})", entityId, entity.Name);
    }

    /// <summary>
    /// Update importance scores based on relationship count and connectivity
    /// </summary>
    private async Task UpdateImportanceScoresAsync(int documentId)
    {
        var entities = await _context.KgEntities
            .Where(e => e.DocumentId == documentId && e.IsActive)
            .ToListAsync();

        foreach (var entity in entities)
        {
            // Count incoming and outgoing relationships
            var relationshipCount = await _context.KgRelationships
                .CountAsync(r => r.IsActive &&
                    (r.SourceEntityId == entity.Id || r.TargetEntityId == entity.Id));

            // Calculate importance based on:
            // - Occurrence count (how often mentioned)
            // - Relationship count (how connected)
            // - Entity type weight
            var typeWeight = entity.EntityType switch
            {
                KgEntityTypes.Concept => 1.0,
                KgEntityTypes.Definition => 0.9,
                KgEntityTypes.Formula => 0.9,
                KgEntityTypes.Theorem => 0.85,
                KgEntityTypes.Method => 0.8,
                KgEntityTypes.Example => 0.6,
                _ => 0.7
            };

            // Normalize to 0-1 range
            var occurrenceScore = Math.Min(entity.OccurrenceCount / 10.0, 1.0);
            var relationshipScore = Math.Min(relationshipCount / 20.0, 1.0);

            entity.ImportanceScore = (occurrenceScore * 0.3 + relationshipScore * 0.4 + typeWeight * 0.3);
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}

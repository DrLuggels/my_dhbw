using System.Diagnostics;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.OmniLearning;

public partial class OmniLearningEngineService
{
    #region Document Processing Pipeline

    /// <summary>
    /// Verarbeitet ein Dokument: Chunking → Embedding → Entity-Extraktion → Knowledge Graph
    /// </summary>
    public async Task<DocumentProcessingResult> ProcessDocumentAsync(
        int documentId, int userId, ProcessingOptions? options = null)
    {
        var stopwatch = Stopwatch.StartNew();
        options ??= new ProcessingOptions();

        var result = new DocumentProcessingResult
        {
            DocumentId = documentId,
            Success = false
        };

        try
        {
            // 1. Lade Dokument
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
            {
                result.ErrorMessage = "Dokument nicht gefunden";
                return result;
            }

            if (string.IsNullOrEmpty(document.ExtractedText))
            {
                result.ErrorMessage = "Dokument enthaelt keinen extrahierten Text";
                result.Warnings.Add("Dokumententext fehlt - bitte zuerst Text extrahieren");
                return result;
            }

            _logger.LogInformation("Starte Verarbeitung von Dokument {DocumentId} fuer User {UserId}",
                documentId, userId);

            // 2. Chunking
            var chunks = await CreateChunksAsync(document, userId, options);
            result.ChunksCreated = chunks.Count;

            if (chunks.Count == 0)
            {
                result.Warnings.Add("Keine Chunks erstellt - Text moeglicherweise zu kurz");
            }

            // 3. Embeddings generieren
            if (options.GenerateEmbeddings && chunks.Any())
            {
                result.EmbeddingsGenerated = await GenerateChunkEmbeddingsAsync(chunks, userId);
            }

            // 4. Entity-Extraktion (vereinfacht - erstellt Entitaeten basierend auf Chunk-Inhalt)
            if (options.ExtractEntities && chunks.Any())
            {
                var extractionResult = await ExtractEntitiesFromChunksAsync(
                    chunks, document, userId, options);
                result.EntitiesExtracted = extractionResult.EntitiesCreated;
                result.RelationshipsCreated = extractionResult.RelationshipsCreated;
            }

            result.Success = true;
            _logger.LogInformation(
                "Dokument {DocumentId} verarbeitet: {Chunks} Chunks, {Entities} Entitaeten, {Relations} Beziehungen",
                documentId, result.ChunksCreated, result.EntitiesExtracted, result.RelationshipsCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Verarbeitung von Dokument {DocumentId}", documentId);
            result.ErrorMessage = ex.Message;
        }

        stopwatch.Stop();
        result.ProcessingTime = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Verarbeitet mehrere Dokumente im Batch
    /// </summary>
    public async Task<BatchProcessingResult> ProcessDocumentsBatchAsync(
        int[] documentIds, int userId, ProcessingOptions? options = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new BatchProcessingResult
        {
            TotalDocuments = documentIds.Length
        };

        foreach (var documentId in documentIds)
        {
            var docResult = await ProcessDocumentAsync(documentId, userId, options);
            result.Results.Add(docResult);

            if (docResult.Success)
                result.SuccessfulDocuments++;
            else
                result.FailedDocuments++;
        }

        stopwatch.Stop();
        result.TotalProcessingTime = stopwatch.Elapsed;
        return result;
    }

    private async Task<List<DocumentChunk>> CreateChunksAsync(
        Document document, int userId, ProcessingOptions options)
    {
        try
        {
            // Nutze den ChunkingService fuer semantisches Chunking
            var chunkingOptions = new ChunkingOptions
            {
                TargetChunkSize = options.ChunkSize,
                ChunkOverlap = options.ChunkOverlap,
                GenerateEmbeddings = false, // Wir generieren Embeddings separat
                UseSemanticChunking = true
            };

            // ChunkDocumentAsync erstellt und speichert die Chunks automatisch
            await _chunkingService.ChunkDocumentAsync(document.Id, chunkingOptions);

            // Hole die erstellten Chunks
            var chunks = await _chunkingService.GetDocumentChunksAsync(document.Id);

            // Aktualisiere TopicLabel falls angegeben
            if (!string.IsNullOrEmpty(options.FocusTopic))
            {
                foreach (var chunk in chunks)
                {
                    if (string.IsNullOrEmpty(chunk.TopicLabel))
                    {
                        chunk.TopicLabel = options.FocusTopic;
                    }
                }
                await _context.SaveChangesAsync();
            }

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Chunking von Dokument {DocumentId}", document.Id);
            return new List<DocumentChunk>();
        }
    }

    private async Task<int> GenerateChunkEmbeddingsAsync(List<DocumentChunk> chunks, int userId)
    {
        int generated = 0;

        foreach (var chunk in chunks)
        {
            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content, userId);
                if (embedding != null && embedding.Length > 0)
                {
                    // Store embedding in Qdrant
                    var pointId = await _qdrantService.UpsertEmbeddingAsync(
                        "document_chunks",
                        embedding,
                        "chunk",
                        chunk.Id,
                        userId,
                        new Dictionary<string, string>
                        {
                            ["document_id"] = chunk.DocumentId.ToString(),
                            ["topic"] = chunk.TopicLabel ?? ""
                        });

                    chunk.QdrantPointId = pointId;
                    chunk.HasEmbedding = true;
                    chunk.Status = "embedded";
                    generated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Konnte Embedding fuer Chunk {ChunkId} nicht generieren", chunk.Id);
            }
        }

        if (generated > 0)
        {
            await _context.SaveChangesAsync();
        }

        return generated;
    }

    private async Task<EntityExtractionResult> ExtractEntitiesFromChunksAsync(
        List<DocumentChunk> chunks, Document document, int userId, ProcessingOptions options)
    {
        var result = new EntityExtractionResult();

        // Vereinfachte Entity-Extraktion: Erstelle eine Entitaet pro Dokument
        // Komplexere KI-basierte Extraktion kann spaeter implementiert werden
        try
        {
            var entity = await CreateEntityAsync(userId, new CreateEntityDto
            {
                EntityType = "document_content",
                Name = document.FileName ?? $"Dokument {document.Id}",
                Description = $"Inhalt aus Dokument: {document.FileName}",
                Subject = options.FocusSubject ?? "Allgemein",
                Topic = options.FocusTopic ?? "Dokumentinhalt",
                SourceDocumentId = document.Id
            });

            result.EntitiesCreated = 1;

            _logger.LogInformation("Entitaet {EntityName} aus Dokument {DocumentId} erstellt",
                entity.Name, document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler bei Entity-Extraktion aus Dokument {DocumentId}", document.Id);
        }

        return result;
    }

    #endregion

    #region Helper Classes

    private class EntityExtractionResult
    {
        public int EntitiesCreated { get; set; }
        public int RelationshipsCreated { get; set; }
    }

    #endregion
}

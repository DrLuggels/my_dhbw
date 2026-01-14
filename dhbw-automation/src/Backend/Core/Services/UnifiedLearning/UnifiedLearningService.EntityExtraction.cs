using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.UnifiedLearning;

/// <summary>
/// Entity Extraction - Claude-based extraction of entities and relationships from documents
/// </summary>
public partial class UnifiedLearningService
{
    /// <inheritdoc />
    public async Task<UnifiedExtractionResult> ExtractEntitiesFromDocumentAsync(
        int documentId,
        int userId,
        UnifiedExtractionOptions? options = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new UnifiedExtractionResult
        {
            DocumentId = documentId,
            Success = false
        };

        options ??= new UnifiedExtractionOptions();

        try
        {
            // Get document
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                result.ErrorMessage = $"Document {documentId} not found";
                return result;
            }

            result.DocumentName = document.FileName;

            // Get chunks for document
            var chunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();

            if (!chunks.Any())
            {
                result.Warnings.Add("No chunks found for document. Please chunk the document first.");
                result.Success = true;
                return result;
            }

            // Process each chunk
            foreach (var chunk in chunks)
            {
                try
                {
                    var chunkResult = await ExtractEntitiesFromChunkAsync(chunk.Id, userId);

                    result.EntitiesCreated += chunkResult.EntitiesCreated;
                    result.EntitiesUpdated += chunkResult.EntitiesUpdated;
                    result.RelationshipsCreated += chunkResult.RelationshipsCreated;
                    result.PrerequisitesDetected += chunkResult.PrerequisitesDetected;
                    result.Entities.AddRange(chunkResult.Entities);
                    result.Relationships.AddRange(chunkResult.Relationships);
                    result.Warnings.AddRange(chunkResult.Warnings);
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Error processing chunk {chunk.Id}: {ex.Message}");
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error extracting entities from document {DocumentId}", documentId);
        }

        stopwatch.Stop();
        result.ProcessingTime = stopwatch.Elapsed;

        return result;
    }

    /// <inheritdoc />
    public async Task<UnifiedExtractionResult> ExtractEntitiesFromChunkAsync(
        int chunkId,
        int userId)
    {
        var result = new UnifiedExtractionResult
        {
            Success = false
        };

        try
        {
            var chunk = await _context.DocumentChunks
                .Include(c => c.Document)
                .FirstOrDefaultAsync(c => c.Id == chunkId);

            if (chunk == null)
            {
                result.ErrorMessage = $"Chunk {chunkId} not found";
                return result;
            }

            result.DocumentId = chunk.DocumentId;
            result.DocumentName = chunk.Document?.FileName ?? "";

            // Build extraction prompt
            var prompt = BuildEntityExtractionPrompt(chunk.Content, chunk.Document?.Subject);

            // Call Claude
            var apiKey = await GetAnthropicApiKeyAsync(userId);
            var response = await _anthropicClient.ChatAsync(
                "You are a knowledge extraction assistant. Extract key concepts, definitions, and relationships from educational content.",
                prompt,
                apiKey: apiKey);

            // Parse response
            var (entities, relationships) = ParseExtractionResponse(response, chunk, userId);

            // Save entities
            foreach (var entityData in entities)
            {
                var existingEntity = await _context.Set<UnifiedKnowledgeEntity>()
                    .FirstOrDefaultAsync(e =>
                        e.UserId == userId &&
                        e.NormalizedName == NormalizeName(entityData.Name) &&
                        e.Subject == entityData.Subject &&
                        e.IsActive);

                if (existingEntity != null)
                {
                    // Update existing
                    existingEntity.OccurrenceCount++;
                    if (entityData.ImportanceScore > existingEntity.ImportanceScore)
                        existingEntity.ImportanceScore = entityData.ImportanceScore;
                    existingEntity.UpdatedAt = DateTime.UtcNow;
                    result.EntitiesUpdated++;
                    result.Entities.Add(MapToDto(existingEntity));
                }
                else
                {
                    // Create new
                    var newEntity = new UnifiedKnowledgeEntity
                    {
                        UserId = userId,
                        Subject = entityData.Subject,
                        Topic = entityData.Topic,
                        EntityType = entityData.EntityType,
                        Name = entityData.Name,
                        NormalizedName = NormalizeName(entityData.Name),
                        Description = entityData.Description,
                        ConfidenceScore = entityData.ConfidenceScore,
                        ImportanceScore = entityData.ImportanceScore,
                        SourceDocumentId = chunk.DocumentId,
                        SourceChunkId = chunkId,
                        CreatedAt = DateTime.UtcNow,
                        LastInteraction = DateTime.UtcNow
                    };

                    _context.Set<UnifiedKnowledgeEntity>().Add(newEntity);
                    await _context.SaveChangesAsync();

                    result.EntitiesCreated++;
                    result.Entities.Add(MapToDto(newEntity));

                    // Generate embedding for entity
                    try
                    {
                        var embeddingText = $"{newEntity.Name}: {newEntity.Description ?? newEntity.Topic}";
                        var embedding = await _embeddingService.GenerateEmbeddingAsync(embeddingText);

                        if (embedding != null && embedding.Length > 0)
                        {
                            var pointId = await _qdrantService.UpsertEmbeddingAsync(
                                UnifiedEntitiesCollection,
                                embedding,
                                "unified_entity",
                                newEntity.Id,
                                userId,
                                new Dictionary<string, string>
                                {
                                    { "name", newEntity.Name },
                                    { "subject", newEntity.Subject },
                                    { "entityType", newEntity.EntityType }
                                });

                            newEntity.HasEmbedding = true;
                            newEntity.QdrantPointId = pointId;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"Failed to create embedding for entity {newEntity.Name}: {ex.Message}");
                    }
                }
            }

            // Save relationships
            foreach (var relData in relationships)
            {
                try
                {
                    var sourceEntity = await _context.Set<UnifiedKnowledgeEntity>()
                        .FirstOrDefaultAsync(e =>
                            e.UserId == userId &&
                            e.NormalizedName == NormalizeName(relData.SourceName) &&
                            e.IsActive);

                    var targetEntity = await _context.Set<UnifiedKnowledgeEntity>()
                        .FirstOrDefaultAsync(e =>
                            e.UserId == userId &&
                            e.NormalizedName == NormalizeName(relData.TargetName) &&
                            e.IsActive);

                    if (sourceEntity != null && targetEntity != null)
                    {
                        var relationship = await CreateOrUpdateRelationshipAsync(
                            userId,
                            sourceEntity.Id,
                            targetEntity.Id,
                            relData.RelationshipType,
                            relData.Strength);

                        relationship.Evidence = relData.Evidence;
                        relationship.ExtractedFromChunkId = chunkId;
                        relationship.ExtractedFromDocumentId = chunk.DocumentId;
                        relationship.IsAutoExtracted = true;
                        relationship.ConfidenceScore = relData.Confidence;

                        await _context.SaveChangesAsync();

                        result.RelationshipsCreated++;
                        result.Relationships.Add(MapToDto(relationship));

                        if (relData.RelationshipType == UnifiedRelationshipTypes.Prerequisite ||
                            relData.RelationshipType == UnifiedRelationshipTypes.Requires)
                        {
                            result.PrerequisitesDetected++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Failed to create relationship {relData.SourceName} -> {relData.TargetName}: {ex.Message}");
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error extracting entities from chunk {ChunkId}", chunkId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<UnifiedExtractionResult>> ProcessDocumentsBatchAsync(
        IEnumerable<int> documentIds,
        int userId,
        UnifiedExtractionOptions? options = null)
    {
        var results = new List<UnifiedExtractionResult>();

        foreach (var documentId in documentIds)
        {
            var result = await ExtractEntitiesFromDocumentAsync(documentId, userId, options);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Build prompt for entity extraction
    /// </summary>
    private string BuildEntityExtractionPrompt(string content, string? subject)
    {
        return $@"Analysiere den folgenden Textabschnitt aus einem Lernmaterial{(subject != null ? $" zum Fach '{subject}'" : "")} und extrahiere:

1. Wichtige Entitäten (Konzepte, Definitionen, Formeln, Personen, Theoreme, Methoden, Algorithmen)
2. Beziehungen zwischen den Entitäten

Textabschnitt:
---
{content}
---

Antworte im folgenden JSON-Format:
{{
  ""entities"": [
    {{
      ""name"": ""Name des Konzepts"",
      ""type"": ""concept|definition|formula|person|theorem|method|algorithm"",
      ""description"": ""Kurze Beschreibung oder Definition"",
      ""topic"": ""Themenbereich"",
      ""importance"": 0.0-1.0,
      ""confidence"": 0.0-1.0
    }}
  ],
  ""relationships"": [
    {{
      ""source"": ""Quell-Entität Name"",
      ""target"": ""Ziel-Entität Name"",
      ""type"": ""is_a|part_of|relates_to|requires|prerequisite|example_of|defines|uses|derives_from"",
      ""strength"": 0.0-1.0,
      ""confidence"": 0.0-1.0,
      ""evidence"": ""Textbeleg für die Beziehung""
    }}
  ]
}}

Wichtig:
- Extrahiere nur klar erkennbare Konzepte
- Nutze 'prerequisite' oder 'requires' für Lernabhängigkeiten
- Die Wichtigkeit (importance) basiert auf der Bedeutung für das Thema
- Die Konfidenz (confidence) basiert auf der Klarheit der Extraktion
- Antworte NUR mit dem JSON, keine zusätzlichen Erklärungen";
    }

    /// <summary>
    /// Parse Claude's extraction response
    /// </summary>
    private (List<ExtractedEntity> entities, List<ExtractedRelationship> relationships) ParseExtractionResponse(
        string response,
        DocumentChunk chunk,
        int userId)
    {
        var entities = new List<ExtractedEntity>();
        var relationships = new List<ExtractedRelationship>();

        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;
            if (jsonStart < 0 || jsonEnd <= jsonStart)
                return (entities, relationships);

            var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
            using var doc = JsonDocument.Parse(jsonContent);

            // Parse entities
            if (doc.RootElement.TryGetProperty("entities", out var entitiesArray))
            {
                foreach (var e in entitiesArray.EnumerateArray())
                {
                    entities.Add(new ExtractedEntity
                    {
                        Name = e.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                        EntityType = e.TryGetProperty("type", out var type) ? type.GetString() ?? "concept" : "concept",
                        Description = e.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        Topic = e.TryGetProperty("topic", out var topic) ? topic.GetString() ?? "" : "",
                        Subject = chunk.Document?.Subject ?? "Allgemein",
                        ImportanceScore = e.TryGetProperty("importance", out var imp) ? imp.GetDouble() : 0.5,
                        ConfidenceScore = e.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.8
                    });
                }
            }

            // Parse relationships
            if (doc.RootElement.TryGetProperty("relationships", out var relsArray))
            {
                foreach (var r in relsArray.EnumerateArray())
                {
                    relationships.Add(new ExtractedRelationship
                    {
                        SourceName = r.TryGetProperty("source", out var source) ? source.GetString() ?? "" : "",
                        TargetName = r.TryGetProperty("target", out var target) ? target.GetString() ?? "" : "",
                        RelationshipType = r.TryGetProperty("type", out var relType) ? relType.GetString() ?? "relates_to" : "relates_to",
                        Strength = r.TryGetProperty("strength", out var str) ? str.GetDouble() : 0.8,
                        Confidence = r.TryGetProperty("confidence", out var relConf) ? relConf.GetDouble() : 0.8,
                        Evidence = r.TryGetProperty("evidence", out var ev) ? ev.GetString() : null
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse extraction response as JSON");
        }

        return (entities, relationships);
    }

    // Helper classes for parsing
    private class ExtractedEntity
    {
        public string Name { get; set; } = "";
        public string EntityType { get; set; } = "concept";
        public string? Description { get; set; }
        public string Topic { get; set; } = "";
        public string Subject { get; set; } = "";
        public double ImportanceScore { get; set; }
        public double ConfidenceScore { get; set; }
    }

    private class ExtractedRelationship
    {
        public string SourceName { get; set; } = "";
        public string TargetName { get; set; } = "";
        public string RelationshipType { get; set; } = "relates_to";
        public double Strength { get; set; }
        public double Confidence { get; set; }
        public string? Evidence { get; set; }
    }
}

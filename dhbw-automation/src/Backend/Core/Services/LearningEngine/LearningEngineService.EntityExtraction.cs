using System.Text.Json;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.LearningEngine;

/// <summary>
/// Entity and relationship extraction for the Learning Engine.
/// Uses Claude to extract concepts, definitions, formulas, etc. from document chunks.
/// </summary>
public partial class LearningEngineService
{
    /// <summary>
    /// Extracts entities and relationships from a specific chunk.
    /// </summary>
    public async Task<ChunkExtractionResult> ExtractFromChunkAsync(int chunkId, int userId)
    {
        var result = new ChunkExtractionResult { ChunkId = chunkId };

        try
        {
            var chunk = await _context.DocumentChunks
                .Include(c => c.Document)
                .FirstOrDefaultAsync(c => c.Id == chunkId && c.UserId == userId);

            if (chunk == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Chunk {chunkId} not found for user {userId}";
                return result;
            }

            if (string.IsNullOrWhiteSpace(chunk.Content))
            {
                result.Success = false;
                result.ErrorMessage = "Chunk has no content";
                return result;
            }

            _logger.LogDebug("Extracting entities and relationships from chunk {ChunkId}", chunkId);

            // Get document subject info if available
            var documentSubject = chunk.Document?.Subject ?? chunk.Document?.Category ?? "";
            var documentTopic = chunk.TopicLabel ?? "";

            // Extract using Claude
            var extraction = await ExtractEntitiesAndRelationshipsAsync(
                chunk.Content,
                documentSubject,
                documentTopic,
                userId);

            if (extraction == null)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to extract entities from chunk";
                return result;
            }

            // Process extracted entities
            foreach (var extractedEntity in extraction.Entities)
            {
                var entity = await CreateOrUpdateEntityAsync(
                    extractedEntity,
                    chunk.DocumentId,
                    chunk.Id,
                    userId);

                if (entity != null)
                {
                    result.Entities.Add(MapToDto(entity));
                }
            }

            // Process extracted relationships
            foreach (var extractedRel in extraction.Relationships)
            {
                var relationship = await CreateRelationshipAsync(
                    extractedRel,
                    chunk.DocumentId,
                    chunk.Id,
                    userId);

                if (relationship != null)
                {
                    result.Relationships.Add(MapToDto(relationship));
                }
            }

            result.Success = true;

            _logger.LogInformation(
                "Extracted {EntityCount} entities and {RelCount} relationships from chunk {ChunkId}",
                result.Entities.Count, result.Relationships.Count, chunkId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting from chunk {ChunkId}", chunkId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Use Claude to extract entities and relationships from text.
    /// </summary>
    private async Task<ExtractionResponse?> ExtractEntitiesAndRelationshipsAsync(
        string content,
        string documentSubject,
        string documentTopic,
        int userId)
    {
        var apiKey = await GetAnthropicApiKeyAsync(userId);

        var systemPrompt = @"Du bist ein Experte für Wissensextraktion aus akademischen Texten.

Deine Aufgabe ist es, aus dem gegebenen Text wichtige Entitäten (Konzepte, Definitionen, Formeln, etc.)
und deren Beziehungen zu extrahieren.

ENTITÄTSTYPEN:
- concept: Allgemeine Konzepte und Ideen
- definition: Formale Definitionen von Begriffen
- formula: Mathematische Formeln und Gleichungen
- theorem: Mathematische Sätze und Beweise
- method: Verfahren und Methoden
- algorithm: Algorithmen und Prozeduren
- person: Personen (Wissenschaftler, Erfinder, etc.)
- date: Wichtige Daten und Zeiträume
- example: Konkrete Beispiele
- term: Fachbegriffe ohne vollständige Definition
- principle: Grundprinzipien

BEZIEHUNGSTYPEN:
- is_a: A ist ein Typ von B (Klassifikation)
- part_of: A ist Teil von B (Komposition)
- relates_to: A hängt mit B zusammen (allgemeine Beziehung)
- requires: A benötigt B (Voraussetzung)
- contradicts: A widerspricht B
- example_of: A ist ein Beispiel für B
- defines: A definiert B
- uses: A verwendet B
- precedes: A kommt vor B (zeitlich/logisch)
- derives_from: A leitet sich von B ab
- extends: A erweitert B
- implements: A implementiert B
- similar_to: A ist ähnlich zu B

WICHTIG:
1. Extrahiere nur klare, eindeutige Entitäten
2. Beschreibe jede Entität kurz aber präzise
3. Beziehungen sollten durch den Text belegt sein
4. Gib die Evidenz (Textausschnitt) für jede Beziehung an
5. Vergiss keine wichtigen Konzepte

Antworte NUR mit validem JSON im folgenden Format:
{
  ""entities"": [
    {
      ""name"": ""Entitätsname"",
      ""type"": ""concept"",
      ""description"": ""Kurze Beschreibung (max 200 Zeichen)"",
      ""confidence"": 0.9
    }
  ],
  ""relationships"": [
    {
      ""source"": ""Entität A"",
      ""target"": ""Entität B"",
      ""type"": ""relates_to"",
      ""evidence"": ""Textausschnitt der die Beziehung belegt"",
      ""strength"": 0.8
    }
  ]
}";

        var contextInfo = "";
        if (!string.IsNullOrEmpty(documentSubject))
        {
            contextInfo += $"Fachbereich: {documentSubject}\n";
        }
        if (!string.IsNullOrEmpty(documentTopic))
        {
            contextInfo += $"Thema: {documentTopic}\n";
        }

        var userMessage = $@"{(string.IsNullOrEmpty(contextInfo) ? "" : $"Kontext:\n{contextInfo}\n")}Extrahiere Entitäten und Beziehungen aus diesem Text:

{content}";

        try
        {
            var responseJson = await _anthropicClient.ChatJsonAsync(
                systemPrompt,
                userMessage,
                model: "claude-sonnet-4-5",
                maxTokens: 4096,
                apiKey: apiKey
            );

            return ParseExtractionResponse(responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude for entity extraction");
            return null;
        }
    }

    /// <summary>
    /// Parse Claude's JSON response into extraction result.
    /// </summary>
    private ExtractionResponse? ParseExtractionResponse(JsonDocument json)
    {
        try
        {
            var result = new ExtractionResponse();
            var root = json.RootElement;

            // Parse entities
            if (root.TryGetProperty("entities", out var entitiesArray))
            {
                foreach (var element in entitiesArray.EnumerateArray())
                {
                    var entity = new ExtractedEntity
                    {
                        Name = element.GetProperty("name").GetString() ?? "",
                        Type = element.TryGetProperty("type", out var type)
                            ? type.GetString() ?? "concept"
                            : "concept",
                        Description = element.TryGetProperty("description", out var desc)
                            ? desc.GetString()
                            : null,
                        Confidence = element.TryGetProperty("confidence", out var conf)
                            ? conf.GetDouble()
                            : 0.8
                    };

                    if (!string.IsNullOrWhiteSpace(entity.Name))
                    {
                        result.Entities.Add(entity);
                    }
                }
            }

            // Parse relationships
            if (root.TryGetProperty("relationships", out var relsArray))
            {
                foreach (var element in relsArray.EnumerateArray())
                {
                    var rel = new ExtractedRelationship
                    {
                        Source = element.GetProperty("source").GetString() ?? "",
                        Target = element.GetProperty("target").GetString() ?? "",
                        Type = element.TryGetProperty("type", out var type)
                            ? type.GetString() ?? "relates_to"
                            : "relates_to",
                        Evidence = element.TryGetProperty("evidence", out var ev)
                            ? ev.GetString()
                            : null,
                        Strength = element.TryGetProperty("strength", out var str)
                            ? str.GetDouble()
                            : 0.8
                    };

                    if (!string.IsNullOrWhiteSpace(rel.Source) && !string.IsNullOrWhiteSpace(rel.Target))
                    {
                        result.Relationships.Add(rel);
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing extraction response");
            return null;
        }
    }

    /// <summary>
    /// Create or update an entity in the database.
    /// If an entity with the same normalized name exists, update it; otherwise create new.
    /// </summary>
    private async Task<KgEntity?> CreateOrUpdateEntityAsync(
        ExtractedEntity extracted,
        int documentId,
        int chunkId,
        int userId)
    {
        if (string.IsNullOrWhiteSpace(extracted.Name))
            return null;

        var normalizedName = NormalizeName(extracted.Name);

        // Check if entity already exists for this user
        var existingEntity = await _context.KgEntities
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.NormalizedName == normalizedName &&
                e.IsActive);

        if (existingEntity != null)
        {
            // Update existing entity
            existingEntity.OccurrenceCount++;

            // Update description if new one is longer/better
            if (!string.IsNullOrEmpty(extracted.Description) &&
                (string.IsNullOrEmpty(existingEntity.Description) ||
                 extracted.Description.Length > existingEntity.Description.Length))
            {
                existingEntity.Description = extracted.Description;
            }

            // Update confidence if higher
            if (extracted.Confidence > existingEntity.ConfidenceScore)
            {
                existingEntity.ConfidenceScore = extracted.Confidence;
            }

            existingEntity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Updated existing entity {EntityId} ({Name}), occurrence: {Count}",
                existingEntity.Id, existingEntity.Name, existingEntity.OccurrenceCount);

            return existingEntity;
        }

        // Create new entity
        var newEntity = new KgEntity
        {
            UserId = userId,
            DocumentId = documentId,
            ChunkId = chunkId,
            EntityType = extracted.Type,
            Name = extracted.Name.Trim(),
            NormalizedName = normalizedName,
            Description = extracted.Description?.Trim(),
            ConfidenceScore = extracted.Confidence,
            OccurrenceCount = 1,
            IsActive = true
        };

        // Try to infer subject from document
        var document = await _context.Documents.FindAsync(documentId);
        if (document != null)
        {
            newEntity.Subject = document.Subject ?? document.Category;
        }

        _context.KgEntities.Add(newEntity);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Created new entity {EntityId} ({Name}, type: {Type})",
            newEntity.Id, newEntity.Name, newEntity.EntityType);

        return newEntity;
    }

    /// <summary>
    /// Create a relationship between two entities.
    /// </summary>
    private async Task<KgRelationship?> CreateRelationshipAsync(
        ExtractedRelationship extracted,
        int documentId,
        int chunkId,
        int userId)
    {
        if (string.IsNullOrWhiteSpace(extracted.Source) || string.IsNullOrWhiteSpace(extracted.Target))
            return null;

        // Find source and target entities
        var sourceNormalized = NormalizeName(extracted.Source);
        var targetNormalized = NormalizeName(extracted.Target);

        var sourceEntity = await _context.KgEntities
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.NormalizedName == sourceNormalized &&
                e.IsActive);

        var targetEntity = await _context.KgEntities
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.NormalizedName == targetNormalized &&
                e.IsActive);

        // Create entities if they don't exist
        if (sourceEntity == null)
        {
            sourceEntity = await CreateOrUpdateEntityAsync(
                new ExtractedEntity { Name = extracted.Source, Type = "concept", Confidence = 0.7 },
                documentId, chunkId, userId);
        }

        if (targetEntity == null)
        {
            targetEntity = await CreateOrUpdateEntityAsync(
                new ExtractedEntity { Name = extracted.Target, Type = "concept", Confidence = 0.7 },
                documentId, chunkId, userId);
        }

        if (sourceEntity == null || targetEntity == null)
        {
            _logger.LogWarning("Could not create entities for relationship: {Source} -> {Target}",
                extracted.Source, extracted.Target);
            return null;
        }

        // Check if relationship already exists
        var existingRel = await _context.KgRelationships
            .FirstOrDefaultAsync(r =>
                r.SourceEntityId == sourceEntity.Id &&
                r.TargetEntityId == targetEntity.Id &&
                r.RelationshipType == extracted.Type &&
                r.IsActive);

        if (existingRel != null)
        {
            // Update existing relationship
            if (extracted.Strength > existingRel.Strength)
            {
                existingRel.Strength = extracted.Strength;
            }
            if (!string.IsNullOrEmpty(extracted.Evidence) && string.IsNullOrEmpty(existingRel.Evidence))
            {
                existingRel.Evidence = extracted.Evidence;
            }
            existingRel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existingRel;
        }

        // Create new relationship
        var newRel = new KgRelationship
        {
            UserId = userId,
            SourceEntityId = sourceEntity.Id,
            TargetEntityId = targetEntity.Id,
            RelationshipType = extracted.Type,
            Strength = extracted.Strength,
            Evidence = extracted.Evidence,
            ExtractedFromChunkId = chunkId,
            ExtractedFromDocumentId = documentId,
            IsAutoExtracted = true,
            IsActive = true
        };

        _context.KgRelationships.Add(newRel);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Created relationship: {Source} --[{Type}]--> {Target}",
            sourceEntity.Name, extracted.Type, targetEntity.Name);

        return newRel;
    }

    // Internal classes for extraction
    private class ExtractionResponse
    {
        public List<ExtractedEntity> Entities { get; set; } = new();
        public List<ExtractedRelationship> Relationships { get; set; } = new();
    }

    private class ExtractedEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "concept";
        public string? Description { get; set; }
        public double Confidence { get; set; } = 0.8;
    }

    private class ExtractedRelationship
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Type { get; set; } = "relates_to";
        public string? Evidence { get; set; }
        public double Strength { get; set; } = 0.8;
    }
}

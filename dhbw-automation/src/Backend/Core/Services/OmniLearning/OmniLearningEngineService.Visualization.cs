using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.OmniLearning;

public partial class OmniLearningEngineService
{
    #region Visualization

    /// <summary>
    /// Holt den Knowledge Graph für Visualisierung
    /// </summary>
    public async Task<KnowledgeGraphDto> GetKnowledgeGraphAsync(
        int userId, GraphVisualizationFilters? filters = null)
    {
        filters ??= new GraphVisualizationFilters();

        var query = _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive);

        // Filter anwenden
        if (!string.IsNullOrEmpty(filters.Subject))
            query = query.Where(e => e.Subject == filters.Subject);
        if (!string.IsNullOrEmpty(filters.Topic))
            query = query.Where(e => e.Topic == filters.Topic);
        if (!filters.IncludeWeakEntities)
            query = query.Where(e => e.MasteryScore >= 0.3);

        var entities = await query
            .Take(filters.MaxNodes)
            .AsNoTracking()
            .ToListAsync();

        var entityIds = entities.Select(e => e.Id).ToHashSet();

        // Hole Beziehungen
        var relationships = await _context.UnifiedKnowledgeRelationships
            .Where(r => r.UserId == userId && r.IsActive &&
                       entityIds.Contains(r.SourceEntityId) &&
                       entityIds.Contains(r.TargetEntityId) &&
                       r.CurrentStrength >= filters.MinStrength)
            .AsNoTracking()
            .ToListAsync();

        // Erstelle Graph
        var graph = new KnowledgeGraphDto();

        // Layout berechnen (einfaches Force-Directed Layout Approximation)
        var subjectGroups = entities.GroupBy(e => e.Subject).ToList();
        var angleStep = 2 * Math.PI / Math.Max(subjectGroups.Count, 1);
        var subjectIndex = 0;

        foreach (var subjectGroup in subjectGroups)
        {
            var baseAngle = angleStep * subjectIndex;
            var radius = 300 + (subjectIndex % 2) * 100;
            var entitiesInGroup = subjectGroup.ToList();

            for (int i = 0; i < entitiesInGroup.Count; i++)
            {
                var entity = entitiesInGroup[i];
                var entityAngle = baseAngle + (i * 0.3) - (entitiesInGroup.Count * 0.15);

                graph.Nodes.Add(new GraphNode
                {
                    Id = entity.Id,
                    Label = entity.Name,
                    EntityType = entity.EntityType,
                    Subject = entity.Subject,
                    Topic = entity.Topic,
                    MasteryScore = entity.MasteryScore,
                    Size = GetNodeSize(entity.ImportanceScore),
                    Color = GetMasteryColor(entity.MasteryScore),
                    X = Math.Cos(entityAngle) * radius,
                    Y = Math.Sin(entityAngle) * radius
                });
            }

            subjectIndex++;
        }

        // Kanten hinzufügen
        foreach (var rel in relationships)
        {
            graph.Edges.Add(new GraphEdge
            {
                Id = rel.Id,
                Source = rel.SourceEntityId,
                Target = rel.TargetEntityId,
                RelationshipType = rel.RelationshipType,
                Strength = rel.CurrentStrength,
                IsPrerequisite = rel.IsPrerequisite
            });
        }

        // Metadaten
        graph.Metadata = new GraphMetadata
        {
            TotalNodes = graph.Nodes.Count,
            TotalEdges = graph.Edges.Count,
            SubjectCount = subjectGroups.Count,
            AverageMastery = entities.Any() ? entities.Average(e => e.MasteryScore) : 0,
            Subjects = subjectGroups.Select(g => g.Key).ToList()
        };

        return graph;
    }

    /// <summary>
    /// Holt Cluster-Visualisierung (2D-Projektion basierend auf semantischer Ähnlichkeit)
    /// </summary>
    public async Task<ClusterVisualizationDto> GetClusterVisualizationAsync(int userId, string? subject = null)
    {
        var query = _context.UnifiedKnowledgeEntities
            .Where(e => e.UserId == userId && e.IsActive && e.HasEmbedding);

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(e => e.Subject == subject);

        var entities = await query
            .Take(100)
            .AsNoTracking()
            .ToListAsync();

        var result = new ClusterVisualizationDto();

        // Gruppiere nach Topic für Cluster
        var topicGroups = entities.GroupBy(e => $"{e.Subject}:{e.Topic}").ToList();
        var random = new Random(42); // Deterministisch für konsistente Layouts

        var clusterIndex = 0;
        foreach (var group in topicGroups)
        {
            var centerX = (clusterIndex % 5) * 200 - 400;
            var centerY = (clusterIndex / 5) * 200 - 200;
            var clusterId = $"cluster_{clusterIndex}";

            var entitiesInCluster = group.ToList();

            result.Clusters.Add(new ClusterInfo
            {
                Id = clusterId,
                Label = group.Key,
                CenterX = centerX,
                CenterY = centerY,
                EntityCount = entitiesInCluster.Count,
                AverageMastery = entitiesInCluster.Average(e => e.MasteryScore)
            });

            // Verteile Entitäten um Cluster-Zentrum
            for (int i = 0; i < entitiesInCluster.Count; i++)
            {
                var entity = entitiesInCluster[i];
                var angle = (2 * Math.PI * i) / entitiesInCluster.Count;
                var radius = 30 + random.NextDouble() * 50;

                result.Points.Add(new ClusterPoint
                {
                    EntityId = entity.Id,
                    Label = entity.Name,
                    X = centerX + Math.Cos(angle) * radius,
                    Y = centerY + Math.Sin(angle) * radius,
                    ClusterId = clusterId,
                    MasteryScore = entity.MasteryScore
                });
            }

            clusterIndex++;
        }

        return result;
    }

    #endregion
}

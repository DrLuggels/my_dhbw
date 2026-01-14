using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services.UnifiedLearning;

/// <summary>
/// Priority & Prerequisites - deadline-aware priority calculation and prerequisite management
/// </summary>
public partial class UnifiedLearningService
{
    /// <inheritdoc />
    public async Task<List<UnifiedLearningPriority>> GetPrioritizedRecommendationsAsync(
        int userId,
        int limit = 10)
    {
        // First, recalculate all priorities
        await RecalculatePrioritiesAsync(userId);

        // Get active priorities sorted by composite score
        var priorities = await _context.Set<UnifiedLearningPriority>()
            .Include(p => p.UnifiedEntity)
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.CompositeScore)
            .Take(limit)
            .ToListAsync();

        return priorities;
    }

    /// <inheritdoc />
    public async Task RecalculatePrioritiesAsync(int userId)
    {
        // Get all entities for user
        var entities = await _context.Set<UnifiedKnowledgeEntity>()
            .Where(e => e.UserId == userId && e.IsActive)
            .ToListAsync();

        // Get upcoming deadlines (assignments and calendar events)
        var now = DateTime.UtcNow;
        var thirtyDaysLater = now.AddDays(30);

        var assignments = await _context.Set<MoodleAssignment>()
            .Where(a => a.UserId == userId && a.DueDate > now && a.DueDate < thirtyDaysLater)
            .ToListAsync();

        var calendarEvents = await _context.Set<CalendarEvent>()
            .Where(e => e.UserId == userId && e.StartTime > now && e.StartTime < thirtyDaysLater &&
                (e.Title.Contains("Prüfung") || e.Title.Contains("Klausur") || e.Title.Contains("Exam")))
            .ToListAsync();

        // Clear old priorities
        var oldPriorities = await _context.Set<UnifiedLearningPriority>()
            .Where(p => p.UserId == userId)
            .ToListAsync();
        _context.Set<UnifiedLearningPriority>().RemoveRange(oldPriorities);

        // Calculate new priorities
        var newPriorities = new List<UnifiedLearningPriority>();
        var rank = 1;

        foreach (var entity in entities)
        {
            var priority = new UnifiedLearningPriority
            {
                UserId = userId,
                UnifiedEntityId = entity.Id,
                Subject = entity.Subject,
                Topic = entity.Topic,
                EntityName = entity.Name,
                CurrentBloomLevel = entity.CurrentBloomLevel,
                CalculatedAt = now,
                IsActive = true
            };

            // Calculate mastery gap
            priority.MasteryGap = (1.0 - entity.EffectiveKnowledge) * 100;

            // Calculate decay amount
            priority.DecayAmount = (1.0 - entity.DecayFactor) * 100;

            // Calculate Bloom gap (assume target is level 3 for most topics)
            var targetBloom = 3;
            priority.TargetBloomLevel = targetBloom;
            priority.BloomGap = Math.Max(0, (targetBloom - entity.CurrentBloomLevel) / 5.0 * 100);

            // Find relevant deadline
            var relevantAssignment = assignments
                .FirstOrDefault(a => MatchesSubject(a.Name, entity.Subject));
            var relevantEvent = calendarEvents
                .FirstOrDefault(e => MatchesSubject(e.Title, entity.Subject));

            if (relevantAssignment != null)
            {
                priority.MoodleAssignmentId = relevantAssignment.Id;
                priority.Deadline = relevantAssignment.DueDate;
                priority.RelatedEventName = relevantAssignment.Name;
            }
            else if (relevantEvent != null)
            {
                priority.CalendarEventId = relevantEvent.Id;
                priority.Deadline = relevantEvent.StartTime;
                priority.RelatedEventName = relevantEvent.Title;
            }

            // Calculate deadline urgency
            if (priority.Deadline.HasValue)
            {
                var daysUntil = (priority.Deadline.Value - now).TotalDays;
                priority.DeadlineUrgency = Math.Max(0, 100 * (1 - daysUntil / 30.0));

                // Topic relevance based on deadline match
                priority.TopicRelevance = MatchesSubject(priority.RelatedEventName ?? "", entity.Subject) ? 80 : 40;
            }
            else
            {
                priority.DeadlineUrgency = 0;
                priority.TopicRelevance = 30; // Base relevance
            }

            // Check prerequisites
            var prereqCheck = await CheckPrerequisitesAsync(entity.Id, userId);
            priority.IsBlocked = !prereqCheck.CanProceed;
            if (priority.IsBlocked)
            {
                priority.BlockReason = prereqCheck.BlockReason;
                priority.BlockingPrerequisites = prereqCheck.BlockingPrerequisites;
            }

            // Calculate composite score
            priority.CalculateCompositeScore();

            newPriorities.Add(priority);
        }

        // Sort by composite score and assign ranks
        newPriorities = newPriorities
            .OrderByDescending(p => p.CompositeScore)
            .ToList();

        foreach (var priority in newPriorities)
        {
            priority.Rank = rank++;
        }

        _context.Set<UnifiedLearningPriority>().AddRange(newPriorities);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Recalculated {Count} priorities for user {UserId}",
            newPriorities.Count, userId);
    }

    /// <inheritdoc />
    public async Task<UnifiedLearningPriority?> GetNextRecommendationAsync(int userId)
    {
        var priorities = await GetPrioritizedRecommendationsAsync(userId, 1);
        return priorities.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<UnifiedPrerequisiteCheckResult> CheckPrerequisitesAsync(
        int entityId,
        int userId)
    {
        var result = new UnifiedPrerequisiteCheckResult
        {
            CanProceed = true,
            TotalPrerequisites = 0,
            MetPrerequisites = 0,
            BlockingPrerequisites = new List<BlockingPrerequisiteInfo>()
        };

        // Find all prerequisite relationships where this entity is the target
        var prerequisites = await _context.Set<UnifiedKnowledgeRelationship>()
            .Include(r => r.SourceEntity)
            .Where(r => r.TargetEntityId == entityId &&
                r.IsActive &&
                r.IsPrerequisite &&
                r.UserId == userId)
            .ToListAsync();

        if (!prerequisites.Any())
            return result;

        result.TotalPrerequisites = prerequisites.Count;

        foreach (var prereq in prerequisites)
        {
            var sourceEntity = prereq.SourceEntity;
            if (sourceEntity == null || !sourceEntity.IsActive)
                continue;

            var isMet = sourceEntity.MasteryScore >= prereq.RequiredMasteryLevel;

            if (isMet)
            {
                result.MetPrerequisites++;
            }
            else
            {
                var blocking = new BlockingPrerequisiteInfo
                {
                    EntityId = sourceEntity.Id,
                    EntityName = sourceEntity.Name,
                    Subject = sourceEntity.Subject,
                    Topic = sourceEntity.Topic,
                    CurrentMastery = sourceEntity.MasteryScore,
                    RequiredMastery = prereq.RequiredMasteryLevel,
                    IsStrict = prereq.IsStrict
                };

                result.BlockingPrerequisites.Add(blocking);

                if (prereq.IsStrict)
                {
                    result.CanProceed = false;
                }
            }
        }

        if (!result.CanProceed)
        {
            result.BlockReason = $"{result.BlockingPrerequisites.Count} Voraussetzung(en) nicht erfüllt";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<UnifiedEntityDto>> GetLearningPathAsync(
        int entityId,
        int userId)
    {
        var learningPath = new List<UnifiedKnowledgeEntity>();
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(entityId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (visited.Contains(currentId))
                continue;

            visited.Add(currentId);

            // Get prerequisites for current entity
            var prerequisites = await _context.Set<UnifiedKnowledgeRelationship>()
                .Include(r => r.SourceEntity)
                .Where(r => r.TargetEntityId == currentId &&
                    r.IsActive &&
                    r.IsPrerequisite &&
                    r.UserId == userId)
                .ToListAsync();

            foreach (var prereq in prerequisites)
            {
                if (prereq.SourceEntity != null &&
                    prereq.SourceEntity.IsActive &&
                    !visited.Contains(prereq.SourceEntityId))
                {
                    // Add to path if not mastered
                    if (prereq.SourceEntity.MasteryScore < prereq.RequiredMasteryLevel)
                    {
                        learningPath.Add(prereq.SourceEntity);
                    }
                    queue.Enqueue(prereq.SourceEntityId);
                }
            }
        }

        // Sort by mastery (lowest first) - these need the most work
        return learningPath
            .OrderBy(e => e.MasteryScore)
            .Select(MapToDto)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<UnifiedKnowledgeRelationship> CreatePrerequisiteAsync(
        int userId,
        int prerequisiteEntityId,
        int dependentEntityId,
        double requiredMasteryLevel = 0.6,
        bool isStrict = true)
    {
        // Check for cycles
        if (await WouldCreateCycleAsync(userId, prerequisiteEntityId, dependentEntityId))
        {
            throw new InvalidOperationException(
                "Diese Voraussetzung würde einen Zyklus erzeugen und kann nicht erstellt werden.");
        }

        var relationship = await CreateOrUpdateRelationshipAsync(
            userId,
            prerequisiteEntityId,
            dependentEntityId,
            UnifiedRelationshipTypes.Prerequisite);

        relationship.RequiredMasteryLevel = requiredMasteryLevel;
        relationship.IsStrict = isStrict;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created prerequisite: Entity {Prereq} -> Entity {Dependent} (required: {Required}%)",
            prerequisiteEntityId, dependentEntityId, requiredMasteryLevel * 100);

        return relationship;
    }

    /// <summary>
    /// Check if adding a prerequisite would create a cycle
    /// </summary>
    private async Task<bool> WouldCreateCycleAsync(
        int userId,
        int prerequisiteId,
        int dependentId)
    {
        // DFS to check if dependentId can reach prerequisiteId
        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(dependentId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == prerequisiteId)
                return true; // Cycle detected

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            // Get all prerequisites of current entity
            var prerequisites = await _context.Set<UnifiedKnowledgeRelationship>()
                .Where(r => r.TargetEntityId == current &&
                    r.IsActive &&
                    r.IsPrerequisite &&
                    r.UserId == userId)
                .Select(r => r.SourceEntityId)
                .ToListAsync();

            foreach (var prereqId in prerequisites)
            {
                if (!visited.Contains(prereqId))
                    stack.Push(prereqId);
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a subject matches a given text (for deadline matching)
    /// </summary>
    private bool MatchesSubject(string text, string subject)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(subject))
            return false;

        var normalizedText = text.ToLowerInvariant();
        var normalizedSubject = subject.ToLowerInvariant();

        return normalizedText.Contains(normalizedSubject) ||
               normalizedSubject.Contains(normalizedText) ||
               // Common abbreviations
               (normalizedSubject == "mathematik" && normalizedText.Contains("mathe")) ||
               (normalizedSubject == "programmierung" && (normalizedText.Contains("prog") || normalizedText.Contains("java")));
    }
}

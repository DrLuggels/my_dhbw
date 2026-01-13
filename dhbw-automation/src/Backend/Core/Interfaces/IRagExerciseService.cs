using DHBWAutomation.Backend.Core.Models;

namespace DHBWAutomation.Backend.Core.Interfaces;

/// <summary>
/// Service for generating personalized exercises using RAG (Retrieval-Augmented Generation).
/// Combines user's document chunks with Claude for context-aware exercise generation.
/// </summary>
public interface IRagExerciseService
{
    /// <summary>
    /// Generates a personalized exercise for a knowledge node using RAG.
    /// </summary>
    Task<RagExerciseResult> GenerateExerciseAsync(
        int userId,
        int nodeId,
        string difficulty,
        ExerciseGenerationOptions? options = null);

    /// <summary>
    /// Generates an exercise for a specific topic (without existing node).
    /// </summary>
    Task<RagExerciseResult> GenerateExerciseForTopicAsync(
        int userId,
        string subject,
        string topic,
        string difficulty,
        ExerciseGenerationOptions? options = null);

    /// <summary>
    /// Retrieves relevant context chunks from user's documents.
    /// </summary>
    Task<List<RetrievedChunk>> RetrieveContextAsync(
        int userId,
        string topic,
        int topK = 5,
        double threshold = 0.5);

    /// <summary>
    /// Generates a batch of exercises for a learning session.
    /// </summary>
    Task<List<RagExerciseResult>> GenerateSessionExercisesAsync(
        int userId,
        int count = 5,
        ExerciseGenerationOptions? options = null);

    /// <summary>
    /// Generates an exam-style set of exercises with 20/40/40 distribution.
    /// </summary>
    Task<List<RagExerciseResult>> GenerateExamExercisesAsync(
        int userId,
        string subject,
        int easyCount = 4,
        int mediumCount = 8,
        int hardCount = 8);
}

/// <summary>
/// Options for exercise generation.
/// </summary>
public class ExerciseGenerationOptions
{
    /// <summary>
    /// Whether to include context from user's documents.
    /// </summary>
    public bool UseRag { get; set; } = true;

    /// <summary>
    /// Maximum number of context chunks to retrieve.
    /// </summary>
    public int MaxContextChunks { get; set; } = 5;

    /// <summary>
    /// Minimum similarity threshold for context retrieval.
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.5;

    /// <summary>
    /// Exercise type: "multiple_choice", "fill_blank", "true_false", "open_ended", "interactive"
    /// </summary>
    public string ExerciseType { get; set; } = "multiple_choice";

    /// <summary>
    /// Whether to include hints in the exercise.
    /// </summary>
    public bool IncludeHints { get; set; } = true;

    /// <summary>
    /// Whether to include detailed explanation.
    /// </summary>
    public bool IncludeExplanation { get; set; } = true;

    /// <summary>
    /// Language for the exercise (default: German).
    /// </summary>
    public string Language { get; set; } = "de";

    /// <summary>
    /// Optional: Focus on specific subtopics.
    /// </summary>
    public List<string>? FocusSubtopics { get; set; }
}

/// <summary>
/// A RAG-generated exercise with metadata.
/// Named differently to avoid conflict with Models.GeneratedExercise (database entity).
/// </summary>
public class RagExerciseResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "multiple_choice";
    public string Difficulty { get; set; } = "medium";
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Subtopic { get; set; }

    // Exercise content
    public string Question { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public List<string>? Hints { get; set; }

    // Context used for generation
    public List<string> SourceDocuments { get; set; } = new();
    public List<RetrievedChunk> UsedChunks { get; set; } = new();
    public bool WasRagUsed { get; set; }

    // For interactive exercises (Brilliant-style steps)
    public List<RagExerciseStep>? Steps { get; set; }

    // Metadata
    public int? NodeId { get; set; }
    public double EstimatedSuccessProbability { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A step in an interactive exercise.
/// </summary>
public class RagExerciseStep
{
    public int StepNumber { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public string? Question { get; set; }
    public List<string>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? Hint { get; set; }
    public string? Explanation { get; set; }
}

/// <summary>
/// A document chunk retrieved for context.
/// </summary>
public class RetrievedChunk
{
    public int ChunkId { get; set; }
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TopicLabel { get; set; }
    public string? Summary { get; set; }
    public double SimilarityScore { get; set; }
    public string? PageNumbers { get; set; }
}

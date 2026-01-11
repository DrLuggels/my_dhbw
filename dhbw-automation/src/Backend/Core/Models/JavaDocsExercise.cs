using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

/// <summary>
/// Exercises parsed from jappuccini/java-docs GitHub repository
/// </summary>
[Table("java_docs_exercises")]
public class JavaDocsExercise
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// File path within the repository (e.g., docs/exercises/oop/exercise01.mdx)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Exercise title (from frontmatter or first heading)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Main topic (e.g., "OOP", "Generics", "Streams")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Subtopic if applicable
    /// </summary>
    [MaxLength(200)]
    public string? Subtopic { get; set; }

    /// <summary>
    /// Difficulty level: "easy", "medium", "hard", "exam"
    /// </summary>
    [MaxLength(50)]
    public string? Difficulty { get; set; }

    /// <summary>
    /// Exercise type: "coding", "theory", "exam", "practice"
    /// </summary>
    [MaxLength(50)]
    public string ExerciseType { get; set; } = "practice";

    /// <summary>
    /// Raw MDX content from the file
    /// </summary>
    [Required]
    [Column(TypeName = "LONGTEXT")]
    public string RawMdxContent { get; set; } = string.Empty;

    /// <summary>
    /// Parsed/cleaned content (markdown without MDX components)
    /// </summary>
    [Column(TypeName = "LONGTEXT")]
    public string? ParsedContent { get; set; }

    /// <summary>
    /// Extracted code snippets as JSON array
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? CodeSnippets { get; set; }

    /// <summary>
    /// Solution code if available
    /// </summary>
    [Column(TypeName = "LONGTEXT")]
    public string? SolutionCode { get; set; }

    /// <summary>
    /// Tags from frontmatter (JSON array)
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? Tags { get; set; }

    /// <summary>
    /// Frontmatter metadata (JSON)
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? Frontmatter { get; set; }

    /// <summary>
    /// Git commit hash when last updated
    /// </summary>
    [MaxLength(64)]
    public string? GitCommitHash { get; set; }

    /// <summary>
    /// Whether this exercise has a vector embedding
    /// </summary>
    public bool HasEmbedding { get; set; } = false;

    /// <summary>
    /// Qdrant point ID for semantic search
    /// </summary>
    [MaxLength(100)]
    public string? QdrantPointId { get; set; }

    /// <summary>
    /// How many times users have practiced this
    /// </summary>
    public int PracticeCount { get; set; } = 0;

    /// <summary>
    /// Average score when practiced
    /// </summary>
    public double AverageScore { get; set; } = 0.0;

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

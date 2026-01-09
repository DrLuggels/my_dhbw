using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("projects")]
public class Project
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Priority { get; set; } = "medium"; // "low", "medium", "high"

    [Required]
    [MaxLength(50)]
    public string Interest { get; set; } = "medium"; // "low", "medium", "high", "fun"

    [Required]
    [MaxLength(50)]
    public string Importance { get; set; } = "medium"; // "low", "medium", "high", "critical"

    // Zeit-Allocation
    public int? WeeklyMinutes { get; set; } // Wie viel Zeit pro Woche

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "idea"; // "idea", "planning", "active", "paused", "completed"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<Todo> Todos { get; set; } = new List<Todo>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Core.Models;

[Table("courses")]
public class CourseInfo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string CourseName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CourseCode { get; set; }

    [MaxLength(100)]
    public string? Professor { get; set; }

    [MaxLength(50)]
    public string? Semester { get; set; }

    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? MoodleUrl { get; set; }

    [MaxLength(100)]
    public string? MoodleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Column(TypeName = "JSON")]
    public string? AdditionalInfo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

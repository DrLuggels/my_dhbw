using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DHBWAutomation.Backend.Core.Models;

[Table("todo_lists")]
public class TodoList
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Icon { get; set; } = "mdi-checkbox-marked-circle-outline";

    [MaxLength(7)]
    public string Color { get; set; } = "#1976D2";

    public int SortOrder { get; set; } = 0;

    public bool IsDefault { get; set; } = false;

    public bool IsArchiveList { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<Todo> Todos { get; set; } = new List<Todo>();
}

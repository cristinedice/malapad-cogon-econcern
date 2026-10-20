using System.ComponentModel.DataAnnotations;

namespace MalapadCogonEConcern.Models.Entities;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string IconClass { get; set; } = "bi-tag";

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Concern> Concerns { get; set; } = new List<Concern>();
}

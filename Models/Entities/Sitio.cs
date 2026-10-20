using System.ComponentModel.DataAnnotations;

namespace MalapadCogonEConcern.Models.Entities;

public class Sitio
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Concern> Concerns { get; set; } = new List<Concern>();
}

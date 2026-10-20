using System.ComponentModel.DataAnnotations;

namespace MalapadCogonEConcern.Models.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(100)]
    public string EntityId { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Details { get; set; } = string.Empty;

    [StringLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

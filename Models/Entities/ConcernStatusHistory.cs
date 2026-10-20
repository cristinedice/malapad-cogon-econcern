using System.ComponentModel.DataAnnotations;
using MalapadCogonEConcern.Models.Enums;

namespace MalapadCogonEConcern.Models.Entities;

public class ConcernStatusHistory
{
    public int Id { get; set; }

    public int ConcernId { get; set; }
    public virtual Concern? Concern { get; set; }

    public ConcernStatus PreviousStatus { get; set; }
    public ConcernStatus NewStatus { get; set; }

    public string? ChangedByUserId { get; set; }
    public virtual ApplicationUser? ChangedByUser { get; set; }

    [StringLength(1000)]
    public string? PublicRemarks { get; set; }

    [StringLength(1000)]
    public string? InternalRemarks { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using MalapadCogonEConcern.Models.Enums;

namespace MalapadCogonEConcern.Models.Entities;

public class Concern
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string VerificationPinHash { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string TrackingSalt { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Description { get; set; } = string.Empty;

    // Foreign Keys
    public int CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    public int SitioId { get; set; }
    public virtual Sitio? Sitio { get; set; }

    [StringLength(200)]
    public string LandmarkOrAddress { get; set; } = string.Empty;

    // Complainant Information (Protected / Confidential under RA 10173)
    [StringLength(100)]
    public string? ComplainantName { get; set; }

    [StringLength(50)]
    public string? ComplainantContact { get; set; }

    public SubmissionChannel Channel { get; set; } = SubmissionChannel.Online;
    public ConcernStatus Status { get; set; } = ConcernStatus.Submitted;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    // Assignment
    public string? AssignedToUserId { get; set; }
    public virtual ApplicationUser? AssignedToUser { get; set; }

    // External Forwarding (LGU Sigma, CAPELCO, DPWH, MENRO, etc.)
    [StringLength(120)]
    public string? ForwardedToAgency { get; set; }

    [StringLength(60)]
    public string? ForwardedReferenceNo { get; set; }

    [StringLength(1000)]
    public string? ForwardedRemarks { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navigation Collections
    public virtual ICollection<ConcernAttachment> Attachments { get; set; } = new List<ConcernAttachment>();
    public virtual ICollection<ConcernStatusHistory> StatusHistories { get; set; } = new List<ConcernStatusHistory>();
}

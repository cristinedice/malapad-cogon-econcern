using System.ComponentModel.DataAnnotations;
using MalapadCogonEConcern.Models.Enums;

namespace MalapadCogonEConcern.Models.Entities;

public class ConcernAttachment
{
    public int Id { get; set; }

    public int ConcernId { get; set; }
    public virtual Concern? Concern { get; set; }

    [Required]
    [StringLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public AttachmentType AttachmentType { get; set; } = AttachmentType.SubmissionPhoto;

    public string? UploadedByUserId { get; set; }
    public virtual ApplicationUser? UploadedByUser { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

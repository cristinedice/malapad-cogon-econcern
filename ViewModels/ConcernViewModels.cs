using System.ComponentModel.DataAnnotations;
using MalapadCogonEConcern.Models.Enums;

namespace MalapadCogonEConcern.ViewModels;

public class ConcernSubmitViewModel
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 150 characters.")]
    [Display(Name = "Concern Title (Ulo sang Reklamo)")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category.")]
    [Display(Name = "Concern Category (Kategorya)")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please select a Sitio or Purok.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Sitio or Purok.")]
    [Display(Name = "Sitio / Purok")]
    public int SitioId { get; set; }

    [Required(ErrorMessage = "Specific location or landmark is required.")]
    [StringLength(200, ErrorMessage = "Landmark cannot exceed 200 characters.")]
    [Display(Name = "Specific Location / Landmark (Eksaktong Lugar o Landmark)")]
    public string LandmarkOrAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Detailed description is required.")]
    [StringLength(3000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 3,000 characters.")]
    [Display(Name = "Detailed Description (Kabilugan nga Paathag)")]
    public string Description { get; set; } = string.Empty;

    // Optional Complainant Info (Protected under RA 10173)
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Your Name (Pangalan - Opsyonal kag Kompidensyal)")]
    public string? ComplainantName { get; set; }

    [StringLength(50, ErrorMessage = "Contact info cannot exceed 50 characters.")]
    [Display(Name = "Mobile Number (Numero sang Telepono - Opsyonal)")]
    public string? ComplainantContact { get; set; }

    // 4-Digit Security PIN for status tracking
    [Required(ErrorMessage = "4-digit secret PIN is required.")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "PIN must be exactly 4 numeric digits (e.g., 1234).")]
    [DataType(DataType.Password)]
    [Display(Name = "4-Digit Secret PIN (4-Digit nga Numero para sa Pag-check)")]
    public string VerificationPin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your 4-digit PIN.")]
    [Compare("VerificationPin", ErrorMessage = "PIN confirmation does not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm 4-Digit PIN")]
    public string ConfirmPin { get; set; } = string.Empty;

    // Optional Photo
    [Display(Name = "Upload Photo Attachment (Litrato sang Reklamo - Opsyonal, max 5MB)")]
    public IFormFile? PhotoAttachment { get; set; }

    // Privacy Consent
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the Data Privacy Notice to submit.")]
    [Display(Name = "I understand and agree to the Data Privacy Notice under RA 10173.")]
    public bool PrivacyConsent { get; set; }
}

public class ConcernConfirmationViewModel
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string VerificationPin { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SitioName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ConcernTrackViewModel
{
    [Required(ErrorMessage = "Reference Number is required.")]
    [Display(Name = "Reference Number (e.g. MC-2026-0001)")]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "4-Digit PIN is required.")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "PIN must be 4 digits.")]
    [DataType(DataType.Password)]
    [Display(Name = "4-Digit Secret PIN")]
    public string VerificationPin { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class ConcernTrackResultViewModel
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = "bi-tag";
    public string SitioName { get; set; } = string.Empty;
    public string LandmarkOrAddress { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ConcernStatus CurrentStatus { get; set; }
    public PriorityLevel Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ForwardedToAgency { get; set; }
    public string? ForwardedRemarks { get; set; }

    public string? SubmissionPhotoUrl { get; set; }
    public string? ResolutionPhotoUrl { get; set; }

    public List<PublicStatusHistoryItem> Timeline { get; set; } = new();
}

public class PublicStatusHistoryItem
{
    public DateTime Timestamp { get; set; }
    public ConcernStatus Status { get; set; }
    public string? PublicRemarks { get; set; }
    public string? ChangedByPosition { get; set; }
}

using System.ComponentModel.DataAnnotations;
using MalapadCogonEConcern.Models.Entities;
using MalapadCogonEConcern.Models.Enums;

namespace MalapadCogonEConcern.ViewModels;

public class AdminConcernFilterViewModel
{
    public string? Search { get; set; }
    public ConcernStatus? Status { get; set; }
    public int? CategoryId { get; set; }
    public int? SitioId { get; set; }
    public string? AssignedToUserId { get; set; }

    public List<Concern> Concerns { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Sitio> Sitios { get; set; } = new();
    public List<ApplicationUser> StaffUsers { get; set; } = new();

    public int TotalCount { get; set; }
}

public class AdminConcernDetailViewModel
{
    public Concern Concern { get; set; } = default!;
    public List<ApplicationUser> AvailableStaff { get; set; } = new();

    public AdminStatusUpdateViewModel StatusUpdate { get; set; } = new();
    public AdminForwardViewModel ForwardUpdate { get; set; } = new();
}

public class AdminStatusUpdateViewModel
{
    public int ConcernId { get; set; }

    [Required]
    public ConcernStatus NewStatus { get; set; }

    [Required(ErrorMessage = "Public remarks are required for transparency.")]
    [StringLength(1000)]
    public string PublicRemarks { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? InternalRemarks { get; set; }

    public IFormFile? ResolutionPhoto { get; set; }
}

public class AdminForwardViewModel
{
    public int ConcernId { get; set; }

    [Required(ErrorMessage = "Agency name is required.")]
    [StringLength(120)]
    public string AgencyName { get; set; } = string.Empty;

    [StringLength(60)]
    public string? EndorsementRefNo { get; set; }

    [Required(ErrorMessage = "Endorsement notes are required.")]
    [StringLength(1000)]
    public string EndorsementNotes { get; set; } = string.Empty;
}

public class AdminCreateAssistedViewModel
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150, MinimumLength = 5)]
    [Display(Name = "Concern Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Sitio / Purok is required.")]
    [Display(Name = "Sitio / Purok")]
    public int SitioId { get; set; }

    [Required(ErrorMessage = "Landmark is required.")]
    [StringLength(200)]
    [Display(Name = "Landmark / Location")]
    public string LandmarkOrAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(3000, MinimumLength = 10)]
    [Display(Name = "Detailed Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Complainant name is required for in-person records.")]
    [StringLength(100)]
    [Display(Name = "Complainant Full Name")]
    public string ComplainantName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Contact Number")]
    public string? ComplainantContact { get; set; }

    [Display(Name = "Intake Channel")]
    public SubmissionChannel Channel { get; set; } = SubmissionChannel.WalkIn;

    [Display(Name = "Initial Priority")]
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    [Display(Name = "Initial Photo (Optional)")]
    public IFormFile? PhotoAttachment { get; set; }

    [Required(ErrorMessage = "4-digit PIN is required for resident tracking.")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "PIN must be 4 digits.")]
    [Display(Name = "4-Digit Resident PIN")]
    public string VerificationPin { get; set; } = string.Empty;
}

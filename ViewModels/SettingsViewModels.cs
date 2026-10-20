using System.ComponentModel.DataAnnotations;
using MalapadCogonEConcern.Models.Entities;

namespace MalapadCogonEConcern.ViewModels;

public class SettingsDashboardViewModel
{
    public List<Category> Categories { get; set; } = new();
    public List<Sitio> Sitios { get; set; } = new();
    public List<StaffUserItemViewModel> StaffUsers { get; set; } = new();
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
}

public class StaffUserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AssignedCount { get; set; }
}

public class CreateStaffViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position / Designation is required.")]
    [StringLength(100)]
    [Display(Name = "Barangay Position (e.g. Barangay Tanod, Desk Officer, Kagawad)")]
    public string Position { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress]
    [Display(Name = "Official Email / Login")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [Display(Name = "System Access Role")]
    public string Role { get; set; } = "Staff";

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Bootstrap Icon Class (e.g. bi-cone-striped, bi-water)")]
    public string IconClass { get; set; } = "bi-tag";

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class SitioFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Sitio / Purok name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

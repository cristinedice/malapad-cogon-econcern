using Microsoft.AspNetCore.Identity;

namespace MalapadCogonEConcern.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Concern> AssignedConcerns { get; set; } = new List<Concern>();
    public virtual ICollection<ConcernStatusHistory> StatusHistories { get; set; } = new List<ConcernStatusHistory>();
}

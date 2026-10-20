using MalapadCogonEConcern.Models.Entities;

namespace MalapadCogonEConcern.ViewModels;

public class HomeIndexViewModel
{
    public List<Category> Categories { get; set; } = new();
    public int TotalConcerns { get; set; }
    public int InProgressConcerns { get; set; }
    public int ResolvedConcerns { get; set; }
    public string BarangayName { get; set; } = "Barangay Malapad, Cogon";
    public string Municipality { get; set; } = "Sigma";
    public string Province { get; set; } = "Capiz";
}

public class UserDashboardViewModel
{
    public ApplicationUser User { get; set; } = default!;
    public int NewCount { get; set; }
    public int OngoingCount { get; set; }
    public int AcknowledgeCount { get; set; }
    public int CompletedCount { get; set; }
}

using MalapadCogonEConcern.Models.Entities;

namespace MalapadCogonEConcern.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalConcerns { get; set; }
    public int UnderReviewConcerns { get; set; }
    public int InProgressConcerns { get; set; }
    public int ForwardedConcerns { get; set; }
    public int ResolvedConcerns { get; set; }
    public int LongPendingConcerns { get; set; }

    public List<Concern> RecentConcerns { get; set; } = new();
    public List<CategoryStat> CategoryBreakdown { get; set; } = new();
}

public class CategoryStat
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

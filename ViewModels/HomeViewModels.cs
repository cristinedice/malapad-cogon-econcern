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

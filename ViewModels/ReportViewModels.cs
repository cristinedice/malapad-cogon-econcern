using MalapadCogonEConcern.Models.Entities;
using MalapadCogonEConcern.Models.Enums;

namespace MalapadCogonEConcern.ViewModels;

public class ReportFilterViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CategoryId { get; set; }
    public int? SitioId { get; set; }

    // Summary KPIs
    public int TotalReceived { get; set; }
    public int TotalResolved { get; set; }
    public int TotalPending { get; set; }
    public int TotalForwarded { get; set; }
    public int TotalRejected { get; set; }

    public double? AverageResolutionDays { get; set; }
    public double ResolutionRate { get; set; }

    // Breakdown Collections
    public List<ReportStatItem> CategoryBreakdown { get; set; } = new();
    public List<ReportStatItem> SitioBreakdown { get; set; } = new();
    public List<ReportStatItem> ChannelBreakdown { get; set; } = new();

    // Data for table & lookups
    public List<Concern> Concerns { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Sitio> Sitios { get; set; } = new();
}

public class ReportStatItem
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MalapadCogonEConcern.Data;
using MalapadCogonEConcern.Models.Entities;
using MalapadCogonEConcern.Models.Enums;
using MalapadCogonEConcern.Services;
using MalapadCogonEConcern.ViewModels;

namespace MalapadCogonEConcern.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public ReportsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? categoryId, int? sitioId)
    {
        var query = _context.Concerns
            .Include(c => c.Category)
            .Include(c => c.Sitio)
            .Include(c => c.AssignedToUser)
            .AsQueryable();

        // Date range filter
        if (startDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt <= endUtc);
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (sitioId.HasValue && sitioId.Value > 0)
        {
            query = query.Where(c => c.SitioId == sitioId.Value);
        }

        var concerns = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var total = concerns.Count;
        var resolved = concerns.Count(c => c.Status == ConcernStatus.Resolved);
        var pending = concerns.Count(c => c.Status == ConcernStatus.Submitted ||
                                          c.Status == ConcernStatus.UnderReview ||
                                          c.Status == ConcernStatus.Accepted ||
                                          c.Status == ConcernStatus.Assigned ||
                                          c.Status == ConcernStatus.InProgress);
        var forwarded = concerns.Count(c => c.Status == ConcernStatus.ForwardedToAgency);
        var rejected = concerns.Count(c => c.Status == ConcernStatus.Rejected);

        // Calculate average resolution time
        double? avgResolutionDays = null;
        var resolvedWithDates = concerns
            .Where(c => c.Status == ConcernStatus.Resolved && c.ResolvedAt.HasValue)
            .ToList();

        if (resolvedWithDates.Any())
        {
            avgResolutionDays = Math.Round(resolvedWithDates.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalDays), 1);
        }

        double resolutionRate = total > 0 ? Math.Round(((double)resolved / total) * 100, 1) : 0;

        // Breakdown by Category
        var categoryBreakdown = concerns
            .GroupBy(c => c.Category?.Name ?? "Unclassified")
            .Select(g => new ReportStatItem
            {
                Name = g.Key,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round(((double)g.Count() / total) * 100, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Breakdown by Sitio / Purok
        var sitioBreakdown = concerns
            .GroupBy(c => c.Sitio?.Name ?? "Unassigned")
            .Select(g => new ReportStatItem
            {
                Name = g.Key,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round(((double)g.Count() / total) * 100, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Breakdown by Channel
        var channelBreakdown = concerns
            .GroupBy(c => c.Channel.ToString())
            .Select(g => new ReportStatItem
            {
                Name = g.Key,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round(((double)g.Count() / total) * 100, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var viewModel = new ReportFilterViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            CategoryId = categoryId,
            SitioId = sitioId,
            TotalReceived = total,
            TotalResolved = resolved,
            TotalPending = pending,
            TotalForwarded = forwarded,
            TotalRejected = rejected,
            AverageResolutionDays = avgResolutionDays,
            ResolutionRate = resolutionRate,
            CategoryBreakdown = categoryBreakdown,
            SitioBreakdown = sitioBreakdown,
            ChannelBreakdown = channelBreakdown,
            Concerns = concerns,
            Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(),
            Sitios = await _context.Sitios.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(DateTime? startDate, DateTime? endDate, int? categoryId, int? sitioId)
    {
        var query = _context.Concerns
            .Include(c => c.Category)
            .Include(c => c.Sitio)
            .Include(c => c.AssignedToUser)
            .AsQueryable();

        if (startDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt <= endUtc);
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (sitioId.HasValue && sitioId.Value > 0)
        {
            query = query.Where(c => c.SitioId == sitioId.Value);
        }

        var list = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var sb = new StringBuilder();

        // UTF-8 BOM for Microsoft Excel compatibility
        sb.Append('\uFEFF');

        // CSV Header (Strictly excluding confidential complainant personal names/phones)
        sb.AppendLine("Reference Number,Date Submitted,Category,Sitio / Purok,Landmark,Channel,Priority,Status,Assigned Staff,Date Resolved,Resolution Days,Forwarded Agency,Forwarded Ref");

        foreach (var c in list)
        {
            var refNo = EscapeCsv(c.ReferenceNumber);
            var dateSubmitted = c.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            var category = EscapeCsv(c.Category?.Name ?? "");
            var sitio = EscapeCsv(c.Sitio?.Name ?? "");
            var landmark = EscapeCsv(c.LandmarkOrAddress);
            var channel = c.Channel.ToString();
            var priority = c.Priority.ToString();
            var status = c.Status.ToString();
            var assignedTo = EscapeCsv(c.AssignedToUser?.FullName ?? "Unassigned");
            var dateResolved = c.ResolvedAt.HasValue ? c.ResolvedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "";
            var resDays = c.ResolvedAt.HasValue ? Math.Round((c.ResolvedAt.Value - c.CreatedAt).TotalDays, 1).ToString() : "";
            var fwdAgency = EscapeCsv(c.ForwardedToAgency ?? "");
            var fwdRef = EscapeCsv(c.ForwardedReferenceNo ?? "");

            sb.AppendLine($"{refNo},{dateSubmitted},{category},{sitio},{landmark},{channel},{priority},{status},{assignedTo},{dateResolved},{resDays},{fwdAgency},{fwdRef}");
        }

        var user = await _userManager.GetUserAsync(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(
            user?.Id,
            "ExportCsv",
            "Report",
            "CSV-Export",
            $"Exported {list.Count} records to CSV.",
            ip);

        var fileName = $"MalapadCogon_eConcern_Report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    private static string EscapeCsv(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "\"\"";
        }

        if (text.Contains(",") || text.Contains("\"") || text.Contains("\n") || text.Contains("\r"))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return $"\"{text}\"";
    }
}

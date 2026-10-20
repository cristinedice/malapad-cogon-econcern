using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MalapadCogonEConcern.Data;
using MalapadCogonEConcern.Models;
using MalapadCogonEConcern.Models.Enums;
using MalapadCogonEConcern.ViewModels;

namespace MalapadCogonEConcern.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context,
        IConfiguration config,
        ILogger<HomeController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var total = await _context.Concerns.CountAsync();
        var inProgress = await _context.Concerns
            .CountAsync(c => c.Status == ConcernStatus.InProgress || c.Status == ConcernStatus.Assigned);
        var resolved = await _context.Concerns
            .CountAsync(c => c.Status == ConcernStatus.Resolved);

        var viewModel = new HomeIndexViewModel
        {
            Categories = categories,
            TotalConcerns = total,
            InProgressConcerns = inProgress,
            ResolvedConcerns = resolved,
            BarangayName = _config["SystemSettings:BarangayName"] ?? "Barangay Malapad, Cogon",
            Municipality = _config["SystemSettings:Municipality"] ?? "Sigma",
            Province = _config["SystemSettings:Province"] ?? "Capiz"
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

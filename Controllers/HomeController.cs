using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MalapadCogonEConcern.Data;
using MalapadCogonEConcern.Models;
using MalapadCogonEConcern.Models.Entities;
using MalapadCogonEConcern.Models.Enums;
using MalapadCogonEConcern.ViewModels;

namespace MalapadCogonEConcern.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        ILogger<HomeController> logger)
    {
        _context = context;
        _userManager = userManager;
        _config = config;
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var query = _context.Concerns.AsQueryable();

        // If regular resident, scope to their own reports
        if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            query = query.Where(c => c.CreatedByUserId == currentUser.Id ||
                                     c.ComplainantContact == currentUser.PhoneNumber ||
                                     c.ComplainantName == currentUser.FullName);
        }

        var newCount = await query.CountAsync(c => c.Status == ConcernStatus.Submitted);
        var ongoingCount = await query.CountAsync(c => c.Status == ConcernStatus.InProgress || c.Status == ConcernStatus.Assigned);
        var ackCount = await query.CountAsync(c => c.Status == ConcernStatus.UnderReview || c.Status == ConcernStatus.Accepted);
        var completedCount = await query.CountAsync(c => c.Status == ConcernStatus.Resolved);

        var viewModel = new UserDashboardViewModel
        {
            User = currentUser,
            NewCount = newCount,
            OngoingCount = ongoingCount,
            AcknowledgeCount = ackCount,
            CompletedCount = completedCount
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

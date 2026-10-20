using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MalapadCogonEConcern.Data;
using MalapadCogonEConcern.Models.Entities;
using MalapadCogonEConcern.Services;
using MalapadCogonEConcern.ViewModels;

namespace MalapadCogonEConcern.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _logger = logger;
    }

    // 1. Settings Overview
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        var sitios = await _context.Sitios.OrderBy(s => s.DisplayOrder).ToListAsync();
        var auditLogs = await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(50)
            .ToListAsync();

        var users = await _userManager.Users.ToListAsync();
        var staffItems = new List<StaffUserItemViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var assignedCount = await _context.Concerns.CountAsync(c => c.AssignedToUserId == u.Id);

            staffItems.Add(new StaffUserItemViewModel
            {
                Id = u.Id,
                Email = u.Email ?? "",
                FullName = u.FullName,
                Position = u.Position,
                Role = roles.FirstOrDefault() ?? "Staff",
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                AssignedCount = assignedCount
            });
        }

        var viewModel = new SettingsDashboardViewModel
        {
            Categories = categories,
            Sitios = sitios,
            StaffUsers = staffItems,
            RecentAuditLogs = auditLogs
        };

        return View(viewModel);
    }

    // 2. Add / Edit Category
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid category data submitted.";
            return RedirectToAction(nameof(Index));
        }

        var currentAdmin = await _userManager.GetUserAsync(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (model.Id == 0)
        {
            // Create
            var category = new Category
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim() ?? "",
                IconClass = string.IsNullOrWhiteSpace(model.IconClass) ? "bi-tag" : model.IconClass.Trim(),
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentAdmin?.Id, "CreateCategory", "Category", category.Id.ToString(), $"Created category: {category.Name}", ip);
            TempData["SuccessMessage"] = $"Category '{category.Name}' added successfully.";
        }
        else
        {
            // Edit
            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null) return NotFound();

            category.Name = model.Name.Trim();
            category.Description = model.Description?.Trim() ?? "";
            category.IconClass = string.IsNullOrWhiteSpace(model.IconClass) ? "bi-tag" : model.IconClass.Trim();
            category.DisplayOrder = model.DisplayOrder;
            category.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(currentAdmin?.Id, "UpdateCategory", "Category", category.Id.ToString(), $"Updated category: {category.Name}", ip);
            TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();

        var currentAdmin = await _userManager.GetUserAsync(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(currentAdmin?.Id, "ToggleCategory", "Category", id.ToString(), $"Toggled {category.Name} to Active={category.IsActive}", ip);

        TempData["SuccessMessage"] = $"Category '{category.Name}' status changed to {(category.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    // 3. Add / Edit Sitio
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSitio(SitioFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid sitio data submitted.";
            return RedirectToAction(nameof(Index));
        }

        var currentAdmin = await _userManager.GetUserAsync(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (model.Id == 0)
        {
            var sitio = new Sitio
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim() ?? "",
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Sitios.AddAsync(sitio);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentAdmin?.Id, "CreateSitio", "Sitio", sitio.Id.ToString(), $"Created sitio: {sitio.Name}", ip);
            TempData["SuccessMessage"] = $"Sitio / Purok '{sitio.Name}' added successfully.";
        }
        else
        {
            var sitio = await _context.Sitios.FindAsync(model.Id);
            if (sitio == null) return NotFound();

            sitio.Name = model.Name.Trim();
            sitio.Description = model.Description?.Trim() ?? "";
            sitio.DisplayOrder = model.DisplayOrder;
            sitio.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(currentAdmin?.Id, "UpdateSitio", "Sitio", sitio.Id.ToString(), $"Updated sitio: {sitio.Name}", ip);
            TempData["SuccessMessage"] = $"Sitio / Purok '{sitio.Name}' updated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSitio(int id)
    {
        var sitio = await _context.Sitios.FindAsync(id);
        if (sitio == null) return NotFound();

        sitio.IsActive = !sitio.IsActive;
        await _context.SaveChangesAsync();

        var currentAdmin = await _userManager.GetUserAsync(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(currentAdmin?.Id, "ToggleSitio", "Sitio", id.ToString(), $"Toggled {sitio.Name} to Active={sitio.IsActive}", ip);

        TempData["SuccessMessage"] = $"Sitio '{sitio.Name}' status changed to {(sitio.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    // 4. Staff Account Management
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fill in all required fields for the new account.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            TempData["ErrorMessage"] = $"An account with email '{model.Email}' already exists.";
            return RedirectToAction(nameof(Index));
        }

        var newStaff = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            Position = model.Position.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(newStaff, model.Password);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        var role = model.Role == "Admin" ? "Admin" : "Staff";
        await _userManager.AddToRoleAsync(newStaff, role);

        var currentAdmin = await _userManager.GetUserAsync(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(
            currentAdmin?.Id,
            "CreateStaffUser",
            "ApplicationUser",
            newStaff.Id,
            $"Created staff account for {newStaff.FullName} ({newStaff.Email}) with role {role}.",
            ip);

        TempData["SuccessMessage"] = $"Authorized staff account for '{newStaff.FullName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStaffStatus(string userId)
    {
        var currentAdmin = await _userManager.GetUserAsync(User);
        if (currentAdmin?.Id == userId)
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own active administrator account.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(
            currentAdmin?.Id,
            "ToggleStaffStatus",
            "ApplicationUser",
            user.Id,
            $"Toggled active status of {user.FullName} ({user.Email}) to {user.IsActive}.",
            ip);

        TempData["SuccessMessage"] = $"Account for {user.FullName} is now {(user.IsActive ? "Activated" : "Deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetStaffPassword(string userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["ErrorMessage"] = "Password must be at least 6 characters.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        var currentAdmin = await _userManager.GetUserAsync(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(
            currentAdmin?.Id,
            "ResetStaffPassword",
            "ApplicationUser",
            user.Id,
            $"Reset password for {user.FullName} ({user.Email}).",
            ip);

        TempData["SuccessMessage"] = $"Password for {user.FullName} reset successfully.";
        return RedirectToAction(nameof(Index));
    }
}

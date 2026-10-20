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
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IReferenceNumberService _refService;
    private readonly IPinService _pinService;
    private readonly IFileStorageService _fileService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IReferenceNumberService refService,
        IPinService pinService,
        IFileStorageService fileService,
        IAuditService auditService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _refService = refService;
        _pinService = pinService;
        _fileService = fileService;
        _auditService = auditService;
        _logger = logger;
    }

    // 1. Dashboard
    public async Task<IActionResult> Index()
    {
        var total = await _context.Concerns.CountAsync();
        var underReview = await _context.Concerns.CountAsync(c => c.Status == ConcernStatus.Submitted || c.Status == ConcernStatus.UnderReview);
        var inProgress = await _context.Concerns.CountAsync(c => c.Status == ConcernStatus.Accepted || c.Status == ConcernStatus.Assigned || c.Status == ConcernStatus.InProgress);
        var forwarded = await _context.Concerns.CountAsync(c => c.Status == ConcernStatus.ForwardedToAgency);
        var resolved = await _context.Concerns.CountAsync(c => c.Status == ConcernStatus.Resolved);

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var longPending = await _context.Concerns.CountAsync(c => c.Status != ConcernStatus.Resolved && c.Status != ConcernStatus.Rejected && c.CreatedAt <= sevenDaysAgo);

        var recent = await _context.Concerns
            .Include(c => c.Category)
            .Include(c => c.Sitio)
            .Include(c => c.AssignedToUser)
            .OrderByDescending(c => c.CreatedAt)
            .Take(8)
            .ToListAsync();

        var categoryBreakdown = await _context.Concerns
            .GroupBy(c => c.Category!.Name)
            .Select(g => new CategoryStat { CategoryName = g.Key, Count = g.Count() })
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            TotalConcerns = total,
            UnderReviewConcerns = underReview,
            InProgressConcerns = inProgress,
            ForwardedConcerns = forwarded,
            ResolvedConcerns = resolved,
            LongPendingConcerns = longPending,
            RecentConcerns = recent,
            CategoryBreakdown = categoryBreakdown
        };

        return View(viewModel);
    }

    // 2. All Concerns List with Filters & Search
    [HttpGet]
    public async Task<IActionResult> Concerns(
        string? search, 
        ConcernStatus? status, 
        int? categoryId, 
        int? sitioId, 
        string? assignedToUserId)
    {
        var query = _context.Concerns
            .Include(c => c.Category)
            .Include(c => c.Sitio)
            .Include(c => c.AssignedToUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => 
                c.ReferenceNumber.ToLower().Contains(term) ||
                c.Title.ToLower().Contains(term) ||
                c.LandmarkOrAddress.ToLower().Contains(term) ||
                (c.ComplainantName != null && c.ComplainantName.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (sitioId.HasValue && sitioId.Value > 0)
        {
            query = query.Where(c => c.SitioId == sitioId.Value);
        }

        if (!string.IsNullOrEmpty(assignedToUserId))
        {
            query = query.Where(c => c.AssignedToUserId == assignedToUserId);
        }

        var concerns = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var viewModel = new AdminConcernFilterViewModel
        {
            Search = search,
            Status = status,
            CategoryId = categoryId,
            SitioId = sitioId,
            AssignedToUserId = assignedToUserId,
            Concerns = concerns,
            TotalCount = concerns.Count,
            Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(),
            Sitios = await _context.Sitios.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync(),
            StaffUsers = await _userManager.Users.Where(u => u.IsActive).ToListAsync()
        };

        return View(viewModel);
    }

    // 3. Concern Details & Action Workspace
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var concern = await _context.Concerns
            .Include(c => c.Category)
            .Include(c => c.Sitio)
            .Include(c => c.AssignedToUser)
            .Include(c => c.Attachments)
                .ThenInclude(a => a.UploadedByUser)
            .Include(c => c.StatusHistories)
                .ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (concern == null)
        {
            return NotFound();
        }

        var staffUsers = await _userManager.Users.Where(u => u.IsActive).ToListAsync();

        var viewModel = new AdminConcernDetailViewModel
        {
            Concern = concern,
            AvailableStaff = staffUsers,
            StatusUpdate = new AdminStatusUpdateViewModel
            {
                ConcernId = concern.Id,
                NewStatus = concern.Status
            },
            ForwardUpdate = new AdminForwardViewModel
            {
                ConcernId = concern.Id
            }
        };

        return View(viewModel);
    }

    // 4. Update Status Action
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(AdminStatusUpdateViewModel model)
    {
        var concern = await _context.Concerns
            .Include(c => c.StatusHistories)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == model.ConcernId);

        if (concern == null)
        {
            return NotFound();
        }

        var previousStatus = concern.Status;
        concern.Status = model.NewStatus;
        concern.UpdatedAt = DateTime.UtcNow;

        if (model.NewStatus == ConcernStatus.Resolved)
        {
            concern.ResolvedAt = DateTime.UtcNow;
        }

        var currentUser = await _userManager.GetUserAsync(User);

        // Upload Resolution Proof Photo (if provided)
        if (model.ResolutionPhoto != null && model.ResolutionPhoto.Length > 0)
        {
            var uploadResult = await _fileService.SavePhotoAsync(model.ResolutionPhoto, "resolutions");
            if (uploadResult.Success)
            {
                var attachment = new ConcernAttachment
                {
                    ConcernId = concern.Id,
                    StoredFileName = uploadResult.StoredFileName,
                    OriginalFileName = uploadResult.OriginalFileName,
                    ContentType = uploadResult.ContentType,
                    FileSizeBytes = uploadResult.FileSizeBytes,
                    AttachmentType = AttachmentType.ResolutionProof,
                    UploadedByUserId = currentUser?.Id,
                    UploadedAt = DateTime.UtcNow
                };
                await _context.ConcernAttachments.AddAsync(attachment);
            }
        }

        // Add to Status History
        var history = new ConcernStatusHistory
        {
            ConcernId = concern.Id,
            PreviousStatus = previousStatus,
            NewStatus = model.NewStatus,
            ChangedByUserId = currentUser?.Id,
            PublicRemarks = model.PublicRemarks.Trim(),
            InternalRemarks = model.InternalRemarks?.Trim(),
            ChangedAt = DateTime.UtcNow
        };
        await _context.ConcernStatusHistories.AddAsync(history);

        await _context.SaveChangesAsync();

        // Audit Log
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(
            currentUser?.Id,
            "UpdateStatus",
            "Concern",
            concern.ReferenceNumber,
            $"Status transitioned from {previousStatus} to {model.NewStatus}. Remarks: {model.PublicRemarks}",
            ip);

        TempData["SuccessMessage"] = $"Status for {concern.ReferenceNumber} successfully updated to {model.NewStatus}.";
        return RedirectToAction(nameof(Details), new { id = model.ConcernId });
    }

    // 5. Assign Staff
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int concernId, string? staffUserId, string? assignNotes)
    {
        var concern = await _context.Concerns.FindAsync(concernId);
        if (concern == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var staffUser = !string.IsNullOrEmpty(staffUserId) 
            ? await _userManager.FindByIdAsync(staffUserId) 
            : null;

        var prevStatus = concern.Status;
        concern.AssignedToUserId = staffUserId;
        concern.Status = ConcernStatus.Assigned;
        concern.UpdatedAt = DateTime.UtcNow;

        var history = new ConcernStatusHistory
        {
            ConcernId = concern.Id,
            PreviousStatus = prevStatus,
            NewStatus = ConcernStatus.Assigned,
            ChangedByUserId = currentUser?.Id,
            PublicRemarks = staffUser != null 
                ? $"Concern accepted and assigned to Barangay Staff ({staffUser.FullName} - {staffUser.Position})." 
                : "Assignment modified.",
            InternalRemarks = assignNotes,
            ChangedAt = DateTime.UtcNow
        };
        await _context.ConcernStatusHistories.AddAsync(history);
        await _context.SaveChangesAsync();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(
            currentUser?.Id,
            "AssignStaff",
            "Concern",
            concern.ReferenceNumber,
            $"Assigned to: {staffUser?.FullName ?? "None"}. Notes: {assignNotes}",
            ip);

        TempData["SuccessMessage"] = $"Concern {concern.ReferenceNumber} assigned to {staffUser?.FullName}.";
        return RedirectToAction(nameof(Details), new { id = concernId });
    }

    // 6. Forward Concern to Municipal Agency (Sigma LGU / CAPELCO / DPWH)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Forward(AdminForwardViewModel model)
    {
        var concern = await _context.Concerns.FindAsync(model.ConcernId);
        if (concern == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var prevStatus = concern.Status;

        concern.Status = ConcernStatus.ForwardedToAgency;
        concern.ForwardedToAgency = model.AgencyName.Trim();
        concern.ForwardedReferenceNo = model.EndorsementRefNo?.Trim();
        concern.ForwardedRemarks = model.EndorsementNotes.Trim();
        concern.UpdatedAt = DateTime.UtcNow;

        var history = new ConcernStatusHistory
        {
            ConcernId = concern.Id,
            PreviousStatus = prevStatus,
            NewStatus = ConcernStatus.ForwardedToAgency,
            ChangedByUserId = currentUser?.Id,
            PublicRemarks = $"Forwarded to external agency: {model.AgencyName}. Endorsement Details: {model.EndorsementNotes}",
            InternalRemarks = $"Endorsement Ref: {model.EndorsementRefNo}",
            ChangedAt = DateTime.UtcNow
        };
        await _context.ConcernStatusHistories.AddAsync(history);
        await _context.SaveChangesAsync();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(
            currentUser?.Id,
            "ForwardConcern",
            "Concern",
            concern.ReferenceNumber,
            $"Forwarded to: {model.AgencyName}. Endorsement: {model.EndorsementNotes}",
            ip);

        TempData["SuccessMessage"] = $"Concern {concern.ReferenceNumber} officially forwarded to {model.AgencyName}.";
        return RedirectToAction(nameof(Details), new { id = model.ConcernId });
    }

    // 7. Assisted Walk-In Intake Form (GET)
    [HttpGet]
    public async Task<IActionResult> CreateAssisted()
    {
        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.Sitios = await _context.Sitios.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();
        return View(new AdminCreateAssistedViewModel());
    }

    // 8. Assisted Walk-In Intake Form (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssisted(AdminCreateAssistedViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            ViewBag.Sitios = await _context.Sitios.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();
            return View(model);
        }

        var currentUser = await _userManager.GetUserAsync(User);

        // Upload photo if provided
        FileUploadResult? uploadResult = null;
        if (model.PhotoAttachment != null && model.PhotoAttachment.Length > 0)
        {
            uploadResult = await _fileService.SavePhotoAsync(model.PhotoAttachment, "concerns");
        }

        var referenceNumber = await _refService.GenerateReferenceNumberAsync();
        var (hash, salt) = _pinService.HashPin(model.VerificationPin);

        var concern = new Concern
        {
            ReferenceNumber = referenceNumber,
            VerificationPinHash = hash,
            TrackingSalt = salt,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            CategoryId = model.CategoryId,
            SitioId = model.SitioId,
            LandmarkOrAddress = model.LandmarkOrAddress.Trim(),
            ComplainantName = model.ComplainantName.Trim(),
            ComplainantContact = model.ComplainantContact?.Trim(),
            Channel = model.Channel,
            Status = ConcernStatus.Accepted, // Walk-in validated directly by desk staff
            Priority = model.Priority,
            CreatedAt = DateTime.UtcNow
        };

        var history = new ConcernStatusHistory
        {
            Concern = concern,
            PreviousStatus = ConcernStatus.Submitted,
            NewStatus = ConcernStatus.Accepted,
            ChangedByUserId = currentUser?.Id,
            PublicRemarks = $"Intake conducted in-person by Barangay Staff ({currentUser?.FullName}). Validated and queued.",
            InternalRemarks = $"Channel: {model.Channel}. Complainant: {model.ComplainantName} ({model.ComplainantContact})",
            ChangedAt = DateTime.UtcNow
        };
        concern.StatusHistories.Add(history);

        if (uploadResult != null && uploadResult.Success)
        {
            var attachment = new ConcernAttachment
            {
                Concern = concern,
                StoredFileName = uploadResult.StoredFileName,
                OriginalFileName = uploadResult.OriginalFileName,
                ContentType = uploadResult.ContentType,
                FileSizeBytes = uploadResult.FileSizeBytes,
                AttachmentType = AttachmentType.SubmissionPhoto,
                UploadedByUserId = currentUser?.Id,
                UploadedAt = DateTime.UtcNow
            };
            concern.Attachments.Add(attachment);
        }

        await _context.Concerns.AddAsync(concern);
        await _context.SaveChangesAsync();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        await _auditService.LogAsync(
            currentUser?.Id,
            "CreateAssistedConcern",
            "Concern",
            concern.ReferenceNumber,
            $"Assisted intake for {model.ComplainantName} via {model.Channel}",
            ip);

        TempData["SuccessMessage"] = $"Assisted concern {referenceNumber} successfully recorded for {model.ComplainantName}. Resident tracking PIN is {model.VerificationPin}.";
        return RedirectToAction(nameof(Details), new { id = concern.Id });
    }
}

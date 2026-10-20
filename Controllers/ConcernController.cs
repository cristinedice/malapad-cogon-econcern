using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MalapadCogonEConcern.Data;
using MalapadCogonEConcern.Models.Entities;
using MalapadCogonEConcern.Models.Enums;
using MalapadCogonEConcern.Services;
using MalapadCogonEConcern.ViewModels;

namespace MalapadCogonEConcern.Controllers;

public class ConcernController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IReferenceNumberService _refService;
    private readonly IPinService _pinService;
    private readonly IFileStorageService _fileService;
    private readonly ILogger<ConcernController> _logger;

    public ConcernController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IReferenceNumberService refService,
        IPinService pinService,
        IFileStorageService fileService,
        ILogger<ConcernController> logger)
    {
        _context = context;
        _userManager = userManager;
        _refService = refService;
        _pinService = pinService;
        _fileService = fileService;
        _logger = logger;
    }

    // --- STEP 1: SELECT A CONCERN CATEGORY (Matching Image 4) ---
    [HttpGet]
    public async Task<IActionResult> SelectCategory(string? search = null)
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        ViewBag.SearchTerm = search;
        return View(categories);
    }

    // --- STEP 2: SUBMIT CONCERN FORM ---
    [HttpGet]
    public async Task<IActionResult> Submit(int? categoryId = null, string? title = null)
    {
        await LoadDropdownsAsync();

        var model = new ConcernSubmitViewModel();
        if (categoryId.HasValue)
        {
            model.CategoryId = categoryId.Value;
        }

        if (!string.IsNullOrEmpty(title))
        {
            model.Title = title;
        }

        // If user is logged in, auto-fill their info
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                model.ComplainantName = user.FullName;
                model.ComplainantContact = user.PhoneNumber;
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ConcernSubmitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        FileUploadResult? uploadResult = null;
        if (model.PhotoAttachment != null && model.PhotoAttachment.Length > 0)
        {
            uploadResult = await _fileService.SavePhotoAsync(model.PhotoAttachment, "concerns");
            if (!uploadResult.Success)
            {
                ModelState.AddModelError("PhotoAttachment", uploadResult.ErrorMessage ?? "Failed to upload photo.");
                await LoadDropdownsAsync();
                return View(model);
            }
        }

        var referenceNumber = await _refService.GenerateReferenceNumberAsync();
        var (hash, salt) = _pinService.HashPin(model.VerificationPin);

        var currentUser = User.Identity?.IsAuthenticated == true ? await _userManager.GetUserAsync(User) : null;

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
            ComplainantName = string.IsNullOrWhiteSpace(model.ComplainantName) ? currentUser?.FullName : model.ComplainantName.Trim(),
            ComplainantContact = string.IsNullOrWhiteSpace(model.ComplainantContact) ? currentUser?.PhoneNumber : model.ComplainantContact.Trim(),
            CreatedByUserId = currentUser?.Id,
            Channel = SubmissionChannel.Online,
            Status = ConcernStatus.Submitted,
            Priority = PriorityLevel.Medium,
            CreatedAt = DateTime.UtcNow
        };

        var initialHistory = new ConcernStatusHistory
        {
            Concern = concern,
            PreviousStatus = ConcernStatus.Submitted,
            NewStatus = ConcernStatus.Submitted,
            PublicRemarks = "Concern successfully submitted online by resident and queued for barangay review.",
            InternalRemarks = $"Submitted via online resident portal by {(currentUser != null ? currentUser.Email : "Resident")}.",
            ChangedAt = DateTime.UtcNow
        };
        concern.StatusHistories.Add(initialHistory);

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
                UploadedAt = DateTime.UtcNow
            };
            concern.Attachments.Add(attachment);
        }

        await _context.Concerns.AddAsync(concern);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New concern created: {RefNo}", referenceNumber);

        TempData["RefNo"] = referenceNumber;
        TempData["Pin"] = model.VerificationPin;

        return RedirectToAction(nameof(Confirmation), new { refNo = referenceNumber });
    }

    // --- STEP 3: SUBMISSION CONFIRMATION ---
    [HttpGet]
    public async Task<IActionResult> Confirmation(string refNo)
    {
        if (string.IsNullOrEmpty(refNo))
        {
            return RedirectToAction(nameof(Submit));
        }

        var concern = await _context.Concerns
            .Include(c => c.Category)
            .Include(c => c.Sitio)
            .FirstOrDefaultAsync(c => c.ReferenceNumber == refNo);

        if (concern == null)
        {
            return NotFound();
        }

        var pin = TempData["Pin"] as string ?? "••••";

        var viewModel = new ConcernConfirmationViewModel
        {
            ReferenceNumber = concern.ReferenceNumber,
            VerificationPin = pin,
            Title = concern.Title,
            CategoryName = concern.Category?.Name ?? "General",
            SitioName = concern.Sitio?.Name ?? "General",
            CreatedAt = concern.CreatedAt
        };

        return View(viewModel);
    }

    // --- STEP 4: MY CONCERNS FEED (Matching Image 5) ---
    [HttpGet]
    public async Task<IActionResult> MyConcerns(string? status = null, string? search = null)
    {
        var currentUser = User.Identity?.IsAuthenticated == true ? await _userManager.GetUserAsync(User) : null;

        var query = _context.Concerns
            .Include(c => c.Category)
            .Include(c => c.Sitio)
            .Include(c => c.StatusHistories)
            .AsQueryable();

        // Scope to current resident if logged in and not admin/staff
        if (currentUser != null && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            query = query.Where(c => c.CreatedByUserId == currentUser.Id || 
                                     c.ComplainantContact == currentUser.PhoneNumber ||
                                     c.ComplainantName == currentUser.FullName);
        }

        // Status Filter Pill
        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            if (status.Equals("New", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.Status == ConcernStatus.Submitted);
            else if (status.Equals("Ongoing", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.Status == ConcernStatus.InProgress || c.Status == ConcernStatus.Assigned);
            else if (status.Equals("Acknowledge", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.Status == ConcernStatus.UnderReview || c.Status == ConcernStatus.Accepted);
            else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.Status == ConcernStatus.Resolved);
            else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.Status == ConcernStatus.Rejected);
        }

        // Search Filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.ReferenceNumber.ToLower().Contains(term) ||
                                     c.Title.ToLower().Contains(term) ||
                                     (c.Category != null && c.Category.Name.ToLower().Contains(term)));
        }

        var concerns = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        ViewBag.CurrentFilter = string.IsNullOrEmpty(status) ? "All" : status;
        ViewBag.SearchTerm = search;

        return View(concerns);
    }

    // --- STEP 5: TRACK CONCERN BY REFERENCE NUMBER & PIN ---
    [HttpGet]
    public IActionResult Track(string? refNo = null)
    {
        return View(new ConcernTrackViewModel { ReferenceNumber = refNo ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Track(ConcernTrackViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cleanRefNo = model.ReferenceNumber.Trim();
        var cleanPin = model.VerificationPin.Trim();

        var concern = await _context.Concerns
            .Include(c => c.Category)
            .Include(c => c.Sitio)
            .Include(c => c.Attachments)
            .Include(c => c.StatusHistories)
                .ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(c => c.ReferenceNumber.ToLower() == cleanRefNo.ToLower());

        if (concern == null || !_pinService.VerifyPin(cleanPin, concern.VerificationPinHash, concern.TrackingSalt))
        {
            model.ErrorMessage = "No matching concern found for this Reference Number and PIN. Please check your credentials.";
            return View(model);
        }

        var submissionPhoto = concern.Attachments.FirstOrDefault(a => a.AttachmentType == AttachmentType.SubmissionPhoto);
        var resolutionPhoto = concern.Attachments.FirstOrDefault(a => a.AttachmentType == AttachmentType.ResolutionProof);

        var submissionPhotoUrl = submissionPhoto != null 
            ? $"/uploads/concerns/{concern.CreatedAt.Year}/{concern.CreatedAt.Month:D2}/{submissionPhoto.StoredFileName}" 
            : null;

        var resolutionPhotoUrl = resolutionPhoto != null 
            ? $"/uploads/resolutions/{resolutionPhoto.UploadedAt.Year}/{resolutionPhoto.UploadedAt.Month:D2}/{resolutionPhoto.StoredFileName}" 
            : null;

        var timeline = concern.StatusHistories
            .OrderBy(h => h.ChangedAt)
            .Select(h => new PublicStatusHistoryItem
            {
                Timestamp = h.ChangedAt,
                Status = h.NewStatus,
                PublicRemarks = h.PublicRemarks,
                ChangedByPosition = h.ChangedByUser?.Position ?? "Barangay Official"
            })
            .ToList();

        var resultViewModel = new ConcernTrackResultViewModel
        {
            ReferenceNumber = concern.ReferenceNumber,
            Title = concern.Title,
            CategoryName = concern.Category?.Name ?? "General",
            CategoryIcon = concern.Category?.IconClass ?? "bi-tag",
            SitioName = concern.Sitio?.Name ?? "General",
            LandmarkOrAddress = concern.LandmarkOrAddress,
            Description = concern.Description,
            CurrentStatus = concern.Status,
            Priority = concern.Priority,
            CreatedAt = concern.CreatedAt,
            ResolvedAt = concern.ResolvedAt,
            ForwardedToAgency = concern.ForwardedToAgency,
            ForwardedRemarks = concern.ForwardedRemarks,
            SubmissionPhotoUrl = submissionPhotoUrl,
            ResolutionPhotoUrl = resolutionPhotoUrl,
            Timeline = timeline
        };

        return View("TrackResult", resultViewModel);
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.Categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        ViewBag.Sitios = await _context.Sitios
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }
}

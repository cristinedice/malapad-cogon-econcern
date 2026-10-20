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
    private readonly IReferenceNumberService _refService;
    private readonly IPinService _pinService;
    private readonly IFileStorageService _fileService;
    private readonly ILogger<ConcernController> _logger;

    public ConcernController(
        ApplicationDbContext context,
        IReferenceNumberService refService,
        IPinService pinService,
        IFileStorageService fileService,
        ILogger<ConcernController> logger)
    {
        _context = context;
        _refService = refService;
        _pinService = pinService;
        _fileService = fileService;
        _logger = logger;
    }

    // 1. Submit Concern (GET)
    [HttpGet]
    public async Task<IActionResult> Submit()
    {
        await LoadDropdownsAsync();
        return View(new ConcernSubmitViewModel());
    }

    // 2. Submit Concern (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ConcernSubmitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        // Handle Photo Upload (if provided)
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

        // Generate Reference Number & Hash PIN
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
            ComplainantName = string.IsNullOrWhiteSpace(model.ComplainantName) ? null : model.ComplainantName.Trim(),
            ComplainantContact = string.IsNullOrWhiteSpace(model.ComplainantContact) ? null : model.ComplainantContact.Trim(),
            Channel = SubmissionChannel.Online,
            Status = ConcernStatus.Submitted,
            Priority = PriorityLevel.Medium,
            CreatedAt = DateTime.UtcNow
        };

        // Add Initial Status History
        var initialHistory = new ConcernStatusHistory
        {
            Concern = concern,
            PreviousStatus = ConcernStatus.Submitted,
            NewStatus = ConcernStatus.Submitted,
            PublicRemarks = "Concern successfully submitted online by resident and queued for review.",
            InternalRemarks = "Submitted via online resident portal.",
            ChangedAt = DateTime.UtcNow
        };
        concern.StatusHistories.Add(initialHistory);

        // Add Attachment Record
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

        _logger.LogInformation("New concern submitted: {RefNo}", referenceNumber);

        // Store confirmation data in TempData
        TempData["RefNo"] = referenceNumber;
        TempData["Pin"] = model.VerificationPin;

        return RedirectToAction(nameof(Confirmation), new { refNo = referenceNumber });
    }

    // 3. Confirmation Receipt (GET)
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

    // 4. Track Concern (GET)
    [HttpGet]
    public IActionResult Track(string? refNo = null)
    {
        return View(new ConcernTrackViewModel { ReferenceNumber = refNo ?? string.Empty });
    }

    // 5. Track Concern (POST - Secure PIN Verified)
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

        // Validate Reference No. and PIN
        if (concern == null || !_pinService.VerifyPin(cleanPin, concern.VerificationPinHash, concern.TrackingSalt))
        {
            model.ErrorMessage = "No matching concern found for this Reference Number and PIN. Please check your credentials.";
            return View(model);
        }

        // Build safe public tracking result (No private complainant data is exposed)
        var submissionPhoto = concern.Attachments
            .FirstOrDefault(a => a.AttachmentType == AttachmentType.SubmissionPhoto);

        var resolutionPhoto = concern.Attachments
            .FirstOrDefault(a => a.AttachmentType == AttachmentType.ResolutionProof);

        var submissionPhotoUrl = submissionPhoto != null 
            ? $"/uploads/concerns/{concern.CreatedAt.Year}/{concern.CreatedAt.Month:D2}/{submissionPhoto.StoredFileName}" 
            : null;

        var resolutionPhotoUrl = resolutionPhoto != null 
            ? $"/uploads/concerns/{concern.CreatedAt.Year}/{concern.CreatedAt.Month:D2}/{resolutionPhoto.StoredFileName}" 
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

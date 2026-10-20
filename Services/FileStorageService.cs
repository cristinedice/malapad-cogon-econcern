namespace MalapadCogonEConcern.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _logger;

    // Allowed extensions and MIME types
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<FileUploadResult> SavePhotoAsync(IFormFile file, string subFolder = "concerns")
    {
        if (file == null || file.Length == 0)
        {
            return new FileUploadResult { Success = false, ErrorMessage = "No file was uploaded." };
        }

        // 1. Check file size
        if (file.Length > MaxFileSize)
        {
            return new FileUploadResult { Success = false, ErrorMessage = "File exceeds the 5 MB maximum size limit." };
        }

        // 2. Check extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return new FileUploadResult { Success = false, ErrorMessage = "Invalid file type. Only JPG, PNG, and WEBP images are allowed." };
        }

        // 3. Check declared MIME type
        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            return new FileUploadResult { Success = false, ErrorMessage = "Invalid image content type." };
        }

        // 4. Verify Magic Bytes
        using (var stream = file.OpenReadStream())
        {
            var header = new byte[12];
            int read = await stream.ReadAsync(header, 0, header.Length);
            if (read < 4)
            {
                return new FileUploadResult { Success = false, ErrorMessage = "Corrupt or unreadable image file." };
            }

            if (!IsValidImageHeader(header, extension))
            {
                _logger.LogWarning("Potential spoofed file upload rejected: {FileName}", file.FileName);
                return new FileUploadResult { Success = false, ErrorMessage = "The file contents do not match a valid image header." };
            }
        }

        // 5. Generate secure randomized filename
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var now = DateTime.UtcNow;
        var relativeDir = Path.Combine("uploads", subFolder, now.Year.ToString(), now.Month.ToString("D2"));
        var physicalDir = Path.Combine(_env.WebRootPath, relativeDir);

        Directory.CreateDirectory(physicalDir);

        var physicalPath = Path.Combine(physicalDir, safeFileName);
        using (var targetStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(targetStream);
        }

        // Use forward slashes for web URLs
        var webRelativePath = "/" + Path.Combine(relativeDir, safeFileName).Replace("\\", "/");

        return new FileUploadResult
        {
            Success = true,
            StoredFileName = safeFileName,
            RelativePath = webRelativePath,
            OriginalFileName = Path.GetFileName(file.FileName),
            FileSizeBytes = file.Length,
            ContentType = file.ContentType
        };
    }

    private static bool IsValidImageHeader(byte[] header, string extension)
    {
        // JPEG: FF D8 FF
        if (extension is ".jpg" or ".jpeg")
        {
            return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        }

        // PNG: 89 50 4E 47
        if (extension == ".png")
        {
            return header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        }

        // WEBP: 'RIFF' ... 'WEBP'
        if (extension == ".webp")
        {
            return header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                   header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
        }

        return false;
    }
}

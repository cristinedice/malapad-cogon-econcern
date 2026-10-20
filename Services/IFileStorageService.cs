namespace MalapadCogonEConcern.Services;

public class FileUploadResult
{
    public bool Success { get; set; }
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public interface IFileStorageService
{
    Task<FileUploadResult> SavePhotoAsync(IFormFile file, string subFolder = "concerns");
}

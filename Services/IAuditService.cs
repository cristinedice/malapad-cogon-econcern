namespace MalapadCogonEConcern.Services;

public interface IAuditService
{
    Task LogAsync(string? userId, string action, string entityName, string entityId, string details, string ipAddress);
}

namespace MalapadCogonEConcern.Services;

public interface IReferenceNumberService
{
    Task<string> GenerateReferenceNumberAsync();
}

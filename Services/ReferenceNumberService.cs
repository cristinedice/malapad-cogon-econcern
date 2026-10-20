using Microsoft.EntityFrameworkCore;
using MalapadCogonEConcern.Data;

namespace MalapadCogonEConcern.Services;

public class ReferenceNumberService : IReferenceNumberService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public ReferenceNumberService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<string> GenerateReferenceNumberAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var prefix = _config["SystemSettings:ReferencePrefix"] ?? "MC";
            var currentYear = DateTime.UtcNow.Year;
            var pattern = $"{prefix}-{currentYear}-";

            // Find concerns created this year matching the pattern
            var latestConcern = await _context.Concerns
                .Where(c => c.ReferenceNumber.StartsWith(pattern))
                .OrderByDescending(c => c.ReferenceNumber)
                .Select(c => c.ReferenceNumber)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (!string.IsNullOrEmpty(latestConcern))
            {
                var parts = latestConcern.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            return $"{prefix}-{currentYear}-{nextSequence:D4}";
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

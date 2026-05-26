using PoOcr.Domain.Extraction;

namespace PoOcr.Application.Abstractions;

public interface IExtractionJobRepository
{
    Task AddAsync(ExtractionJob job, CancellationToken cancellationToken);
    Task<ExtractionJob?> GetNextQueuedAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

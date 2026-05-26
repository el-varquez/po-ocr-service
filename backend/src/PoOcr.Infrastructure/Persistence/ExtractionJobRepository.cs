using Microsoft.EntityFrameworkCore;
using PoOcr.Application.Abstractions;
using PoOcr.Domain.Extraction;

namespace PoOcr.Infrastructure.Persistence;

public sealed class ExtractionJobRepository(OcrDbContext dbContext) : IExtractionJobRepository
{
    public async Task AddAsync(
        ExtractionJob job,
        CancellationToken cancellationToken)
    {
        await dbContext.ExtractionJobs.AddAsync(job, cancellationToken);
    }

    public async Task<ExtractionJob?> GetNextQueuedAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.ExtractionJobs.Where(x => x.StartedAt == null).OrderBy(x => x.QueuedAt).FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
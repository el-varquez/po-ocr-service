using Microsoft.EntityFrameworkCore;
using PoOcr.Application.Abstractions;
using PoOcr.Domain.Uploads;

namespace PoOcr.Infrastructure.Persistence;

public sealed class UploadRepository(OcrDbContext dbContext) : IUploadRepository
{
    public async Task<IReadOnlyList<UploadFile>> GetByIdAsync(
        IReadOnlyCollection<Guid> uploadIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.UploadFiles.Where(x => uploadIds.Contains(x.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UploadFile>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        return await dbContext.UploadFiles.OrderByDescending(x => x.UploadedAt).Take(take).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        UploadFile upload,
        CancellationToken cancellationToken)
    {
        await dbContext.UploadFiles.AddAsync(upload, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
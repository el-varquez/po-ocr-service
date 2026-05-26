using Microsoft.EntityFrameworkCore;
using PoOcr.Application.Abstractions;
using PoOcr.Domain.Drafts;

namespace PoOcr.Infrastructure.Persistence;

public sealed class DraftRepository(OcrDbContext dbContext) : IDraftRepository
{
    public async Task AddAsync(
        PoDraft draft,
        CancellationToken cancellationToken)
    {
        await dbContext.PoDrafts.AddAsync(draft, cancellationToken);
    }

    public async Task<PoDraft?> GetByIdAsync(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PoDrafts.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == draftId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
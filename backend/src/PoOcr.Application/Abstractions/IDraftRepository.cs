using PoOcr.Domain.Drafts;

namespace PoOcr.Application.Abstractions;

public interface IDraftRepository
{
    Task AddAsync(PoDraft draft, CancellationToken cancellationToken);
    Task<IReadOnlyList<PoDraft>> GetRecentAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<PoDraft>> GetHistoryAsync(int take, CancellationToken cancellationToken);
    Task<PoDraft?> GetByIdAsync(Guid draftId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

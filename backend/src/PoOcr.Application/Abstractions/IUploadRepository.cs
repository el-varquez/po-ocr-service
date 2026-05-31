using PoOcr.Domain.Uploads;

namespace PoOcr.Application.Abstractions;

public interface IUploadRepository
{
    Task<IReadOnlyList<UploadFile>> GetByIdAsync(
        IReadOnlyCollection<Guid> uploadIds,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<UploadFile>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<UploadFile>> GetHistoryAsync(
        int take,
        CancellationToken cancellationToken
    );

    Task AddAsync(UploadFile upload, CancellationToken cancellationToken);
    
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

using PoOcr.Application.Abstractions;
using PoOcr.Domain.Audit;

namespace  PoOcr.Infrastructure.Persistence;

public sealed class AuditWriter(OcrDbContext dbContext) : IAuditWriter
{
    public async Task WriteAsync(
        string action,
        string actor,
        string message,
        CancellationToken cancellationToken)
    {
        var auditEvent = AuditEvent.Create(action, actor, message);
        await dbContext.AuditEvents.AddAsync(
            auditEvent,
            cancellationToken);
    }
}
namespace PoOcr.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(
        string action,
        string actor,
        string messages,
        CancellationToken cancellationToken
    );
}

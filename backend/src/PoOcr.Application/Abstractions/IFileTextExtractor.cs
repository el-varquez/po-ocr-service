namespace PoOcr.Application.Abstractions;

public interface IFileTextExtractor
{
    Task<string> ExtractTextAsync(
        string storagePath,
        string contentType,
        CancellationToken cancellationToken
    );
}

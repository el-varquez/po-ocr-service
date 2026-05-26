namespace  PoOcr.Application.Abstractions;

public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(
        FileStorageRequest request,
        CancellationToken cancellationToken
    );
}

public sealed record FileStorageRequest(
    string OriginalFileName,
    string ContentType,
    Stream Content,
    string UploadedBy
);

public sealed record StoredFileResult(
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string StoredPath,
    string CheckSum,
    string UploadedBy
);
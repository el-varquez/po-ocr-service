using System.Security.Cryptography;
using PoOcr.Application.Abstractions;

namespace PoOcr.Infrastructure.Storage;

public sealed class LocalFileStorage(LocalFileStorageOptions options) : IFileStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    public async Task<StoredFileResult> SaveAsync(
        FileStorageRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        Directory.CreateDirectory(options.RootPath);

        var uploadId = Guid.NewGuid();
        var extension = Path.GetExtension(request.OriginalFileName);
        var datedFolder = Path.Combine(options.RootPath, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));

        Directory.CreateDirectory(datedFolder);

        var storedPath = Path.Combine(datedFolder, $"{uploadId:N}{extension}");

        await using var output = File.Create(storedPath);

        using var sha256 = SHA256.Create();
        await using var cryptoStream = new CryptoStream(output, sha256, CryptoStreamMode.Write);

        await request.Content.CopyToAsync(cryptoStream, cancellationToken);
        await cryptoStream.FlushAsync(cancellationToken);
        cryptoStream.FlushFinalBlock();

        var checksum = Convert.ToHexString(sha256.Hash ?? []).ToLowerInvariant();

        var sizeBytes = new FileInfo(storedPath).Length;

        return new StoredFileResult(
            request.OriginalFileName.Trim(),
            request.ContentType.Trim(),
            sizeBytes,
            storedPath,
            checksum,
            request.UploadedBy.Trim()
        );
    }

    private void Validate(FileStorageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
            throw new InvalidOperationException("Original file name is required.");

        if (string.IsNullOrWhiteSpace(request.UploadedBy))
            throw new InvalidOperationException("Uploaded by is required.");

        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new InvalidOperationException($"Unsupported file type: {request.ContentType}");

        if (request.Content.Length <= 0)
            throw new InvalidOperationException("File is empty.");

        if (request.Content.Length > options.MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the maximum allowed size.");
    }
}      
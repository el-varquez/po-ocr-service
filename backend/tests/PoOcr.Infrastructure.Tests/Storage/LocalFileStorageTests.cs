using System.Security.Cryptography;
using System.Text;
using PoOcr.Application.Abstractions;
using PoOcr.Infrastructure.Storage;

namespace PoOcr.Infrastructure.Tests.Storage;

public sealed class LocalFileStorageTests
{
    [Fact]
    public async Task SaveAsync_WhenFileIsValid_WritesFileAndReturnsMetadata()
    {
        var rootPath = CreateTempDirectory();
        var storage = new LocalFileStorage(new LocalFileStorageOptions(rootPath));

        await using var content = new MemoryStream(
            Encoding.UTF8.GetBytes("sample purchase order"));

        var result = await storage.SaveAsync(
            new FileStorageRequest(
                "sample-po.pdf",
                "application/pdf",
                content,
                "admin"),
            CancellationToken.None);

        Assert.Equal("sample-po.pdf", result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("admin", result.UploadedBy);
        Assert.True(File.Exists(result.StoredPath));
        Assert.Equal("sample purchase order", await
        File.ReadAllTextAsync(result.StoredPath));

        var expectedChecksum = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes("sample purchase order")))
            .ToLowerInvariant();

        Assert.Equal(expectedChecksum, result.CheckSum);
    }

    [Fact]
    public async Task SaveAsync_WhenContentTypeIsUnsupported_Throws()
    {
        var rootPath = CreateTempDirectory();
        var storage = new LocalFileStorage(new LocalFileStorageOptions(rootPath));

        await using var content = new MemoryStream(
            Encoding.UTF8.GetBytes("bad file"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.SaveAsync(
                new FileStorageRequest(
                    "notes.txt",
                    "text/plain",
                    content,
                    "admin"),
                CancellationToken.None));

        Assert.Contains("Unsupported file type", ex.Message);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "po-ocr-service-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);

        return path;
    }
}
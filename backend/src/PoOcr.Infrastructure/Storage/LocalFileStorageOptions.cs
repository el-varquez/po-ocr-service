namespace  PoOcr.Infrastructure.Storage;

public sealed record LocalFileStorageOptions(
    string RootPath,
    long MaxFileSizeBytes = 10 * 1024 * 1024);

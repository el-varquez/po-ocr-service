namespace PoOcr.Api.Contracts;

internal sealed record UploadResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Status,
    string UploadedBy,
    DateTimeOffset UploadedAt,
    string? FailureReason);

namespace PoOcr.Api.Contracts;

internal sealed record UploadHistoryResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Status,
    string UploadedBy,
    DateTimeOffset UploadedAt,
    string? FailureReason,
    bool IsDeleted,
    DateTimeOffset? DeletedAt,
    string? DeletedBy);

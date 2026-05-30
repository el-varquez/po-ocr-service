namespace PoOcr.Api.Contracts;

internal sealed record DraftListResponse(
    Guid Id,
    Guid UploadFileId,
    string PoNumber,
    DateOnly? PoDate,
    string CustomerName,
    int LineCount,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Warnings);

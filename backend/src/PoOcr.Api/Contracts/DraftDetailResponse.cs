namespace PoOcr.Api.Contracts;

internal sealed record DraftDetailResponse(
    Guid Id,
    Guid UploadFileId,
    string PoNumber,
    DateOnly? PoDate,
    string CustomerName,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Warnings,
    IReadOnlyCollection<DraftLineResponse> Lines);

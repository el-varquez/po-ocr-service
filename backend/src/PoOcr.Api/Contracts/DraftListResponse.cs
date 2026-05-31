namespace PoOcr.Api.Contracts;

internal sealed record DraftListResponse(
    Guid Id,
    Guid UploadFileId,
    string VendorName,
    DateOnly? PoDate,
    string ReferenceNumber,
    DateOnly? DateExpected,
    string PaymentTerms,
    decimal? TotalAmount,
    int LineCount,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Warnings);

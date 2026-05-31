namespace PoOcr.Api.Contracts;

internal sealed record DraftDetailResponse(
    Guid Id,
    Guid UploadFileId,
    string VendorName,
    DateOnly? PoDate,
    string ReferenceNumber,
    DateOnly? DateExpected,
    string ShipTo,
    string ShipVia,
    string PaymentTerms,
    decimal? TotalAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Warnings,
    IReadOnlyCollection<DraftLineResponse> Lines);

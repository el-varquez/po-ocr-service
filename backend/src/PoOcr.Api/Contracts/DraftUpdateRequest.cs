namespace PoOcr.Api.Contracts;

internal sealed record DraftUpdateRequest(
    string? VendorName,
    DateOnly? PoDate,
    string? ReferenceNumber,
    DateOnly? DateExpected,
    string? ShipTo,
    string? ShipVia,
    string? PaymentTerms,
    decimal? TotalAmount,
    IReadOnlyCollection<DraftLineUpdateRequest> Lines);

namespace PoOcr.Api.Contracts;

internal sealed record DraftLineUpdateRequest(
    decimal Quantity,
    string ItemCode,
    string Description,
    decimal UnitPrice,
    decimal Amount);

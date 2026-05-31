namespace PoOcr.Api.Contracts;

internal sealed record DraftLineResponse(
    decimal Quantity,
    string ItemCode,
    string Description,
    decimal UnitPrice,
    decimal Amount);

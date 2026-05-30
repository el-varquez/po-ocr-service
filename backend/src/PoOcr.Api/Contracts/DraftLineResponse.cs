namespace PoOcr.Api.Contracts;

internal sealed record DraftLineResponse(
    string ItemCode,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal LineTotal);

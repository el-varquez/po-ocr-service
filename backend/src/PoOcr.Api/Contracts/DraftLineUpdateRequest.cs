namespace PoOcr.Api.Contracts;

internal sealed record DraftLineUpdateRequest(
    string ItemCode,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal LineTotal);

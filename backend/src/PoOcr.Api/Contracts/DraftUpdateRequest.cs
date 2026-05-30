namespace PoOcr.Api.Contracts;

internal sealed record DraftUpdateRequest(
    string? PoNumber,
    DateOnly? PoDate,
    string? CustomerName,
    IReadOnlyCollection<DraftLineUpdateRequest> Lines);

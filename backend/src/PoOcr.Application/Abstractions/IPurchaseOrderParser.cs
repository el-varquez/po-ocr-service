namespace PoOcr.Application.Abstractions;
public interface IPurchaseOrderParser
{
    Task<ParsedPurchaseOrder> ParseAsync(
        string text,
        CancellationToken cancellationToken
    );
}    

public sealed record ParsedPurchaseOrder(
    string? PoNumber,
    DateOnly? PoDate,
    string? CustomerName,
    IReadOnlyList<ParsedPurchaseOrderLine> Lines,
    IReadOnlyList<string> Warnings
);

public sealed record ParsedPurchaseOrderLine(
    string ItemCode,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal LineTotal
);


namespace PoOcr.Application.Abstractions;
public interface IPurchaseOrderParser
{
    Task<ParsedPurchaseOrder> ParseAsync(
        string text,
        CancellationToken cancellationToken
    );
}    

public sealed record ParsedPurchaseOrder(
    string? VendorName,
    DateOnly? PoDate,
    string? ReferenceNumber,
    DateOnly? DateExpected,
    string? ShipTo,
    string? ShipVia,
    string? PaymentTerms,
    decimal? TotalAmount,
    IReadOnlyList<ParsedPurchaseOrderLine> Lines,
    IReadOnlyList<string> Warnings);

public sealed record ParsedPurchaseOrderLine(
    decimal Quantity,
    string ItemCode,
    string Description,
    decimal UnitPrice,
    decimal Amount);


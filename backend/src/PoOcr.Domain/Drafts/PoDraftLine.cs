namespace PoOcr.Domain.Drafts;

public sealed class PoDraftLine
{
    public PoDraftLine(
        decimal quantity,
        string itemCode,
        string description,
        decimal unitPrice,
        decimal amount
    )
    {
        Quantity = quantity;
        ItemCode = itemCode.Trim();
        Description = description.Trim();
        UnitPrice = unitPrice;
        Amount = amount;
    }

    public decimal Quantity { get; set; }
    public string ItemCode { get; set; }
    public string Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    
}
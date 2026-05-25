namespace PoOcr.Domain.Drafts;

public sealed class PoDraftLine
{
    public PoDraftLine(
        string itemCode,
        string description,
        decimal quantity,
        string unit,
        decimal unitPrice,
        decimal lineTotal   
    )
    {
        ItemCode = itemCode.Trim();
        Description = description.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
    }

    public string ItemCode { get; set; }
    public string Description { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    
}
using PoOcr.Infrastructure.Parsing;

namespace PoOcr.Infrastructure.Tests.Parsing;

public sealed class RuleBasedPurchaseOrderParserTests
{
    [Fact]
    public async Task ParseAsync_WhenTextContainsHeaderAndItemLines_ReturnsStructuredPurchaseOrder()
    {
        var parser = new RuleBasedPurchaseOrderParser();
        var text = """
            PO No: PO-1001
            Date: 2026-05-25
            Customer: ABC TRADING

            ITEM-001 Sample Item 2 PCS 10.50 21.00
            ITEM-002 Second Item 3 BOX 5.00 15.00
            """;

        var parsed = await parser.ParseAsync(text, CancellationToken.None);

        Assert.Equal("PO-1001", parsed.PoNumber);
        Assert.Equal(new DateOnly(2026, 5, 25), parsed.PoDate);
        Assert.Equal("ABC TRADING", parsed.CustomerName);
        Assert.Empty(parsed.Warnings);
        Assert.Equal(2, parsed.Lines.Count);
        Assert.Equal("ITEM-001", parsed.Lines[0].ItemCode);
        Assert.Equal("Sample Item", parsed.Lines[0].Description);
        Assert.Equal(2, parsed.Lines[0].Quantity);
        Assert.Equal("PCS", parsed.Lines[0].Unit);
        Assert.Equal(10.50m, parsed.Lines[0].UnitPrice);
        Assert.Equal(21.00m, parsed.Lines[0].LineTotal);
    }

    [Fact]
    public async Task ParseAsync_WhenRequiredFieldsAreMissing_ReturnsWarningsInsteadOfFailing()
    {
        var parser = new RuleBasedPurchaseOrderParser();

        var parsed = await parser.ParseAsync("", CancellationToken.None);

        Assert.Null(parsed.PoNumber);
        Assert.Null(parsed.PoDate);
        Assert.Null(parsed.CustomerName);
        Assert.Empty(parsed.Lines);
        Assert.Contains("PO number was not found.", parsed.Warnings);
        Assert.Contains("PO date was not found.", parsed.Warnings);
        Assert.Contains("Customer name was not found.", parsed.Warnings);
        Assert.Contains("No item lines were found.", parsed.Warnings);
    }
}

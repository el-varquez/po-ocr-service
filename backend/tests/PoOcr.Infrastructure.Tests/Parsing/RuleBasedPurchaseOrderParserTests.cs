using PoOcr.Infrastructure.Parsing;

namespace PoOcr.Infrastructure.Tests.Parsing;

public sealed class RuleBasedPurchaseOrderParserTests
{
    [Fact]
    public async Task ParseAsync_WhenTextContainsHeaderAndItemLines_ReturnsStructuredPurchaseOrder()
    {
        var parser = new RuleBasedPurchaseOrderParser();
        var text = """
            Reference #: PO-1001
            Date: 2026-05-25
            Vendor: ABC TRADING
            Date Expected: 2026-06-30
            Payment Terms: Net 30
            Total Amount: P2,615.00

            5 MON2000 1877 Solera Reserva 1.75ml 523.00 2,615.00
            """;

        var parsed = await parser.ParseAsync(text, CancellationToken.None);

        Assert.Equal("PO-1001", parsed.ReferenceNumber);
        Assert.Equal(new DateOnly(2026, 5, 25), parsed.PoDate);
        Assert.Equal("ABC TRADING", parsed.VendorName);
        Assert.Equal(new DateOnly(2026, 6, 30), parsed.DateExpected);
        Assert.Equal("Net 30", parsed.PaymentTerms);
        Assert.Equal(2615, parsed.TotalAmount);
        Assert.Empty(parsed.Warnings);
        var line = Assert.Single(parsed.Lines);
        Assert.Equal("MON2000", line.ItemCode);
        Assert.Equal("1877 Solera Reserva 1.75ml", line.Description);
        Assert.Equal(5, line.Quantity);
        Assert.Equal(523, line.UnitPrice);
        Assert.Equal(2615, line.Amount);
    }

    [Fact]
    public async Task ParseAsync_WhenRequiredFieldsAreMissing_ReturnsWarningsInsteadOfFailing()
    {
        var parser = new RuleBasedPurchaseOrderParser();

        var parsed = await parser.ParseAsync("", CancellationToken.None);

        Assert.Null(parsed.ReferenceNumber);
        Assert.Null(parsed.PoDate);
        Assert.Null(parsed.VendorName);
        Assert.Null(parsed.DateExpected);
        Assert.Null(parsed.TotalAmount);
        Assert.Empty(parsed.Lines);
        Assert.Contains("Reference number was not found.", parsed.Warnings);
        Assert.Contains("PO date was not found.", parsed.Warnings);
        Assert.Contains("Vendor name was not found.", parsed.Warnings);
        Assert.Contains("Payment terms was not found.", parsed.Warnings);
        Assert.Contains("Total amount was not found.", parsed.Warnings);
        Assert.Contains("No item lines were found.", parsed.Warnings);
    }

    [Fact]
    public async Task
    ParseAsync_WhenTextUsesActiveSystemsPurchaseOrderLayout_ReturnsStructuredPurchaseOrder()
    {
        var parser = new RuleBasedPurchaseOrderParser();
        var text = """
            ActiveSystems Software Inc.
            Davao City
            Tin XXX-XXX-XXX-000

            Computer Seller
            Computer Address

            Purchase Order
            Date: Reference #:
            05/31/2026 0016

            Date Expected Ship To Ship Via Payment Terms Total Amount
            06/30/2026 Courier Net 30 P2,615.00

            Details
            Quantity Item Code Description Unit Price Amount
            5 MON2000 1877 Solera Reserva 1.75ml 523.00 2,615.00

            Total Amount -> P2,615.00
            """;

        var parsed = await parser.ParseAsync(text, CancellationToken.None);

        Assert.Equal("Computer Seller", parsed.VendorName);
        Assert.Equal(new DateOnly(2026, 5, 31), parsed.PoDate);
        Assert.Equal("0016", parsed.ReferenceNumber);
        Assert.Equal(new DateOnly(2026, 6, 30), parsed.DateExpected);
        Assert.Equal("", parsed.ShipTo);
        Assert.Equal("Courier", parsed.ShipVia);
        Assert.Equal("Net 30", parsed.PaymentTerms);
        Assert.Equal(2615, parsed.TotalAmount);

        var line = Assert.Single(parsed.Lines);
        Assert.Equal(5, line.Quantity);
        Assert.Equal("MON2000", line.ItemCode);
        Assert.Equal("1877 Solera Reserva 1.75ml", line.Description);
        Assert.Equal(523, line.UnitPrice);
        Assert.Equal(2615, line.Amount);

        Assert.Empty(parsed.Warnings);
    }

    [Fact]
    public async Task
    ParseAsync_WhenTextUsesTesseractSplitActiveSystemsLayout_ReturnsStructuredPurchaseOrder()
    {
        var parser = new RuleBasedPurchaseOrderParser();
        var text = """
            ActiveSystems Software Inc.

            Davao City

            Computer Seller
            Computer Address

            Tin XXX-X0X0O™OXK-000

            Purchase Order

            Date: Reference #:
            05/31/2026 0016

            Payment Terms Total Amount

            Date Expected Ship To Ship Via
            06/30/2026 Courier
            Details

            Quantity Item Code Description

            Net 30 P2,615.00

            Unit Price Amount

            5 MON2000 1877 Solera Reserva 1.75ml

            523.00 2,615.00

            5
            Notes:

            Ordered By:

            Printed Name & Date

            5/31/2026 7:29:06 PM

            Total Amount -> P2,615.00

            Page 1 of 1
            """;

        var parsed = await parser.ParseAsync(text, CancellationToken.None);

        Assert.Equal("Computer Seller", parsed.VendorName);
        Assert.Equal(new DateOnly(2026, 5, 31), parsed.PoDate);
        Assert.Equal("0016", parsed.ReferenceNumber);
        Assert.Equal(new DateOnly(2026, 6, 30), parsed.DateExpected);
        Assert.Equal("", parsed.ShipTo);
        Assert.Equal("Courier", parsed.ShipVia);
        Assert.Equal("Net 30", parsed.PaymentTerms);
        Assert.Equal(2615, parsed.TotalAmount);

        var line = Assert.Single(parsed.Lines);
        Assert.Equal(5, line.Quantity);
        Assert.Equal("MON2000", line.ItemCode);
        Assert.Equal("1877 Solera Reserva 1.75ml", line.Description);
        Assert.Equal(523, line.UnitPrice);
        Assert.Equal(2615, line.Amount);

        Assert.Empty(parsed.Warnings);
    }
}

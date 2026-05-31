using System.Globalization;
using System.Text.RegularExpressions;
using PoOcr.Application.Abstractions;

namespace PoOcr.Infrastructure.Parsing;

public sealed partial class RuleBasedPurchaseOrderParser : IPurchaseOrderParser
{
    public Task<ParsedPurchaseOrder> ParseAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var lines = NormalizeLines(text);

        var referenceNumber = FindLabeledValue(
            lines,
            "Reference #",
            "Reference No",
            "PO No",
            "PO Number",
            "P.O. No");

        var poDate = ParseDate(FindLabeledValue(lines, "Date", "PO Date"));

        var vendorName = FindLabeledValue(
            lines,
            "Vendor",
            "Vendor Name",
            "Customer",
            "Customer Name",
            "Client");

        var dateExpected = ParseDate(FindLabeledValue(
            lines,
            "Date Expected",
            "Date Needed",
            "Expected Date"));

        var shipTo = FindLabeledValue(lines, "Ship To");
        var shipVia = FindLabeledValue(lines, "Ship Via");
        var paymentTerms = FindLabeledValue(lines, "Payment Terms");
        var totalAmount = ParseMoneyOrNull(FindLabeledValue(lines, "Total Amount"));

        var itemLines = ParseItemLines(lines);
        var warnings = BuildWarnings(
            vendorName,
            poDate,
            referenceNumber,
            dateExpected,
            paymentTerms,
            totalAmount,
            itemLines);

        return Task.FromResult(new ParsedPurchaseOrder(
            vendorName,
            poDate,
            referenceNumber,
            dateExpected,
            shipTo,
            shipVia,
            paymentTerms,
            totalAmount,
            itemLines,
            warnings));
    }

    private static string[] NormalizeLines(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
    }

    private static string? FindLabeledValue(string[] lines, params string[] labels)
    {
        foreach (var line in lines)
        {
            foreach (var label in labels)
            {
                if (!line.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                    continue;

                var separatorIndex = line.IndexOf(':');
                var value = separatorIndex >= 0
                    ? line[(separatorIndex + 1)..]
                    : line[label.Length..];

                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }

        return null;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private static IReadOnlyList<ParsedPurchaseOrderLine> ParseItemLines(string[]
    lines)
    {
        var parsedLines = new List<ParsedPurchaseOrderLine>();

        foreach (var line in lines)
        {
            var match = ItemLineRegex().Match(line);
            if (!match.Success)
                continue;

            parsedLines.Add(new ParsedPurchaseOrderLine(
                ParseDecimal(match.Groups["quantity"].Value),
                match.Groups["itemCode"].Value.Trim(),
                match.Groups["description"].Value.Trim(),
                ParseDecimal(match.Groups["unitPrice"].Value),
                ParseDecimal(match.Groups["amount"].Value)));
        }

        return parsedLines;
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(
            value.Replace(",", "").Replace("P", "").Replace("₱", ""),
            NumberStyles.Number,
            CultureInfo.InvariantCulture);
    }

    private static decimal? ParseMoneyOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(
            value.Replace(",", "").Replace("P", "").Replace("₱", "").Trim(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private static IReadOnlyList<string> BuildWarnings(
        string? vendorName,
        DateOnly? poDate,
        string? referenceNumber,
        DateOnly? dateExpected,
        string? paymentTerms,
        decimal? totalAmount,
        IReadOnlyList<ParsedPurchaseOrderLine> itemLines)
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(vendorName))
            warnings.Add("Vendor name was not found.");

        if (poDate is null)
            warnings.Add("PO date was not found.");

        if (string.IsNullOrWhiteSpace(referenceNumber))
            warnings.Add("Reference number was not found.");

        if (dateExpected is null)
            warnings.Add("Date expected was not found.");

        if (string.IsNullOrWhiteSpace(paymentTerms))
            warnings.Add("Payment terms was not found.");

        if (totalAmount is null)
            warnings.Add("Total amount was not found.");

        if (itemLines.Count == 0)
            warnings.Add("No item lines were found.");

        return warnings;
    }

    [GeneratedRegex(@"^(?<quantity>\d+(?:\.\d+)?)\s+(?<itemCode>[A-Z0-9][A-Z0-9\-]*)\s+(?<description>.+?)\s+(?<unitPrice>\d+(?:,\d{3})*(?:\.\d+)?)\s+(?<amount>\d+(?:,\d{3})*(?:\.\d+)?)$", RegexOptions.Compiled)]
    private static partial Regex ItemLineRegex();
}
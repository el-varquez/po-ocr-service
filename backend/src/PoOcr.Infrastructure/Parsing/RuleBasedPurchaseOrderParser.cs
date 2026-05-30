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
        var poNumber = FindLabeledValue(lines, "PO No", "PO Number", "P.O. No");
        var poDate = ParseDate(FindLabeledValue(lines, "Date", "PO Date"));
        var customerName = FindLabeledValue(lines, "Customer", "Customer Name", "Client");
        var itemLines = ParseItemLines(lines);
        var warnings = BuildWarnings(poNumber, poDate, customerName, itemLines);

        return Task.FromResult(new ParsedPurchaseOrder(
            poNumber,
            poDate,
            customerName,
            itemLines,
            warnings));
    }

    private static string[] NormalizeLines(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

    private static IReadOnlyList<ParsedPurchaseOrderLine> ParseItemLines(string[] lines)
    {
        var parsedLines = new List<ParsedPurchaseOrderLine>();

        foreach (var line in lines)
        {
            var match = ItemLineRegex().Match(line);
            if (!match.Success)
                continue;

            parsedLines.Add(new ParsedPurchaseOrderLine(
                match.Groups["itemCode"].Value.Trim(),
                match.Groups["description"].Value.Trim(),
                ParseDecimal(match.Groups["quantity"].Value),
                match.Groups["unit"].Value.Trim(),
                ParseDecimal(match.Groups["unitPrice"].Value),
                ParseDecimal(match.Groups["lineTotal"].Value)));
        }

        return parsedLines;
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(
            value.Replace(",", ""),
            NumberStyles.Number,
            CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<string> BuildWarnings(
        string? poNumber,
        DateOnly? poDate,
        string? customerName,
        IReadOnlyList<ParsedPurchaseOrderLine> itemLines)
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(poNumber))
            warnings.Add("PO number was not found.");

        if (poDate is null)
            warnings.Add("PO date was not found.");

        if (string.IsNullOrWhiteSpace(customerName))
            warnings.Add("Customer name was not found.");

        if (itemLines.Count == 0)
            warnings.Add("No item lines were found.");

        return warnings;
    }

    [GeneratedRegex(@"^(?<itemCode>[A-Z0-9][A-Z0-9\-]{2,})\s+(?<description>.+?)\s+(?<quantity>\d+(?:\.\d+)?)\s+(?<unit>[A-Z]+)\s+(?<unitPrice>\d+(?:,\d{3})*(?:\.\d+)?)\s+(?<lineTotal>\d+(?:,\d{3})*(?:\.\d+)?)$")]
    private static partial Regex ItemLineRegex();
}

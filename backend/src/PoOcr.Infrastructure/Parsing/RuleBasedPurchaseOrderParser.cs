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
        var activeTerms = FindActiveSystemsTermsLine(lines);

        var referenceNumber = FindLabeledValue(
            lines,
            "Reference #",
            "Reference No",
            "PO No",
            "PO Number",
            "P.O. No")
            ?? FindActiveSystemsReferenceNumber(lines);

        var poDate = ParseDate(FindLabeledValue(lines, "Date", "PO Date"))
            ?? FindActiveSystemsPoDate(lines);

        var vendorName = FindLabeledValue(
            lines,
            "Vendor",
            "Vendor Name",
            "Customer",
            "Customer Name",
            "Client")
            ?? FindActiveSystemsVendorName(lines);

        var dateExpected = ParseDate(FindLabeledValue(
            lines,
            "Date Expected",
            "Date Needed",
            "Expected Date"))
            ?? activeTerms.DateExpected;

        var shipTo = FindLabeledValue(lines, "Ship To") ?? activeTerms.ShipTo;
        var shipVia = FindLabeledValue(lines, "Ship Via") ?? activeTerms.ShipVia;
        var paymentTerms = activeTerms.PaymentTerms ?? FindLabeledValue(lines, "Payment Terms");

        var totalAmount = ParseMoneyOrNull(FindLabeledValue(lines, "Total Amount"))
            ?? activeTerms.TotalAmount
            ?? FindActiveSystemsTotalAmount(lines);

        var itemLines = ParseItemLines(lines);
        var warnings = BuildWarnings(
            vendorName,
            poDate,
            referenceNumber,
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

    private static string? FindActiveSystemsVendorName(string[] lines)
    {
        var titleIndex = Array.FindIndex(lines, line =>
            line.Contains("Purchase Order", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Sales Order", StringComparison.OrdinalIgnoreCase));

        if (titleIndex <= 0)
            return null;

        var candidates = lines[..titleIndex]
            .Where(line => !IsActiveSystemsHeaderLine(line))
            .ToArray();

        if (candidates.Length >= 2)
            return candidates[^2];

        return candidates.LastOrDefault();
    }

    private static bool IsActiveSystemsHeaderLine(string line)
    {
        return line.Contains("ActiveSystems", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Tin", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Phone", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
            || line.Contains('@')
            || line.Contains("Davao City", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Zone", StringComparison.OrdinalIgnoreCase);
    }

    private static DateOnly? FindActiveSystemsPoDate(string[] lines)
    {
        var valuesLine = FindLineAfterDateReferenceHeader(lines);
        if (valuesLine is null)
            return null;

        var parts = SplitWords(valuesLine);
        return parts.Length == 0 ? null : ParseDate(parts[0]);
    }

    private static string? FindActiveSystemsReferenceNumber(string[] lines)
    {
        var valuesLine = FindLineAfterDateReferenceHeader(lines);
        if (valuesLine is null)
            return null;

        var parts = SplitWords(valuesLine);
        return parts.Length < 2 ? null : parts[^1];
    }

    private static string? FindLineAfterDateReferenceHeader(string[] lines)
    {
        for (var index = 0; index < lines.Length - 1; index++)
        {
            var line = lines[index];

            if (line.Contains("Date", StringComparison.OrdinalIgnoreCase)
                && line.Contains("Reference", StringComparison.OrdinalIgnoreCase))
            {
                return lines[index + 1];
            }
        }

        return null;
    }

    private static ActiveSystemsTermsLine FindActiveSystemsTermsLine(string[] lines)
    {
        DateOnly? dateExpected = null;
        string? shipTo = null;
        string? shipVia = null;
        string? paymentTerms = null;
        decimal? totalAmount = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];

            if (line.Contains("Date Expected", StringComparison.OrdinalIgnoreCase)
                && index < lines.Length - 1)
            {
                var dateLine = lines[index + 1];
                var dateMatch = DateRegex().Match(dateLine);
                if (dateMatch.Success)
                {
                    dateExpected = ParseDate(dateMatch.Value);
                    var remaining = dateLine[(dateMatch.Index + dateMatch.Length)..].Trim();
                    var netTermsMatch = NetTermsRegex().Match(remaining);

                    if (netTermsMatch.Success)
                    {
                        (shipTo, shipVia) = ParseShipFields(remaining[..netTermsMatch.Index]);
                        paymentTerms = netTermsMatch.Value;
                        totalAmount ??= ParseMoneyOrNull(remaining);
                    }
                    else
                    {
                        (shipTo, shipVia) = ParseShipFields(remaining);
                    }
                }
            }

            var paymentTermsMatch = NetTermsRegex().Match(line);
            if (paymentTermsMatch.Success)
            {
                paymentTerms = paymentTermsMatch.Value;
                totalAmount ??= ParseMoneyOrNull(line);
            }
        }

        return new ActiveSystemsTermsLine(
            dateExpected,
            shipTo,
            shipVia,
            paymentTerms,
            totalAmount);
    }

    private static (string ShipTo, string ShipVia) ParseShipFields(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ("", "");

        var parts = SplitWords(value);
        if (parts.Length == 1)
            return ("", parts[0]);

        return (string.Join(' ', parts[..^1]), parts[^1]);
    }

    private static decimal? FindActiveSystemsTotalAmount(string[] lines)
    {
        foreach (var line in lines.Reverse())
        {
            if (!line.Contains("Total Amount", StringComparison.OrdinalIgnoreCase))
                continue;

            var matches = MoneyRegex().Matches(line);
            if (matches.Count == 0)
                continue;

            return ParseMoneyOrNull(matches[^1].Value);
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

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var match = ItemLineRegex().Match(line);
            if (match.Success)
            {
                parsedLines.Add(new ParsedPurchaseOrderLine(
                    ParseDecimal(match.Groups["quantity"].Value),
                    match.Groups["itemCode"].Value.Trim(),
                    match.Groups["description"].Value.Trim(),
                    ParseDecimal(match.Groups["unitPrice"].Value),
                    ParseDecimal(match.Groups["amount"].Value)));

                continue;
            }

            var itemWithoutPriceMatch = ItemWithoutPriceRegex().Match(line);
            if (!itemWithoutPriceMatch.Success)
                continue;

            var priceLine = FindNextPriceLine(lines, index + 1);
            if (priceLine is null)
                continue;

            parsedLines.Add(new ParsedPurchaseOrderLine(
                ParseDecimal(itemWithoutPriceMatch.Groups["quantity"].Value),
                itemWithoutPriceMatch.Groups["itemCode"].Value.Trim(),
                itemWithoutPriceMatch.Groups["description"].Value.Trim(),
                ParseDecimal(priceLine.Value.UnitPrice),
                ParseDecimal(priceLine.Value.Amount)));
        }

        return parsedLines;
    }

    private static (string UnitPrice, string Amount)? FindNextPriceLine(
        string[] lines,
        int startIndex)
    {
        for (var index = startIndex; index < lines.Length; index++)
        {
            var match = PriceLineRegex().Match(lines[index]);
            if (!match.Success)
                continue;

            return (
                match.Groups["unitPrice"].Value,
                match.Groups["amount"].Value);
        }

        return null;
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(
            CleanMoney(value),
            NumberStyles.Number,
            CultureInfo.InvariantCulture);
    }

    private static decimal? ParseMoneyOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var moneyMatch = MoneyRegex().Match(value);
        var moneyValue = moneyMatch.Success ? moneyMatch.Value : value;

        return decimal.TryParse(
            CleanMoney(moneyValue),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private static string CleanMoney(string value)
    {
        return value
            .Replace(",", "")
            .Replace("P", "")
            .Replace("\u20B1", "")
            .Trim();
    }

    private static string[] SplitWords(string value)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IReadOnlyList<string> BuildWarnings(
        string? vendorName,
        DateOnly? poDate,
        string? referenceNumber,
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

        if (string.IsNullOrWhiteSpace(paymentTerms))
            warnings.Add("Payment terms was not found.");

        if (totalAmount is null)
            warnings.Add("Total amount was not found.");

        if (itemLines.Count == 0)
            warnings.Add("No item lines were found.");

        return warnings;
    }

    private sealed record ActiveSystemsTermsLine(
        DateOnly? DateExpected,
        string? ShipTo,
        string? ShipVia,
        string? PaymentTerms,
        decimal? TotalAmount);

    [GeneratedRegex(@"^(?<quantity>\d+(?:\.\d+)?)\s+(?<itemCode>[A-Z0-9][A-Z0-9\-]*)\s+(?<description>.+?)\s+(?<unitPrice>\d+(?:,\d{3})*(?:\.\d+)?)\s+(?<amount>\d+(?:,\d{3})*(?:\.\d+)?)(?:\s+[A-Z])?$", RegexOptions.Compiled)]
    private static partial Regex ItemLineRegex();

    [GeneratedRegex(@"^(?<quantity>\d+(?:\.\d+)?)\s+(?<itemCode>[A-Z0-9][A-Z0-9\-]*)\s+(?<description>.+)$", RegexOptions.Compiled)]
    private static partial Regex ItemWithoutPriceRegex();

    [GeneratedRegex(@"^(?<unitPrice>\d+(?:,\d{3})*(?:\.\d+)?)\s+(?<amount>\d+(?:,\d{3})*(?:\.\d+)?)$", RegexOptions.Compiled)]
    private static partial Regex PriceLineRegex();

    [GeneratedRegex(@"\d{1,2}/\d{1,2}/\d{4}", RegexOptions.Compiled)]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"(?:P|₱)?\d+(?:,\d{3})*(?:\.\d{2,4})", RegexOptions.Compiled)]
    private static partial Regex MoneyRegex();

    [GeneratedRegex(@"Net\s+\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NetTermsRegex();
}

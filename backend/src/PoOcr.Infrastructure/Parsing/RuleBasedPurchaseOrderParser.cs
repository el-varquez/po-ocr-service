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

        var activeTerms = FindActiveSystemsTermsLine(lines);

        var dateExpected = ParseDate(FindLabeledValue(
            lines,
            "Date Expected",
            "Date Needed",
            "Expected Date"))
            ?? activeTerms.DateExpected;

        var shipTo = FindLabeledValue(lines, "Ship To") ?? activeTerms.ShipTo;
        var shipVia = FindLabeledValue(lines, "Ship Via") ?? activeTerms.ShipVia;
        var paymentTerms = FindLabeledValue(lines, "Payment Terms") ?? activeTerms.PaymentTerms;

        var totalAmount = ParseMoneyOrNull(FindLabeledValue(lines, "Total Amount"))
            ?? activeTerms.TotalAmount
            ?? FindActiveSystemsTotalAmount(lines);

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
        for (var index = 0; index < lines.Length - 1; index++)
        {
            var headerLine = lines[index];

            if (!headerLine.Contains("Date Expected", StringComparison.OrdinalIgnoreCase)
                || !headerLine.Contains("Payment Terms", StringComparison.OrdinalIgnoreCase)
                || !headerLine.Contains("Total Amount", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return ParseActiveSystemsTermsLine(lines[index + 1]);
        }

        return new ActiveSystemsTermsLine(null, null, null, null, null);
    }

    private static ActiveSystemsTermsLine ParseActiveSystemsTermsLine(string line)
    {
        var totalAmountMatch = MoneyRegex().Match(line);
        var totalAmount = totalAmountMatch.Success
            ? ParseMoneyOrNull(totalAmountMatch.Value)
            : null;

        var withoutTotal = totalAmountMatch.Success
            ? line[..totalAmountMatch.Index].Trim()
            : line.Trim();

        var dateMatch = DateRegex().Match(withoutTotal);
        var dateExpected = dateMatch.Success
            ? ParseDate(dateMatch.Value)
            : null;

        var remaining = dateMatch.Success
            ? withoutTotal[(dateMatch.Index + dateMatch.Length)..].Trim()
            : withoutTotal;

        var netTermsMatch = NetTermsRegex().Match(remaining);
        if (netTermsMatch.Success)
        {
            var beforePaymentTerms = remaining[..netTermsMatch.Index].Trim();
            var beforeTermsParts = SplitWords(beforePaymentTerms);

            var shipVia = beforeTermsParts.Length == 0 ? "" : beforeTermsParts[^1];
            var shipTo = beforeTermsParts.Length <= 1
                ? ""
                : string.Join(' ', beforeTermsParts[..^1]);

            return new ActiveSystemsTermsLine(
                dateExpected,
                shipTo,
                shipVia,
                netTermsMatch.Value,
                totalAmount);
        }

        return new ActiveSystemsTermsLine(
            dateExpected,
            "",
            "",
            string.IsNullOrWhiteSpace(remaining) ? null : remaining,
            totalAmount);
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
            CleanMoney(value),
            NumberStyles.Number,
            CultureInfo.InvariantCulture);
    }

    private static decimal? ParseMoneyOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(
            CleanMoney(value),
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
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries |
        StringSplitOptions.TrimEntries);
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

    private sealed record ActiveSystemsTermsLine(
        DateOnly? DateExpected,
        string? ShipTo,
        string? ShipVia,
        string? PaymentTerms,
        decimal? TotalAmount);

    [GeneratedRegex(@"^(?<quantity>\d+(?:\.\d+)?)\s+(?<itemCode>[A-Z0-9][A-Z0-9\-]*)\s+(?<description>.+?)\s+(?<unitPrice>\d+(?:,\d{3})*(?:\.\d+)?)\s+(?<amount>\d+(?:,\d{3})*(?:\.\d+)?)(?:\s+[A-Z])?$", RegexOptions.Compiled)]
    private static partial Regex ItemLineRegex();

    [GeneratedRegex(@"\d{1,2}/\d{1,2}/\d{4}", RegexOptions.Compiled)]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"(?:P|₱)?\d+(?:,\d{3})*(?:\.\d{2,4})", RegexOptions.Compiled)]
    private static partial Regex MoneyRegex();

    [GeneratedRegex(@"Net\s+\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NetTermsRegex();
}
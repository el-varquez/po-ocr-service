namespace PoOcr.Domain.Drafts;

public sealed class PoDraft
{
    private readonly List<PoDraftLine> _lines = [];
    private readonly List<string> _warnings = [];

    private PoDraft(Guid id, Guid uploadFileId, string createdBy)
    {
        Id = id;
        UploadFileId = uploadFileId;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UploadFileId { get; private set; }

    public string VendorName { get; private set; } = "";
    public DateOnly? PoDate { get; private set; }
    public string ReferenceNumber { get; private set ; } = "";
    public DateOnly? DateExpected { get; private set; }
    public string ShipTo { get; private set; } = "";
    public string ShipVia { get; private set; } = "";
    public string PaymentTerms { get; private set; } = "";
    public decimal? TotalAmount { get; private set; }

    public string CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<PoDraftLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<string> Warnings => _warnings.AsReadOnly();

    public static PoDraft CreateFromExtraction(
        Guid uploadFileId,
        string? vendorName,
        DateOnly? poDate,
        string? referenceNumber,
        DateOnly? dateExpected,
        string? shipTo,
        string? shipVia,
        string? paymentTerms,
        decimal? totalAmount,
        IEnumerable<PoDraftLine> lines,
        string createdBy
        )
    {
        if (uploadFileId == Guid.Empty)
            throw new ArgumentException("Upload file id is required",
            nameof(uploadFileId));

        var draft = new PoDraft(Guid.NewGuid(), uploadFileId, createdBy.Trim());

        draft.ApplyValues(
            vendorName,
            poDate,
            referenceNumber,
            dateExpected,
            shipTo,
            shipVia,
            paymentTerms,
            totalAmount,
            lines
        );
        draft.RefreshWarnings();

        return draft;
    }

    public void SaveChanges(
        string? vendorName,
        DateOnly? poDate,
        string? referenceNumber,
        DateOnly? dateExpected,
        string? shipTo,
        string? shipVia,
        string? paymentTerms,
        decimal? totalAmount,
        IEnumerable<PoDraftLine> lines,
        string changedBy)
    {
        if (string.IsNullOrWhiteSpace(changedBy))
            throw new ArgumentException("Changed by is required.", nameof(changedBy));

        ApplyValues(
            vendorName,
            poDate,
            referenceNumber,
            dateExpected,
            shipTo,
            shipVia,
            paymentTerms,
            totalAmount,
            lines
        );
        UpdatedBy = changedBy.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        RefreshWarnings();
    }

    private void ApplyValues( 
        string? vendorName,
        DateOnly? poDate,
        string? referenceNumber,
        DateOnly? dateExpected,
        string? shipTo,
        string? shipVia,
        string? paymentTerms,
        decimal? totalAmount,
        IEnumerable<PoDraftLine> lines)
    {
        VendorName = vendorName?.Trim() ?? "";
        PoDate = poDate;
        ReferenceNumber = referenceNumber?.Trim() ?? "";
        DateExpected = dateExpected;
        ShipTo = shipTo?.Trim() ?? "";
        ShipVia = shipVia?.Trim() ?? "";
        PaymentTerms = paymentTerms?.Trim() ?? "";
        TotalAmount = totalAmount;

        _lines.Clear();
        _lines.AddRange(lines);
    }

    private void RefreshWarnings()
    {
        _warnings.Clear();

        if (string.IsNullOrWhiteSpace(VendorName))
            _warnings.Add("Vendor name is missing.");

        if (PoDate is null)
            _warnings.Add("PO date is missing.");

        if (string.IsNullOrWhiteSpace(ReferenceNumber))
            _warnings.Add("Reference number is missing.");

        if (DateExpected is null)
            _warnings.Add("Date expected is missing.");

        if (string.IsNullOrWhiteSpace(PaymentTerms))
            _warnings.Add("Payment terms is missing.");

        if (TotalAmount is null)
            _warnings.Add("Total amount is missing.");

        if (_lines.Count == 0)
            _warnings.Add("No PO lines were extracted.");
    }
}

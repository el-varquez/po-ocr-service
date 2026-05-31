namespace PoOcr.Domain.Drafts;

public sealed class PoDraft
{
    private readonly List<PoDraftLine> _lines = [];

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
    public DateTimeOffset? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }
    public bool IsDeleted => DeletedAt is not null;

    public IReadOnlyCollection<PoDraftLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<string> Warnings => BuildWarnings();

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

    }

    public void SoftDelete(string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("Deleted by is required.", nameof(deletedBy));

        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy.Trim();
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

    private IReadOnlyCollection<string> BuildWarnings()
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(VendorName))
            warnings.Add("Vendor name is missing.");

        if (PoDate is null)
            warnings.Add("PO date is missing.");

        if (string.IsNullOrWhiteSpace(ReferenceNumber))
            warnings.Add("Reference number is missing.");

        if (DateExpected is null)
            warnings.Add("Date expected is missing.");

        if (string.IsNullOrWhiteSpace(PaymentTerms))
            warnings.Add("Payment terms is missing.");

        if (TotalAmount is null)
            warnings.Add("Total amount is missing.");

        if (_lines.Count == 0)
            warnings.Add("No PO lines were extracted.");

        return warnings;
    }
}

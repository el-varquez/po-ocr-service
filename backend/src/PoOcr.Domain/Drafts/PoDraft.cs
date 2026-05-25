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
    public string PoNumber { get; private set; } = "";
    public DateOnly? PoDate { get; private set; }
    public string CustomerName { get; private set; } = "";
    public string CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<PoDraftLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<string> Warnings => _warnings.AsReadOnly();

    public static PoDraft CreateFromExtraction(
        Guid uploadFileId,
        string? poNumber,
        DateOnly? poDate,
        string? customerName,
        IEnumerable<PoDraftLine> lines,
        string createdBy)
    {
        if (uploadFileId == Guid.Empty)
            throw new ArgumentException("Upload file id is required",
            nameof(uploadFileId));

        var draft = new PoDraft(Guid.NewGuid(), uploadFileId, createdBy.Trim());

        draft.ApplyValues(poNumber, poDate, customerName, lines);
        draft.RefreshWarnings();

        return draft;
    }

      public void SaveChanges(
          string? poNumber,
          DateOnly? poDate,
          string? customerName,
          IEnumerable<PoDraftLine> lines,
          string changedBy)
      {
          ApplyValues(poNumber, poDate, customerName, lines);
          UpdatedBy = changedBy.Trim();
          UpdatedAt = DateTimeOffset.UtcNow;

          RefreshWarnings();
      }

      private void ApplyValues(
          string? poNumber,
          DateOnly? poDate,
          string? customerName,
          IEnumerable<PoDraftLine> lines)
      {
          PoNumber = poNumber?.Trim() ?? "";
          PoDate = poDate;
          CustomerName = customerName?.Trim() ?? "";

          _lines.Clear();
          _lines.AddRange(lines);
      }

      private void RefreshWarnings()
      {
          _warnings.Clear();

          if (string.IsNullOrWhiteSpace(PoNumber))
              _warnings.Add("PO number is missing.");

          if (PoDate is null)
              _warnings.Add("PO date is missing.");

          if (string.IsNullOrWhiteSpace(CustomerName))
              _warnings.Add("Customer name is missing.");

          if (_lines.Count == 0)
              _warnings.Add("No PO lines were extracted.");
      }
}

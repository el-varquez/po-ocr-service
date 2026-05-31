namespace PoOcr.Domain.Uploads;

public sealed class UploadFile
{
    private UploadFile()
    {
        OriginalFileName = "";
        ContentType = "";
        StorePath = "";
        CheckSum = "";
        UploadedBy = "";
    }

    private UploadFile(
        Guid id,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storedPath,
        string checksum,
        string uploadedBy,
        DateTimeOffset uploadedAt)
    {
        Id = id;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorePath = storedPath;
        CheckSum = checksum;
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAt;
        Status = UploadStatus.PendingExtraction;
    }

    public Guid Id { get; private set; }
    public string OriginalFileName { get; set; }
    public string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string StorePath { get; set; }
    public string CheckSum { get; set; }
    public string UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public UploadStatus Status { get; set; }
    public string? FailureReason { get; private set; }

    public static UploadFile Create(
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storedPath,
        string checksum,
        string uploadedBy)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name is required.",
            nameof(originalFileName));

        if (sizeBytes <= 0)
            throw new ArgumentException(nameof(sizeBytes), "File size must be greated than zeo.");

        return new UploadFile(
            Guid.NewGuid(),
            originalFileName.Trim(),
            contentType.Trim(),
            sizeBytes,
            storedPath.Trim(),
            checksum.Trim(),
            uploadedBy.Trim(),
            DateTimeOffset.UtcNow);
    } 

    public void QueueForExtraction()
    {
        if (Status is not UploadStatus.PendingExtraction and not UploadStatus.Failed)
            throw new InvalidOperationException($"Upload cannot be queued from status{Status}.");

        Status = UploadStatus.QueuedForExtraction;
        FailureReason = null;
    }

    public void MarkExtracting()
    {
        if (Status != UploadStatus.QueuedForExtraction)
            throw new InvalidOperationException($"Upload cannot start extraction from status {Status}.");

        Status = UploadStatus.Extracting;
    }

    public void MarkNeedsReview()
    {
        if (Status != UploadStatus.Extracting)
            throw new InvalidOperationException($"Upload cannot move to review from status {Status}.");
        
        Status = UploadStatus.NeedsReview;
    }

    public void MarkSaved()
    {
        if (Status != UploadStatus.NeedsReview)
        throw new InvalidOperationException($"Upload cannot be saved from status {Status}.");

        Status = UploadStatus.Saved;
    }

    public void MarkFailed(string error)
    {
        Status = UploadStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(error) ? "Unknown extraction failur." : error.Trim();
    }
}

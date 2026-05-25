using PoOcr.Domain.Uploads;

namespace PoOcr.Domain.Extraction;

public sealed class ExtractionJob
{
    private ExtractionJob(Guid id, Guid uploadFileId, DateTimeOffset queuedAt)
    {
        Id = id;
        UploadFileId = uploadFileId;
        QueuedAt = queuedAt;
    }

    public Guid Id { get; set; }
    public Guid UploadFileId { get; set; }
    public DateTimeOffset QueuedAt { get; set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public bool IsCompleted => CompletedAt.HasValue;
    public bool IsFailed => !string.IsNullOrWhiteSpace(FailureReason);

    public static ExtractionJob Create(Guid uploadFileId)
    {
        if (uploadFileId == Guid.Empty)
            throw new ArgumentException("Upload file id is required.",
            nameof(uploadFileId));

        return new ExtractionJob(Guid.NewGuid(), uploadFileId,
        DateTimeOffset.UtcNow);
    }

    public void Start()
    {
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        CompletedAt = DateTimeOffset.UtcNow;
        FailureReason = null;
    }

    public void Fail(string reason)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Unknown extraction failure." : reason.Trim();
    }
}

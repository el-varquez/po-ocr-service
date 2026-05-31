namespace PoOcr.Domain.Uploads;

public enum UploadStatus
{
    PendingExtraction = 1,
    QueuedForExtraction = 2,
    Extracting = 3,
    NeedsReview = 4,
    Saved = 5,
    Failed = 6,
}

namespace PoOcr.Api.Contracts;

internal sealed record QueueExtractionRequest(IReadOnlyCollection<Guid> UploadIds);

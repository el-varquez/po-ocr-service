using PoOcr.Application.Abstractions;
using PoOcr.Application.Common;
using PoOcr.Domain.Extraction;

namespace PoOcr.Application.Extraction;

public sealed class QueueExtractionUseCase(
    IUploadRepository uploadRepository,
    IExtractionJobRepository extractionJobRepository,
    IAuditWriter auditWriter,
    IExtractionJobSignal extractionJobSignal)
{
      public async Task<ApplicationResult> Handle(
          QueueExtractionCommand command,
          CancellationToken cancellationToken)
    {
        if (command.UploadIds.Count == 0)
            return ApplicationResult.Failure("At least one upload is required.");

        if (string.IsNullOrWhiteSpace(command.Actor))
            return ApplicationResult.Failure("Actor is required");

        var uploads = await uploadRepository.GetByIdAsync(
            command.UploadIds,
            cancellationToken
        );

        if (uploads.Count != command.UploadIds.Count)
            return ApplicationResult.Failure("One or more uploads were not found.");

        try
        {
            foreach (var upload in uploads)
            {
                upload.QueueForExtraction();

                var job = ExtractionJob.Create(upload.Id);
                await extractionJobRepository.AddAsync(job,
                cancellationToken);

                await auditWriter.WriteAsync(
                    "extraction.queued",
                    command.Actor,
                    $"Queued upload {upload.OriginalFileName} for extractions",
                    cancellationToken
                );
            }
        }
        catch (InvalidOperationException ex)
        {
            return ApplicationResult.Failure(ex.Message);
        }

        await uploadRepository.SaveChangesAsync(cancellationToken);
        await extractionJobRepository.SaveChangesAsync(cancellationToken);

        extractionJobSignal.NotifyJobQueued();

        return ApplicationResult.Success();
    }
}

public sealed record QueueExtractionCommand(
    IReadOnlyCollection<Guid> UploadIds,
    string Actor
);

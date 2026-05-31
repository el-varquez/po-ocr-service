using PoOcr.Application.Abstractions;
using PoOcr.Domain.Drafts;

namespace PoOcr.Application.Extraction;

public sealed class ProcessNextExtractionJobUseCase(
    IExtractionJobRepository extractionJobRepository,
    IUploadRepository uploadRepository,
    IDraftRepository draftRepository,
    IFileTextExtractor fileTextExtractor,
    IPurchaseOrderParser purchaseOrderParser,
    IAuditWriter auditWriter) : IExtractionJobProcessor
{
    public async Task<ProcessNextExtractionJobResult> Handle(
        CancellationToken cancellationToken)
    {
        var job = await extractionJobRepository.GetNextQueuedAsync(cancellationToken);

        if (job is null)
            return new ProcessNextExtractionJobResult(true, false, "");

        var uploads = await uploadRepository.GetByIdAsync(
            [job.UploadFileId],
            cancellationToken
        );

        var upload = uploads.SingleOrDefault();

        if (upload is null)
            return new ProcessNextExtractionJobResult(false, false, "Upload was not fount");

        try
        {
            upload.MarkExtracting();
            job.Start();

            var text = await fileTextExtractor.ExtractTextAsync(
                upload.StorePath,
                upload.ContentType,
                cancellationToken
            );

            var parsed = await purchaseOrderParser.ParseAsync(
                text,
                cancellationToken
            );

            var draftLines = parsed.Lines.Select(line => new PoDraftLine(
                line.Quantity,
                line.ItemCode,
                line.Description,
                line.UnitPrice,
                line.Amount
            ));

            var draft = PoDraft.CreateFromExtraction(
                upload.Id,
                parsed.VendorName,
                parsed.PoDate,
                parsed.ReferenceNumber,
                parsed.DateExpected,
                parsed.ShipTo,
                parsed.ShipVia,
                parsed.PaymentTerms,
                parsed.TotalAmount,
                draftLines,
                "system"
            );

            await draftRepository.AddAsync(draft, cancellationToken);

            upload.MarkNeedsReview();
            job.Complete();

            await auditWriter.WriteAsync(
                "extraction.completed",
                "system",
                $"Extraction completed for {upload.OriginalFileName}.",
                cancellationToken
            );
            await uploadRepository.SaveChangesAsync(cancellationToken);
            await extractionJobRepository.SaveChangesAsync(cancellationToken);
            await draftRepository.SaveChangesAsync(cancellationToken);

            return new ProcessNextExtractionJobResult(true, true, "");
        }
        catch (Exception ex)
        {
            upload.MarkFailed(ex.Message);
            job.Fail(ex.Message);
            
            await auditWriter.WriteAsync(
                "extraction.failed",
                "system",
                $"Extraction failed for {upload.OriginalFileName}: {ex.Message}",
                cancellationToken
            );

            await uploadRepository.SaveChangesAsync(cancellationToken);
            await extractionJobRepository.SaveChangesAsync(cancellationToken);
            
            return new ProcessNextExtractionJobResult(false, true, ex.Message);
        }
    }
}

public sealed record ProcessNextExtractionJobResult(
    bool IsSuccess,
    bool ProcessedJob,
    string Error
);

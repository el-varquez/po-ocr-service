using PoOcr.Api.Contracts;
using PoOcr.Application.Abstractions;
using PoOcr.Domain.Drafts;
using PoOcr.Domain.Uploads;

namespace PoOcr.Api.Api;

internal static class History
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/history/uploads", async (
            IUploadRepository uploadRepository,
            CancellationToken cancellationToken) =>
        {
            var uploads = await uploadRepository.GetHistoryAsync(100, cancellationToken);

            return Results.Ok(uploads.Select(ToUploadHistoryResponse));
        });

        app.MapGet("/api/history/drafts", async (
            IDraftRepository draftRepository,
            CancellationToken cancellationToken) =>
        {
            var drafts = await draftRepository.GetHistoryAsync(100, cancellationToken);

            return Results.Ok(drafts.Select(ToDraftHistoryResponse));
        });
    }

    private static UploadHistoryResponse ToUploadHistoryResponse(UploadFile upload)
    {
        return new UploadHistoryResponse(
            upload.Id,
            upload.OriginalFileName,
            upload.ContentType,
            upload.SizeBytes,
            upload.Status.ToString(),
            upload.UploadedBy,
            upload.UploadedAt,
            upload.FailureReason,
            upload.IsDeleted,
            upload.DeletedAt,
            upload.DeletedBy);
    }

    private static DraftHistoryResponse ToDraftHistoryResponse(PoDraft draft)
    {
        return new DraftHistoryResponse(
            draft.Id,
            draft.UploadFileId,
            draft.VendorName,
            draft.PoDate,
            draft.ReferenceNumber,
            draft.DateExpected,
            draft.PaymentTerms,
            draft.TotalAmount,
            draft.Lines.Count,
            draft.CreatedAt,
            draft.Warnings,
            draft.IsDeleted,
            draft.DeletedAt,
            draft.DeletedBy);
    }
}

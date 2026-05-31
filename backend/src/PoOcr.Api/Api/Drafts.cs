using PoOcr.Api.Contracts;
using PoOcr.Application.Abstractions;
using PoOcr.Domain.Drafts;

namespace PoOcr.Api.Api;

internal static class Drafts
{
public static void Map(IEndpointRouteBuilder app)
{
    app.MapGet("/api/drafts", async (
        IDraftRepository draftRepository,
        CancellationToken cancellationToken) =>
    {
        var drafts = await draftRepository.GetRecentAsync(100, cancellationToken);

        return Results.Ok(drafts.Select(ToListResponse));
    });

    app.MapGet("/api/drafts/{draftId:guid}", async (
        Guid draftId,
        IDraftRepository draftRepository,
        CancellationToken cancellationToken) =>
    {
        var draft = await draftRepository.GetByIdAsync(draftId, cancellationToken);

        return draft is null
            ? Results.NotFound()
            : Results.Ok(ToDetailResponse(draft));
    });

    app.MapPut("/api/drafts/{draftId:guid}", async (
        Guid draftId,
        DraftUpdateRequest request,
        IDraftRepository draftRepository,
        CancellationToken cancellationToken) =>
    {
        var draft = await draftRepository.GetByIdAsync(draftId, cancellationToken);
        if (draft is null)
            return Results.NotFound();

            draft.SaveChanges(
                request.VendorName,
                request.PoDate,
                request.ReferenceNumber,
                request.DateExpected,
                request.ShipTo,
                request.ShipVia,
                request.PaymentTerms,
                request.TotalAmount,
                request.Lines.Select(ToDraftLine),
                "test-user");

        await draftRepository.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToDetailResponse(draft));
    });
}

    private static DraftListResponse ToListResponse(PoDraft draft)
    {
        return new DraftListResponse(
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
            draft.Warnings);
    }

    private static DraftDetailResponse ToDetailResponse(PoDraft draft)
    {
        return new DraftDetailResponse(
            draft.Id,
            draft.UploadFileId,
            draft.VendorName,
            draft.PoDate,
            draft.ReferenceNumber,
            draft.DateExpected,
            draft.ShipTo,
            draft.ShipVia,
            draft.PaymentTerms,
            draft.TotalAmount,
            draft.CreatedAt,
            draft.Warnings,
            draft.Lines.Select(ToLineResponse).ToList());
    }

    private static DraftLineResponse ToLineResponse(PoDraftLine line)
    {
        return new DraftLineResponse(
            line.Quantity,
            line.ItemCode,
            line.Description,
            line.UnitPrice,
            line.Amount);
    }

    private static PoDraftLine ToDraftLine(DraftLineUpdateRequest line)
    {
        return new PoDraftLine(
            line.Quantity,
            line.ItemCode,
            line.Description,
            line.UnitPrice,
            line.Amount);
    }
}

using PoOcr.Api.Contracts;
using PoOcr.Application.Abstractions;
using PoOcr.Domain.Uploads;

namespace PoOcr.Api.Api;

internal static class Uploads
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/uploads", async (
            IUploadRepository uploadRepository,
            CancellationToken cancellationToken) =>
        {
            var uploads = await uploadRepository.GetRecentAsync(100, cancellationToken);

            return Results.Ok(uploads.Select(upload => new UploadResponse(
                upload.Id,
                upload.OriginalFileName,
                upload.ContentType,
                upload.SizeBytes,
                upload.Status.ToString(),
                upload.UploadedBy,
                upload.UploadedAt,
                upload.FailureReason)));
        });

        app.MapPost("/api/uploads", async (
            HttpRequest request,
            IFileStorage fileStorage,
            IUploadRepository uploadRepository,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Multipart form upload is required.");

            var form = await request.ReadFormAsync(cancellationToken);
            if (form.Files.Count == 0)
                return Results.BadRequest("At least one file is required.");

            var responses = new List<UploadResponse>();

            foreach (var file in form.Files)
            {
                await using var fileStream = file.OpenReadStream();
                var storedFile = await fileStorage.SaveAsync(
                    new FileStorageRequest(
                        file.FileName,
                        file.ContentType,
                        fileStream,
                        "test-user"),
                    cancellationToken);

                var upload = UploadFile.Create(
                    storedFile.OriginalFileName,
                    storedFile.ContentType,
                    storedFile.SizeBytes,
                    storedFile.StoredPath,
                    storedFile.CheckSum,
                    storedFile.UploadedBy);

                await uploadRepository.AddAsync(upload, cancellationToken);

                responses.Add(new UploadResponse(
                    upload.Id,
                    upload.OriginalFileName,
                    upload.ContentType,
                    upload.SizeBytes,
                    upload.Status.ToString(),
                    upload.UploadedBy,
                    upload.UploadedAt,
                    upload.FailureReason));
            }

            await uploadRepository.SaveChangesAsync(cancellationToken);

            return Results.Ok(responses);
        });

        app.MapDelete("/api/uploads/{uploadId:guid}", async (
            Guid uploadId,
            IUploadRepository uploadRepository,
            CancellationToken cancellationToken) =>
        {
            var uploads = await uploadRepository.GetByIdAsync([uploadId], cancellationToken);
            var upload = uploads.SingleOrDefault();

            if (upload is null)
                return Results.NotFound();

            upload.SoftDelete("test-user");
            await uploadRepository.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });
    }
}

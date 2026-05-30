using PoOcr.Api.Contracts;
using PoOcr.Application.Extraction;

namespace PoOcr.Api.Api;

internal static class Extraction
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/extraction/queue", async (
            QueueExtractionRequest request,
            QueueExtractionUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var result = await useCase.Handle(
                new QueueExtractionCommand(request.UploadIds, "test-user"),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok()
                : Results.BadRequest(result.Error);
        });
    }
}

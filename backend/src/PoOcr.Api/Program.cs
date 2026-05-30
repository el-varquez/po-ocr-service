using Microsoft.EntityFrameworkCore;
using PoOcr.Application.Abstractions;
using PoOcr.Application.Extraction;
using PoOcr.Domain.Uploads;
using PoOcr.Infrastructure.Persistence;
using PoOcr.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OcrDatabase");

if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("Connection string 'OcrDatabase' is required.");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<OcrDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}
builder.Services.AddScoped<IUploadRepository, UploadRepository>();
builder.Services.AddScoped<IExtractionJobRepository, ExtractionJobRepository>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<QueueExtractionUseCase>();
builder.Services.AddScoped<IFileStorage>(_ =>
{
    var storageRoot = builder.Configuration["FileStorage:RootPath"]
        ?? Path.Combine(AppContext.BaseDirectory, "uploads");

    return new LocalFileStorage(new LocalFileStorageOptions(storageRoot));
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

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

app.Run();

public partial class Program;

internal sealed record UploadResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Status,
    string UploadedBy,
    DateTimeOffset UploadedAt,
    string? FailureReason);

internal sealed record QueueExtractionRequest(IReadOnlyCollection<Guid> UploadIds);

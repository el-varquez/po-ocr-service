using Microsoft.EntityFrameworkCore;
using PoOcr.Application.Abstractions;
using PoOcr.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OcrDatabase")
    ?? "server=localhost;database=ocr_service;user=root;password=M@st3rk3y";

builder.Services.AddDbContext<OcrDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddScoped<IUploadRepository, UploadRepository>();

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

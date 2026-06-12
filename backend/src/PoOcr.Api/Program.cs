using Microsoft.EntityFrameworkCore;
using PoOcr.Api.Api;
using PoOcr.Api.Workers;
using PoOcr.Application.Abstractions;
using PoOcr.Application.Extraction;
using PoOcr.Infrastructure.Configuration;
using PoOcr.Infrastructure.Messaging;
using PoOcr.Infrastructure.Ocr;
using PoOcr.Infrastructure.Parsing;
using PoOcr.Infrastructure.Persistence;
using PoOcr.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

var connectionString = OcrDatabaseConnectionString.Resolve(
    builder.Configuration.GetConnectionString("OcrDatabase"));

if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("Connection string 'OcrDatabase' is required.");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<OcrDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

    if (!builder.Environment.IsEnvironment("Testing"))
        builder.Services.AddHostedService<ExtractionWorker>();
}
builder.Services.AddScoped<IUploadRepository, UploadRepository>();
builder.Services.AddScoped<IExtractionJobRepository, ExtractionJobRepository>();
builder.Services.AddScoped<IDraftRepository, DraftRepository>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<QueueExtractionUseCase>();
builder.Services.AddScoped<IPurchaseOrderParser, RuleBasedPurchaseOrderParser>();
builder.Services.AddScoped<IFileTextExtractor>(_ =>
{
    var executablePath = builder.Configuration["Tesseract:ExecutablePath"]
        ?? @"C:\Program Files\Tesseract-OCR\tesseract.exe";
    var language = builder.Configuration["Tesseract:Language"] ?? "eng";
    var timeoutSeconds = builder.Configuration.GetValue("Tesseract:TimeoutSeconds", 60);

    return new TesseractFileTextExtractor(new TesseractOptions(
        executablePath,
        language,
        timeoutSeconds));
});
builder.Services.AddScoped<ProcessNextExtractionJobUseCase>();
builder.Services.AddScoped<IExtractionJobProcessor>(provider =>
    provider.GetRequiredService<ProcessNextExtractionJobUseCase>());
builder.Services.AddSingleton<IExtractionJobSignal, ChannelExtractionJobSignal>();
builder.Services.AddScoped<IFileStorage>(_ =>
{
    var storageRoot = builder.Configuration["FileStorage:RootPath"]
        ?? Path.Combine(AppContext.BaseDirectory, "uploads");

    return new LocalFileStorage(new LocalFileStorageOptions(storageRoot));
});

var frontendOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173", "http://127.0.0.1:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("Frontend");

app.MapGet("/", () => "Hello World!");
Uploads.Map(app);
Extraction.Map(app);
Drafts.Map(app);
History.Map(app);

app.Run();

public partial class Program;
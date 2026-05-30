using Microsoft.EntityFrameworkCore;
using PoOcr.Application.Abstractions;
using PoOcr.Application.Extraction;
using PoOcr.Infrastructure.Ocr;
using PoOcr.Infrastructure.Parsing;
using PoOcr.Infrastructure.Persistence;
using PoOcr.Worker;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OcrDatabase");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'OcrDatabase' is required.");

builder.Services.AddDbContext<OcrDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<IUploadRepository, UploadRepository>();
builder.Services.AddScoped<IExtractionJobRepository, ExtractionJobRepository>();
builder.Services.AddScoped<IDraftRepository, DraftRepository>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
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
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

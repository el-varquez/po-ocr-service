namespace PoOcr.Infrastructure.Ocr;

public sealed record TesseractOptions(
    string ExecutablePath,
    string Language = "eng",
    int TimeoutSeconds = 60
);
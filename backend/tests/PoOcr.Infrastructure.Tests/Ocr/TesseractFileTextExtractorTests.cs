using PoOcr.Infrastructure.Ocr;

namespace PoOcr.Infrastructure.Tests.Ocr;

public sealed class TesseractFileTextExtractorTests
{
    [Fact]
    public async Task ExtractTextAsync_WhenFileDoesNotExist_ThrowsClearError()
    {
        var extractor = new TesseractFileTextExtractor(
            new TesseractOptions(
                @"C:\Program Files\Tesseract-OCR\tesseract.exe"));

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            extractor.ExtractTextAsync(
                "missing-file.png",
                "image/png",
                CancellationToken.None));

        Assert.Contains("OCR source file was not found", ex.Message);
    }

    [Fact]
    public async Task ExtractTextAsync_WhenContentTypeIsPdf_ThrowsClearError()
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            var extractor = new TesseractFileTextExtractor(
                new TesseractOptions(
                    @"C:\Program Files\Tesseract-OCR\tesseract.exe"));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                extractor.ExtractTextAsync(
                    tempFile,
                    "application/pdf",
                    CancellationToken.None));

            Assert.Contains("PDF OCR requires PDF-to-image conversion", ex.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_WhenImageIsInvalid_ReturnsTesseractFailure()
    {
        var tesseractPath = @"C:\Program Files\Tesseract-OCR\tesseract.exe";

        if (!File.Exists(tesseractPath))
            return;

        var imagePath = Path.Combine(
            Path.GetTempPath(),
            $"po-ocr-invalid-{Guid.NewGuid():N}.png");

        await File.WriteAllTextAsync(imagePath, "not a real png");

        try
        {
            var extractor = new TesseractFileTextExtractor(
                new TesseractOptions(tesseractPath));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                extractor.ExtractTextAsync(
                    imagePath,
                    "image/png",
                    CancellationToken.None));

            Assert.Contains("Tesseract OCR failed", ex.Message);
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    private static string CreateSimplePngWithTextPlaceholder()
    {
        // Tiny valid 1x1 PNG. This verifies process integration, not OCR accuracy.
        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

        var path = Path.Combine(
            Path.GetTempPath(),
            $"po-ocr-test-{Guid.NewGuid():N}.png");

        File.WriteAllBytes(path, pngBytes);

        return path;
    }
}
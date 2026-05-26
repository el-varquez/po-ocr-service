using System.Diagnostics;
using PoOcr.Application.Abstractions;

namespace PoOcr.Infrastructure.Ocr;

public sealed class TesseractFileTextExtractor(TesseractOptions options) : IFileTextExtractor
{
    private static readonly HashSet<string> SupportedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg"
    };

    public async Task<string> ExtractTextAsync(
        string storagePath,
        string contentType,
        CancellationToken cancellationToken)
    {
        Validate(storagePath, contentType);

        var outputBasePath = Path.Combine(
            Path.GetTempPath(),
            $"po-ocr-tesseract-{Guid.NewGuid():N}");

        var outputTextPath = outputBasePath + ".txt";

        try
        {
            await RunTesseractAsync(
                storagePath,
                outputBasePath,
                cancellationToken);

            if (!File.Exists(outputTextPath))
                throw new InvalidOperationException("Tesseract did not produce an output text file.");

            return await File.ReadAllTextAsync(outputTextPath, cancellationToken);
        }
        finally
        {
            if (File.Exists(outputTextPath))
                File.Delete(outputTextPath);
        }
    }

    private void Validate(string storagePath, string contentType)
    {
        if (string.IsNullOrWhiteSpace(options.ExecutablePath))
            throw new InvalidOperationException("Tesseract executable path is required");
        
        if (!File.Exists(options.ExecutablePath))
            throw new FileNotFoundException("Tesseract executable was not found", options.ExecutablePath);

        if (string.IsNullOrWhiteSpace(storagePath) || !File.Exists(storagePath))
            throw new FileNotFoundException("OCR source file was not found.", storagePath);

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PDF OCR requires PDF-to-image conversion");

        if (!SupportedImageContentTypes.Contains(contentType))
            throw new InvalidOperationException($"Unsupported OCR content type: {contentType}");
    }

    private async Task RunTesseractAsync(
        string inputPath,
        string outputBasePath,
        CancellationToken cancellationToken)
    {
        var arguments = $"\"{inputPath}\" \"{outputBasePath}\" -l {options.Language}";

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = options.ExecutablePath,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var processTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(options.TimeoutSeconds), cancellationToken);

        var completedTask = await Task.WhenAny(processTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process may have exited between timeout and kill.
            }

            throw new TimeoutException("Tesseract OCR timed out.");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(standardError)
                ? standardOutput
                : standardError;

            throw new InvalidOperationException($"Tesseract OCR failed: {message}");
        }
    }
}

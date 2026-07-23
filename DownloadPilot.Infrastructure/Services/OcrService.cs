using DownloadPilot.Core.Abstractions;
using DownloadPilot.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Tesseract;

namespace DownloadPilot.Infrastructure.Services;

public sealed class OcrService(
    ISettingsService settingsService,
    ILogger<OcrService> logger) : IOcrService
{
    private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".webp"];

    public async Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        if (!settings.OcrEnabled)
        {
            return null;
        }

        var extension = Path.GetExtension(filePath);
        if (!SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var tessDataPath = Path.Combine(SqlitePaths.DataDirectory, "tessdata");
        if (!Directory.Exists(tessDataPath))
        {
            logger.LogInformation("OCR overgeslagen: tessdata map niet gevonden op {Path}", tessDataPath);
            return null;
        }

        var language = ResolveLanguage(tessDataPath);
        if (language is null)
        {
            logger.LogInformation("OCR overgeslagen: geen taaldata (nld/eng) gevonden in {Path}", tessDataPath);
            return null;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
            using var image = Pix.LoadFromFile(filePath);
            using var page = engine.Process(image);
            var text = page.GetText();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OCR mislukt voor {Path}", filePath);
            return null;
        }
    }

    private static string? ResolveLanguage(string tessDataPath)
    {
        var hasNld = File.Exists(Path.Combine(tessDataPath, "nld.traineddata"));
        var hasEng = File.Exists(Path.Combine(tessDataPath, "eng.traineddata"));

        return (hasNld, hasEng) switch
        {
            (true, true) => "nld+eng",
            (true, false) => "nld",
            (false, true) => "eng",
            _ => null
        };
    }
}

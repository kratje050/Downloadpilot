using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Infrastructure.Services;

public sealed class FileAnalysisService : IFileAnalysisService
{
    private readonly IClassificationService _classificationService;
    private readonly IPdfTextExtractionService _pdfTextExtractionService;
    private readonly IOcrService _ocrService;
    private readonly ISettingsService? _settingsService;

    public FileAnalysisService(
        IClassificationService classificationService,
        IPdfTextExtractionService pdfTextExtractionService,
        IOcrService ocrService)
        : this(classificationService, pdfTextExtractionService, ocrService, null)
    {
    }

    public FileAnalysisService(
        IClassificationService classificationService,
        IPdfTextExtractionService pdfTextExtractionService,
        IOcrService ocrService,
        ISettingsService? settingsService)
    {
        _classificationService = classificationService;
        _pdfTextExtractionService = pdfTextExtractionService;
        _ocrService = ocrService;
        _settingsService = settingsService;
    }

    public async Task<FileAnalysisResult> AnalyzeAsync(string filePath, string sourceFolder, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        var extension = fileInfo.Extension.ToLowerInvariant();

        var extractedText = extension == ".pdf"
            ? await _pdfTextExtractionService.TryExtractTextAsync(filePath, cancellationToken)
            : IsImageExtension(extension)
                ? await _ocrService.TryExtractTextAsync(filePath, cancellationToken)
                : null;

        var insights = DocumentInsightsExtractor.Extract(extractedText);
        var imageInsights = IsImageExtension(extension)
            ? ImageInsightsExtractor.Extract(filePath, extractedText, insights)
            : new ImageInsights();

        var (category, confidence, reason) = _classificationService.Classify(filePath, sourceFolder, extractedText);

        var suggestedFileName = BuildSuggestedFileName(fileInfo, category.ToString(), insights);

        var destinationRoot = await GetDestinationRootAsync(cancellationToken);
        var destinationFolder = BuildSuggestedDestinationFolder(destinationRoot, category, insights, imageInsights);
        var enrichedReason = BuildReason(reason, insights, imageInsights, destinationFolder, destinationRoot, category.ToString());

        return new FileAnalysisResult
        {
            OriginalPath = filePath,
            OriginalFileName = fileInfo.Name,
            SourceFolder = sourceFolder,
            Extension = extension,
            FileSizeBytes = fileInfo.Length,
            CreatedLocal = fileInfo.CreationTime,
            SuggestedCategory = category,
            SuggestedDestinationFolder = destinationFolder,
            SuggestedFileName = suggestedFileName,
            Reason = enrichedReason,
            Confidence = confidence
        };
    }

    private async Task<string> GetDestinationRootAsync(CancellationToken cancellationToken)
    {
        if (_settingsService is null)
        {
            return DefaultDestinationRoot();
        }

        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(settings.DefaultDestinationRoot)
                ? DefaultDestinationRoot()
                : settings.DefaultDestinationRoot;
        }
        catch
        {
            return DefaultDestinationRoot();
        }
    }

    private static string BuildSuggestedFileName(FileInfo fileInfo, string fallbackCategory, DocumentInsights insights)
    {
        var date = (insights.DocumentDate ?? fileInfo.CreationTime).ToString("yyyy-MM-dd");
        var docType = FileNameSanitizer.Sanitize(insights.DocumentType ?? fallbackCategory);
        var company = FileNameSanitizer.Sanitize(insights.CompanyName ?? "Onbekend");
        var subject = FileNameSanitizer.Sanitize(insights.Subject ?? Path.GetFileNameWithoutExtension(fileInfo.Name));
        var amount = insights.Amount is null
            ? null
            : insights.Amount.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', '-');

        var parts = new List<string> { date, docType, company };
        if (!string.IsNullOrWhiteSpace(amount))
        {
            parts.Add(amount);
        }
        parts.Add(subject);

        var combined = string.Join("_", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return FileNameSanitizer.Sanitize(combined) + fileInfo.Extension.ToLowerInvariant();
    }

    private static string BuildSuggestedDestinationFolder(
        string destinationRoot,
        Core.Enums.FileCategory category,
        DocumentInsights insights,
        ImageInsights imageInsights)
    {
        var segments = new List<string>
        {
            destinationRoot,
            category.ToString()
        };

        var smartFolder = category == Core.Enums.FileCategory.Facturen
            ? insights.CompanyName
            : category == Core.Enums.FileCategory.Afbeeldingen
                ? imageInsights.SuggestedFolderName
                : null;

        if (!string.IsNullOrWhiteSpace(smartFolder))
        {
            segments.Add(FileNameSanitizer.Sanitize(smartFolder));
        }

        return Path.Combine([.. segments]);
    }

    private static string BuildReason(
        string baseReason,
        DocumentInsights insights,
        ImageInsights imageInsights,
        string destinationFolder,
        string destinationRoot,
        string categoryName)
    {
        var categoryFolder = Path.Combine(destinationRoot, categoryName);
        if (destinationFolder.Equals(categoryFolder, StringComparison.OrdinalIgnoreCase))
        {
            return baseReason;
        }

        var folderName = Path.GetFileName(destinationFolder);
        if (!string.IsNullOrWhiteSpace(insights.CompanyName))
        {
            return $"{baseReason}. Slimme map: {folderName} op basis van herkende bedrijfsnaam";
        }

        if (!string.IsNullOrWhiteSpace(imageInsights.Reason))
        {
            return $"{baseReason}. Slimme map: {folderName} ({imageInsights.Reason})";
        }

        return $"{baseReason}. Slimme map: {folderName}";
    }

    private static bool IsImageExtension(string extension)
    {
        return extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tif" or ".tiff" or ".webp" or ".heic" or ".heif";
    }

    private static string DefaultDestinationRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DownloadPilot");
    }
}

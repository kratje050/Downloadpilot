using DownloadPilot.Core.Abstractions;
using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Infrastructure.Services;

public sealed class DocumentRecognitionService(IPdfTextExtractionService pdf, IOcrService ocr) : IDocumentRecognitionService
{
    public async Task<string?> DetectDocumentTypeAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var text = await pdf.TryExtractTextAsync(filePath, cancellationToken)
            ?? await ocr.TryExtractTextAsync(filePath, cancellationToken)
            ?? string.Empty;

        return DocumentInsightsExtractor.DetectDocumentType(text);
    }
}

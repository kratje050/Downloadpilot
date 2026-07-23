using DownloadPilot.Core.Abstractions;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace DownloadPilot.Infrastructure.Services;

public sealed class PdfTextExtractionService(ILogger<PdfTextExtractionService> logger) : IPdfTextExtractionService
{
    public Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            using var pdf = PdfDocument.Open(filePath);
            var text = string.Join(Environment.NewLine, pdf.GetPages().Select(p => p.Text));
            return Task.FromResult<string?>(string.IsNullOrWhiteSpace(text) ? null : text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PDF tekstextractie mislukt voor {Path}", filePath);
            return Task.FromResult<string?>(null);
        }
    }
}

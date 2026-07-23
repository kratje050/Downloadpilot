namespace DownloadPilot.Core.Abstractions;

public interface IPdfTextExtractionService
{
    Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken);
}

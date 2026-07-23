namespace DownloadPilot.Core.Abstractions;

public interface IOcrService
{
    Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken);
}

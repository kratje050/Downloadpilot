namespace DownloadPilot.Core.Abstractions;

public interface IDocumentRecognitionService
{
    Task<string?> DetectDocumentTypeAsync(string filePath, CancellationToken cancellationToken);
}

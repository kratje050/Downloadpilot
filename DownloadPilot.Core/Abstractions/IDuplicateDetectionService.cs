namespace DownloadPilot.Core.Abstractions;

public interface IDuplicateDetectionService
{
    Task<bool> IsExactDuplicateAsync(string filePath, CancellationToken cancellationToken);
}

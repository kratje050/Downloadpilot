namespace DownloadPilot.Core.Abstractions;

public interface IFileStabilityService
{
    Task<bool> WaitUntilStableAsync(string filePath, CancellationToken cancellationToken);
}

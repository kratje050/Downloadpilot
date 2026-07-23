namespace DownloadPilot.Core.Abstractions;

public interface IFileHashService
{
    Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken);
}

namespace DownloadPilot.Core.Abstractions;

public interface IArchiveExtractionService
{
    Task<IReadOnlyList<string>> PreviewZipEntriesAsync(string zipPath, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ExtractZipSafelyAsync(string zipPath, string destinationFolder, CancellationToken cancellationToken);
}

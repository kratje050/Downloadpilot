using System.IO.Compression;
using DownloadPilot.Core.Abstractions;

namespace DownloadPilot.Infrastructure.Services;

public sealed class ArchiveExtractionService : IArchiveExtractionService
{
    public Task<IReadOnlyList<string>> PreviewZipEntriesAsync(string zipPath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .Select(e => e.FullName)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(entries);
    }

    public Task<IReadOnlyList<string>> ExtractZipSafelyAsync(string zipPath, string destinationFolder, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationFolder);
        var destinationFullPath = Path.GetFullPath(destinationFolder);

        var extracted = new List<string>();
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var targetPath = Path.GetFullPath(Path.Combine(destinationFullPath, entry.FullName));
            if (!targetPath.StartsWith(destinationFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Verdachte archive-entry geblokkeerd (path traversal)");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            entry.ExtractToFile(targetPath, overwrite: false);
            extracted.Add(targetPath);
        }

        return Task.FromResult<IReadOnlyList<string>>(extracted);
    }
}

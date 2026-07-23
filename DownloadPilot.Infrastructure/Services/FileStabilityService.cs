using DownloadPilot.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace DownloadPilot.Infrastructure.Services;

public sealed class FileStabilityService(ILogger<FileStabilityService> logger) : IFileStabilityService
{
    private static readonly string[] TemporaryExtensions = [".crdownload", ".part", ".tmp", ".download"];

    public async Task<bool> WaitUntilStableAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        if (TemporaryExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        long previousSize = -1;
        for (var attempt = 0; attempt < 15; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(filePath))
            {
                return false;
            }

            long currentSize;
            try
            {
                var info = new FileInfo(filePath);
                currentSize = info.Length;
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(500, cancellationToken);
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(500, cancellationToken);
                continue;
            }

            if (currentSize > 0 && currentSize == previousSize)
            {
                return true;
            }

            previousSize = currentSize;
            await Task.Delay(500, cancellationToken);
        }

        logger.LogWarning("Bestand niet stabiel binnen timeout: {Path}", filePath);
        return false;
    }
}

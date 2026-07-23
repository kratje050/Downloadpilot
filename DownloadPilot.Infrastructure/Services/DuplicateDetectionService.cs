using DownloadPilot.Core.Abstractions;

namespace DownloadPilot.Infrastructure.Services;

public sealed class DuplicateDetectionService(
    IFileHashService fileHashService,
    IHistoryService? historyService = null) : IDuplicateDetectionService
{
    private readonly HashSet<string> _seenHashes = new(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> IsExactDuplicateAsync(string filePath, CancellationToken cancellationToken)
    {
        var hash = await fileHashService.ComputeSha256Async(filePath, cancellationToken);

        if (historyService is not null &&
            await historyService.HasSuccessfulHashAsync(hash, filePath, cancellationToken))
        {
            lock (_seenHashes)
            {
                _seenHashes.Add(hash);
            }

            return true;
        }

        lock (_seenHashes)
        {
            if (_seenHashes.Contains(hash))
            {
                return true;
            }

            _seenHashes.Add(hash);
            return false;
        }
    }
}

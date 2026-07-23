using DownloadPilot.Core.Models;

namespace DownloadPilot.Core.Abstractions;

public interface IHistoryService
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task<long> AddEntryAsync(HistoryEntry entry, CancellationToken cancellationToken);

    Task MarkUndoneAsync(long id, CancellationToken cancellationToken);

    Task<HistoryEntry?> GetLastUndoableAsync(CancellationToken cancellationToken);

    Task<HistoryEntry?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(int count, CancellationToken cancellationToken);

    Task<bool> HasSuccessfulHashAsync(string sha256Hash, string? excludePath, CancellationToken cancellationToken);

    Task DeleteOlderThanAsync(DateTime cutoffLocal, CancellationToken cancellationToken);
}

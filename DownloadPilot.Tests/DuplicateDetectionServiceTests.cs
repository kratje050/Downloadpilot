using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace DownloadPilot.Tests;

public sealed class DuplicateDetectionServiceTests
{
    [Fact]
    public async Task IsExactDuplicateAsync_ShouldUseSuccessfulHashesFromHistory()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var original = Path.Combine(root, "origineel.txt");
        var duplicate = Path.Combine(root, "kopie.txt");
        await File.WriteAllTextAsync(original, "zelfde inhoud");
        await File.WriteAllTextAsync(duplicate, "zelfde inhoud");

        try
        {
            var factory = new SqliteConnectionFactory(Path.Combine(root, "downloadpilot.db"));
            IHistoryService history = new HistoryService(factory);
            await history.InitializeAsync(CancellationToken.None);

            IFileHashService hashService = new FileHashService();
            var hash = await hashService.ComputeSha256Async(original, CancellationToken.None);

            await history.AddEntryAsync(new HistoryEntry
            {
                TimestampLocal = DateTime.Now,
                OriginalPath = original,
                NewPath = original,
                OriginalName = Path.GetFileName(original),
                NewName = Path.GetFileName(original),
                Sha256Hash = hash,
                ActionType = HistoryActionType.Verplaats,
                Status = HistoryStatus.Geslaagd,
                CanUndo = false
            }, CancellationToken.None);

            var service = new DuplicateDetectionService(hashService, history);

            var result = await service.IsExactDuplicateAsync(duplicate, CancellationToken.None);

            Assert.True(result);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}

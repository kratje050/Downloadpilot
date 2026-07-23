using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace DownloadPilot.Tests;

public sealed class FolderWatchServiceRestartTests
{
    [Fact]
    public async Task StartAsync_ShouldRestartAfterStop()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var watchedFolder = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(watchedFolder);

        try
        {
            var factory = new SqliteConnectionFactory(Path.Combine(root, "downloadpilot.db"));
            IHistoryService history = new HistoryService(factory);
            await history.InitializeAsync(CancellationToken.None);

            ISettingsService settings = new SettingsService(factory);
            await settings.SaveAsync(new AppSettings
            {
                WatchedFolders =
                [
                    new WatchedFolder { Path = watchedFolder, IsEnabled = true }
                ],
                AutomaticBackupsEnabled = false
            }, CancellationToken.None);

            var service = new FolderWatchService(
                settings,
                new FileStabilityService(NullLogger<FileStabilityService>.Instance),
                NullLogger<FolderWatchService>.Instance);

            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
            await service.DisposeAsync();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}

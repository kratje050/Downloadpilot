using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace DownloadPilot.Tests;

public sealed class SettingsServiceEncryptionTests
{
    [Fact]
    public async Task SaveAsync_ShouldStoreDpapiProtectedPayload()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var dbPath = Path.Combine(root, "secure.db");

        try
        {
            var factory = new SqliteConnectionFactory(dbPath);
            IHistoryService history = new HistoryService(factory);
            await history.InitializeAsync(CancellationToken.None);

            ISettingsService settings = new SettingsService(factory);
            await settings.SaveAsync(new AppSettings
            {
                WatchedFolders =
                [
                    new WatchedFolder { Path = "C:\\Users\\test\\Downloads", IsEnabled = true }
                ],
                DefaultDestinationRoot = "C:\\Users\\test\\Documents",
                StartWithWindows = false,
                NotificationsEnabled = true,
                MinAutoApplyConfidence = 90,
                StoreDocumentText = false,
                OcrEnabled = false,
                HashCheckEnabled = true
            }, CancellationToken.None);

            await using var connection = factory.CreateOpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE [Key] = 'app-settings' LIMIT 1;";
            var raw = await command.ExecuteScalarAsync() as string;

            Assert.NotNull(raw);
            Assert.StartsWith("dpapi:", raw, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}

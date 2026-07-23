using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace DownloadPilot.Tests;

public sealed class SettingsServiceAppSettingsPersistenceTests
{
    [Fact]
    public async Task SaveAndLoadAsync_ShouldPersistAppSettingsValues()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var dbPath = Path.Combine(root, "settings.db");

        try
        {
            var factory = new SqliteConnectionFactory(dbPath);
            IHistoryService history = new HistoryService(factory);
            await history.InitializeAsync(CancellationToken.None);

            ISettingsService settings = new SettingsService(factory);

            var toSave = new AppSettings
            {
                WatchedFolders =
                [
                    new WatchedFolder { Path = "C:\\Users\\test\\Downloads", IsEnabled = true }
                ],
                ProtectedPaths = ["C:\\NoTouch"],
                IgnoredPaths = ["C:\\IgnoreMe"],
                ExtraScanPaths = ["D:\\Audit"],
                DefaultDestinationRoot = "C:\\Users\\test\\Documents",
                HasCompletedOnboarding = true,
                StartWithWindows = true,
                NotificationsEnabled = false,
                UpdateChecksEnabled = false,
                AutoDownloadUpdates = true,
                OrganizationProfile = "Alleen advies",
                CleanupSchedule = "Maandelijks",
                PermissionNoticeAccepted = true,
                MinAutoApplyConfidence = 93,
                StoreDocumentText = true,
                OcrEnabled = true,
                HashCheckEnabled = false
            };

            await settings.SaveAsync(toSave, CancellationToken.None);
            var loaded = await settings.LoadAsync(CancellationToken.None);

            Assert.Equal(93, loaded.MinAutoApplyConfidence);
            Assert.True(loaded.HasCompletedOnboarding);
            Assert.False(loaded.NotificationsEnabled);
            Assert.False(loaded.UpdateChecksEnabled);
            Assert.True(loaded.AutoDownloadUpdates);
            Assert.Equal("Alleen advies", loaded.OrganizationProfile);
            Assert.Equal("Maandelijks", loaded.CleanupSchedule);
            Assert.True(loaded.PermissionNoticeAccepted);
            Assert.True(loaded.StoreDocumentText);
            Assert.True(loaded.StartWithWindows);
            Assert.True(loaded.OcrEnabled);
            Assert.False(loaded.HashCheckEnabled);
            Assert.Single(loaded.WatchedFolders);
            Assert.Equal("C:\\Users\\test\\Downloads", loaded.WatchedFolders[0].Path);
            Assert.Equal("C:\\NoTouch", Assert.Single(loaded.ProtectedPaths));
            Assert.Equal("C:\\IgnoreMe", Assert.Single(loaded.IgnoredPaths));
            Assert.Equal("D:\\Audit", Assert.Single(loaded.ExtraScanPaths));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}

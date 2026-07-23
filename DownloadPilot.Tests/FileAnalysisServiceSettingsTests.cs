using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace DownloadPilot.Tests;

public sealed class FileAnalysisServiceSettingsTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldUseConfiguredDefaultDestinationRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var targetRoot = Path.Combine(root, "Doel");
        Directory.CreateDirectory(root);

        var filePath = Path.Combine(root, "foto.jpg");
        await File.WriteAllTextAsync(filePath, "dummy");

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
                    new WatchedFolder { Path = root, IsEnabled = true }
                ],
                DefaultDestinationRoot = targetRoot,
                AutomaticBackupsEnabled = false
            }, CancellationToken.None);

            var service = new FileAnalysisService(
                new ClassificationService(),
                new EmptyPdfTextExtractionService(),
                new EmptyOcrService(),
                settings);

            var result = await service.AnalyzeAsync(filePath, root, CancellationToken.None);

            Assert.Equal(FileCategory.Afbeeldingen, result.SuggestedCategory);
            Assert.Equal(Path.Combine(targetRoot, "Afbeeldingen"), result.SuggestedDestinationFolder);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class EmptyPdfTextExtractionService : IPdfTextExtractionService
    {
        public Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }

    private sealed class EmptyOcrService : IOcrService
    {
        public Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }
}

using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DownloadPilot.Tests;

public sealed class OcrServiceTests
{
    [Fact]
    public async Task TryExtractTextAsync_ShouldReturnNullWhenOcrDisabled()
    {
        var service = new OcrService(new FakeSettingsService(ocrEnabled: false), NullLogger<OcrService>.Instance);

        var result = await service.TryExtractTextAsync("scan.jpg", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryExtractTextAsync_ShouldReturnNullWhenExtensionNotSupported()
    {
        var service = new OcrService(new FakeSettingsService(ocrEnabled: true), NullLogger<OcrService>.Instance);

        var result = await service.TryExtractTextAsync("document.pdf", CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class FakeSettingsService(bool ocrEnabled) : ISettingsService
    {
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new AppSettings
            {
                WatchedFolders =
                [
                    new WatchedFolder { Path = "C:\\Users\\test\\Downloads", IsEnabled = true }
                ],
                DefaultDestinationRoot = "C:\\Users\\test\\Documents",
                StartWithWindows = false,
                NotificationsEnabled = true,
                MinAutoApplyConfidence = 85,
                StoreDocumentText = false,
                OcrEnabled = ocrEnabled,
                HashCheckEnabled = true
            });
        }

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<RuleDefinition>> LoadRulesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<RuleDefinition>>([]);

        public Task<int> UpsertRuleAsync(RuleDefinition rule, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task DeleteRuleAsync(int ruleId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}

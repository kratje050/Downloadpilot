using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace DownloadPilot.Tests;

public sealed class SettingsServiceRulePersistenceTests
{
    [Fact]
    public async Task UpsertAndDeleteRuleAsync_ShouldPersistAndRemoveRule()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var dbPath = Path.Combine(root, "rules.db");

        try
        {
            var factory = new SqliteConnectionFactory(dbPath);
            IHistoryService history = new HistoryService(factory);
            await history.InitializeAsync(CancellationToken.None);

            ISettingsService settings = new SettingsService(factory);

            var id = await settings.UpsertRuleAsync(new RuleDefinition
            {
                Id = 0,
                Name = "Pdf Facturen",
                ExtensionEquals = ".pdf",
                FileNameContains = "factuur",
                SourceFolderContains = null,
                AutoApply = false,
                Priority = 91,
                Category = FileCategory.Facturen,
                DestinationFolder = "C:\\Facturen",
                RenameTemplate = "{datum}_Factuur_{origineel}"
            }, CancellationToken.None);

            var rules = await settings.LoadRulesAsync(CancellationToken.None);
            Assert.Contains(rules, r => r.Id == id && r.Name == "Pdf Facturen");

            await settings.UpsertRuleAsync(new RuleDefinition
            {
                Id = id,
                Name = "Pdf Facturen Auto",
                ExtensionEquals = ".pdf",
                FileNameContains = "factuur",
                SourceFolderContains = null,
                AutoApply = true,
                Priority = 95,
                Category = FileCategory.Facturen,
                DestinationFolder = "C:\\Facturen\\Auto",
                RenameTemplate = "{datum}_Factuur_Auto_{origineel}"
            }, CancellationToken.None);

            var updated = await settings.LoadRulesAsync(CancellationToken.None);
            Assert.Contains(updated, r => r.Id == id && r.Name == "Pdf Facturen Auto" && r.AutoApply);

            await settings.DeleteRuleAsync(id, CancellationToken.None);

            var afterDelete = await settings.LoadRulesAsync(CancellationToken.None);
            Assert.DoesNotContain(afterDelete, r => r.Id == id);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}

using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;

namespace DownloadPilot.Infrastructure.Services;

public sealed class SettingsService(SqliteConnectionFactory connectionFactory) : ISettingsService
{
    private const string AppSettingsKey = "app-settings";

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Settings WHERE [Key] = $key LIMIT 1;";
        command.Parameters.AddWithValue("$key", AppSettingsKey);

        var serialized = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (!string.IsNullOrWhiteSpace(serialized))
        {
            var payload = TryDecrypt(serialized) ?? serialized;
            var loaded = JsonSerializer.Deserialize<AppSettings>(payload);
            if (loaded is not null)
            {
                return loaded;
            }
        }

        var defaultSettings = BuildDefaultSettings();
        await SaveAsync(defaultSettings, cancellationToken);
        return defaultSettings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        if (settings.AutomaticBackupsEnabled)
        {
            TryCreateDatabaseBackup();
        }

        var serialized = JsonSerializer.Serialize(settings);
        var protectedPayload = Protect(serialized);

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Settings ([Key], [Value]) VALUES ($key, $value)
ON CONFLICT([Key]) DO UPDATE SET [Value] = excluded.[Value];";
        command.Parameters.AddWithValue("$key", AppSettingsKey);
        command.Parameters.AddWithValue("$value", protectedPayload);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RuleDefinition>> LoadRulesAsync(CancellationToken cancellationToken)
    {
        var rules = new List<RuleDefinition>();

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT
Id, Name, ExtensionEquals, FileNameContains, SourceFolderContains,
AutoApply, Priority, Category, DestinationFolder, RenameTemplate
FROM Rules ORDER BY Priority DESC, Id ASC;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rules.Add(new RuleDefinition
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ExtensionEquals = reader.IsDBNull(2) ? null : reader.GetString(2),
                FileNameContains = reader.IsDBNull(3) ? null : reader.GetString(3),
                SourceFolderContains = reader.IsDBNull(4) ? null : reader.GetString(4),
                AutoApply = reader.GetInt32(5) == 1,
                Priority = reader.GetInt32(6),
                Category = (Core.Enums.FileCategory)reader.GetInt32(7),
                DestinationFolder = reader.IsDBNull(8) ? null : reader.GetString(8),
                RenameTemplate = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        if (rules.Count == 0)
        {
            await SeedDefaultRulesAsync(cancellationToken);
            return await LoadRulesAsync(cancellationToken);
        }

        return rules;
    }

    public async Task<int> UpsertRuleAsync(RuleDefinition rule, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();

        if (rule.Id <= 0)
        {
            command.CommandText = @"
INSERT INTO Rules (Name, ExtensionEquals, FileNameContains, SourceFolderContains, AutoApply, Priority, Category, DestinationFolder, RenameTemplate)
VALUES ($name, $extensionEquals, $fileNameContains, $sourceFolderContains, $autoApply, $priority, $category, $destinationFolder, $renameTemplate);
SELECT last_insert_rowid();";
        }
        else
        {
            command.CommandText = @"
UPDATE Rules
SET Name = $name,
    ExtensionEquals = $extensionEquals,
    FileNameContains = $fileNameContains,
    SourceFolderContains = $sourceFolderContains,
    AutoApply = $autoApply,
    Priority = $priority,
    Category = $category,
    DestinationFolder = $destinationFolder,
    RenameTemplate = $renameTemplate
WHERE Id = $id;
SELECT $id;";
            command.Parameters.AddWithValue("$id", rule.Id);
        }

        command.Parameters.AddWithValue("$name", rule.Name);
        command.Parameters.AddWithValue("$extensionEquals", (object?)rule.ExtensionEquals ?? DBNull.Value);
        command.Parameters.AddWithValue("$fileNameContains", (object?)rule.FileNameContains ?? DBNull.Value);
        command.Parameters.AddWithValue("$sourceFolderContains", (object?)rule.SourceFolderContains ?? DBNull.Value);
        command.Parameters.AddWithValue("$autoApply", rule.AutoApply ? 1 : 0);
        command.Parameters.AddWithValue("$priority", rule.Priority);
        command.Parameters.AddWithValue("$category", (int)rule.Category);
        command.Parameters.AddWithValue("$destinationFolder", (object?)rule.DestinationFolder ?? DBNull.Value);
        command.Parameters.AddWithValue("$renameTemplate", (object?)rule.RenameTemplate ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task DeleteRuleAsync(int ruleId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Rules WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", ruleId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task SeedDefaultRulesAsync(CancellationToken cancellationToken)
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Rules (Name, ExtensionEquals, FileNameContains, SourceFolderContains, AutoApply, Priority, Category, DestinationFolder, RenameTemplate)
VALUES
('Installatiebestanden', '.exe', NULL, NULL, 0, 90, 6, $softwareFolder, '{datum}_Installatie_{origineel}'),
('Facturen PDF', '.pdf', 'factuur', NULL, 0, 95, 4, NULL, '{datum}_Factuur_{origineel}');";
        command.Parameters.AddWithValue("$softwareFolder", Path.Combine(docs, "Software", "Installatiebestanden"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static AppSettings BuildDefaultSettings()
    {
        var downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        return new AppSettings
        {
            WatchedFolders =
            [
                new WatchedFolder
                {
                    Path = downloads,
                    IsEnabled = true
                }
            ],
            DefaultDestinationRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "DownloadPilot")
        };
    }

    private void TryCreateDatabaseBackup()
    {
        var databasePath = connectionFactory.DatabasePath;
        if (!File.Exists(databasePath))
        {
            return;
        }

        try
        {
            var backupFolder = Path.Combine(Path.GetDirectoryName(databasePath)!, "backups");
            Directory.CreateDirectory(backupFolder);

            var backupPath = Path.Combine(
                backupFolder,
                $"downloadpilot-{DateTime.Now:yyyyMMdd-HHmmss}.db");

            File.Copy(databasePath, backupPath, overwrite: false);

            var oldBackups = Directory
                .EnumerateFiles(backupFolder, "downloadpilot-*.db")
                .OrderByDescending(File.GetCreationTimeUtc)
                .Skip(10);

            foreach (var oldBackup in oldBackups)
            {
                File.Delete(oldBackup);
            }
        }
        catch
        {
            // Backups mogen instellingen opslaan nooit blokkeren.
        }
    }

    private static string Protect(string value)
    {
        if (!OperatingSystem.IsWindows())
        {
            return value;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return "dpapi:" + Convert.ToBase64String(protectedBytes);
    }

    private static string? TryDecrypt(string value)
    {
        if (!value.StartsWith("dpapi:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            var payload = value["dpapi:".Length..];
            var protectedBytes = Convert.FromBase64String(payload);
            var plainBytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return null;
        }
    }
}

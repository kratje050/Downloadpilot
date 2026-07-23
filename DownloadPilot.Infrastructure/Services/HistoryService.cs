using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace DownloadPilot.Infrastructure.Services;

public sealed class HistoryService(SqliteConnectionFactory connectionFactory) : IHistoryService
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        var sql = @"
CREATE TABLE IF NOT EXISTS History (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TimestampLocal TEXT NOT NULL,
    OriginalPath TEXT NOT NULL,
    NewPath TEXT NOT NULL,
    OriginalName TEXT NOT NULL,
    NewName TEXT NOT NULL,
    RuleName TEXT NULL,
    Sha256Hash TEXT NULL,
    ActionType INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    ErrorMessage TEXT NULL,
    IsAutoApplied INTEGER NOT NULL,
    CanUndo INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS Settings (
    [Key] TEXT PRIMARY KEY,
    [Value] TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Rules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    ExtensionEquals TEXT NULL,
    FileNameContains TEXT NULL,
    SourceFolderContains TEXT NULL,
    AutoApply INTEGER NOT NULL,
    Priority INTEGER NOT NULL,
    Category INTEGER NOT NULL,
    DestinationFolder TEXT NULL,
    RenameTemplate TEXT NULL
);
";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long> AddEntryAsync(HistoryEntry entry, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO History (
    TimestampLocal, OriginalPath, NewPath, OriginalName, NewName,
    RuleName, Sha256Hash, ActionType, Status, ErrorMessage,
    IsAutoApplied, CanUndo
)
VALUES (
    $timestampLocal, $originalPath, $newPath, $originalName, $newName,
    $ruleName, $sha256Hash, $actionType, $status, $errorMessage,
    $isAutoApplied, $canUndo
);
SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("$timestampLocal", entry.TimestampLocal.ToString("O"));
        command.Parameters.AddWithValue("$originalPath", entry.OriginalPath);
        command.Parameters.AddWithValue("$newPath", entry.NewPath);
        command.Parameters.AddWithValue("$originalName", entry.OriginalName);
        command.Parameters.AddWithValue("$newName", entry.NewName);
        command.Parameters.AddWithValue("$ruleName", (object?)entry.RuleName ?? DBNull.Value);
        command.Parameters.AddWithValue("$sha256Hash", (object?)entry.Sha256Hash ?? DBNull.Value);
        command.Parameters.AddWithValue("$actionType", (int)entry.ActionType);
        command.Parameters.AddWithValue("$status", (int)entry.Status);
        command.Parameters.AddWithValue("$errorMessage", (object?)entry.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("$isAutoApplied", entry.IsAutoApplied ? 1 : 0);
        command.Parameters.AddWithValue("$canUndo", entry.CanUndo ? 1 : 0);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (long)(result ?? 0L);
    }

    public async Task MarkUndoneAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE History SET Status = $status, CanUndo = 0 WHERE Id = $id;";
        command.Parameters.AddWithValue("$status", (int)HistoryStatus.Teruggedraaid);
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<HistoryEntry?> GetLastUndoableAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM History WHERE CanUndo = 1 AND Status = $status ORDER BY Id DESC LIMIT 1;";
        command.Parameters.AddWithValue("$status", (int)HistoryStatus.Geslaagd);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<HistoryEntry?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM History WHERE Id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(int count, CancellationToken cancellationToken)
    {
        var result = new List<HistoryEntry>();

        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM History ORDER BY Id DESC LIMIT $count;";
        command.Parameters.AddWithValue("$count", count);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task<bool> HasSuccessfulHashAsync(string sha256Hash, string? excludePath, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT 1
FROM History
WHERE Sha256Hash = $sha256Hash
  AND Status = $status
  AND ($excludePath IS NULL OR (OriginalPath <> $excludePath AND NewPath <> $excludePath))
LIMIT 1;";
        command.Parameters.AddWithValue("$sha256Hash", sha256Hash);
        command.Parameters.AddWithValue("$status", (int)HistoryStatus.Geslaagd);
        command.Parameters.AddWithValue("$excludePath", (object?)excludePath ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task DeleteOlderThanAsync(DateTime cutoffLocal, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM History WHERE TimestampLocal < $cutoff AND CanUndo = 0;";
        command.Parameters.AddWithValue("$cutoff", cutoffLocal.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static HistoryEntry Map(SqliteDataReader reader)
    {
        return new HistoryEntry
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            TimestampLocal = DateTime.Parse(reader.GetString(reader.GetOrdinal("TimestampLocal"))),
            OriginalPath = reader.GetString(reader.GetOrdinal("OriginalPath")),
            NewPath = reader.GetString(reader.GetOrdinal("NewPath")),
            OriginalName = reader.GetString(reader.GetOrdinal("OriginalName")),
            NewName = reader.GetString(reader.GetOrdinal("NewName")),
            RuleName = reader.IsDBNull(reader.GetOrdinal("RuleName")) ? null : reader.GetString(reader.GetOrdinal("RuleName")),
            Sha256Hash = reader.IsDBNull(reader.GetOrdinal("Sha256Hash")) ? null : reader.GetString(reader.GetOrdinal("Sha256Hash")),
            ActionType = (HistoryActionType)reader.GetInt32(reader.GetOrdinal("ActionType")),
            Status = (HistoryStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage")),
            IsAutoApplied = reader.GetInt32(reader.GetOrdinal("IsAutoApplied")) == 1,
            CanUndo = reader.GetInt32(reader.GetOrdinal("CanUndo")) == 1
        };
    }
}

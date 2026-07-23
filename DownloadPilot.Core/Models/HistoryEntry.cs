using DownloadPilot.Core.Enums;

namespace DownloadPilot.Core.Models;

public sealed class HistoryEntry
{
    public long Id { get; init; }

    public DateTime TimestampLocal { get; init; }

    public required string OriginalPath { get; init; }

    public required string NewPath { get; init; }

    public required string OriginalName { get; init; }

    public required string NewName { get; init; }

    public string? RuleName { get; init; }

    public string? Sha256Hash { get; init; }

    public HistoryActionType ActionType { get; init; }

    public HistoryStatus Status { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsAutoApplied { get; init; }

    public bool CanUndo { get; init; }
}

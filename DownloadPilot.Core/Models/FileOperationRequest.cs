namespace DownloadPilot.Core.Models;

public sealed class FileOperationRequest
{
    public required FileAnalysisResult Analysis { get; init; }

    public required string TargetFolder { get; init; }

    public required string TargetFileName { get; init; }

    public string? AppliedRuleName { get; init; }

    public bool IsAutoApplied { get; init; }
}

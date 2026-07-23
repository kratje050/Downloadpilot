using DownloadPilot.Core.Enums;

namespace DownloadPilot.Core.Models;

public sealed class FileAnalysisResult
{
    public required string OriginalPath { get; init; }

    public required string OriginalFileName { get; init; }

    public required string SourceFolder { get; init; }

    public required string Extension { get; init; }

    public long FileSizeBytes { get; init; }

    public DateTime CreatedLocal { get; init; }

    public FileCategory SuggestedCategory { get; init; }

    public required string SuggestedDestinationFolder { get; init; }

    public required string SuggestedFileName { get; init; }

    public required string Reason { get; init; }

    public int Confidence { get; init; }
}

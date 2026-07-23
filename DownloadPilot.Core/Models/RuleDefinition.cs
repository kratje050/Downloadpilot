using DownloadPilot.Core.Enums;

namespace DownloadPilot.Core.Models;

public sealed class RuleDefinition
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public string? ExtensionEquals { get; init; }

    public string? FileNameContains { get; init; }

    public string? SourceFolderContains { get; init; }

    public bool AutoApply { get; init; }

    public int Priority { get; init; }

    public FileCategory Category { get; init; } = FileCategory.Overig;

    public string? DestinationFolder { get; init; }

    public string? RenameTemplate { get; init; }
}

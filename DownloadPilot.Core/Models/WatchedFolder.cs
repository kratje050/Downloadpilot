namespace DownloadPilot.Core.Models;

public sealed class WatchedFolder
{
    public required string Path { get; init; }

    public bool IsEnabled { get; init; } = true;
}

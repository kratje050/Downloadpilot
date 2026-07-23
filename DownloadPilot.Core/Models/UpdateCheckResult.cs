namespace DownloadPilot.Core.Models;

public sealed class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; init; }

    public required string CurrentVersion { get; init; }

    public string? LatestVersion { get; init; }

    public string? ReleaseUrl { get; init; }

    public string? DownloadUrl { get; init; }

    public string? AssetName { get; init; }

    public string? Message { get; init; }
}

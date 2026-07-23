namespace DownloadPilot.Infrastructure.Utilities;

public sealed class GameResidueScanOptions
{
    public IReadOnlyList<string>? SteamRoots { get; init; }

    public IReadOnlyList<string>? EpicManifestRoots { get; init; }

    public IReadOnlyList<string>? InstalledGameRoots { get; init; }

    public IReadOnlyList<string>? ScanRoots { get; init; }

    public int MaxCandidates { get; init; } = 250;
}

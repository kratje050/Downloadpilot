namespace DownloadPilot.Infrastructure.Utilities;

public sealed class SteamResidueScanOptions
{
    public IReadOnlyList<string>? SteamRoots { get; init; }

    public IReadOnlyList<string>? AppDataRoots { get; init; }

    public int MaxCandidates { get; init; } = 200;
}

namespace DownloadPilot.Infrastructure.Utilities;

public sealed class SteamResidueCandidate
{
    public required string Path { get; init; }

    public required string Name { get; init; }

    public required string RootName { get; init; }

    public long SizeBytes { get; init; }

    public DateTime LastWriteLocal { get; init; }

    public int Confidence { get; init; }

    public required string Reason { get; init; }
}

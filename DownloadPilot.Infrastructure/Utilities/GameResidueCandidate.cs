namespace DownloadPilot.Infrastructure.Utilities;

public sealed class GameResidueCandidate
{
    public required string Path { get; init; }

    public required string Name { get; init; }

    public required string RootName { get; init; }

    public required string Source { get; init; }

    public long SizeBytes { get; init; }

    public DateTime LastWriteLocal { get; init; }

    public int Confidence { get; init; }

    public required string Reason { get; init; }
}

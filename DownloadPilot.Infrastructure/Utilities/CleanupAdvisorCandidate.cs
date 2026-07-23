namespace DownloadPilot.Infrastructure.Utilities;

public sealed class CleanupAdvisorCandidate
{
    public required string Title { get; init; }

    public required string Detail { get; init; }

    public required string Metric { get; init; }

    public required string Category { get; init; }

    public long SizeBytes { get; init; }

    public string? Path { get; init; }

    public string? Action { get; init; }

    public int SafetyScore { get; init; }

    public string SafetyLabel => SafetyScore >= 80
        ? "Veilig"
        : SafetyScore >= 55
            ? "Twijfel"
            : "Niet aanraden";
}

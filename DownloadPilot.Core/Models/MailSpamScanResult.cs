namespace DownloadPilot.Core.Models;

public sealed class MailSpamScanResult
{
    public int ScannedCount { get; init; }

    public required IReadOnlyList<MailSpamCandidate> Candidates { get; init; }

    public int CandidateCount => Candidates.Count;

    public int HighConfidenceCount => Candidates.Count(candidate => candidate.SpamScore >= 80);
}

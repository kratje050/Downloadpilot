namespace DownloadPilot.Infrastructure.Utilities;

public static class SteamResidueScanner
{
    public static IReadOnlyList<SteamResidueCandidate> Scan(SteamResidueScanOptions? options = null)
    {
        options ??= new SteamResidueScanOptions();
        var candidates = GameResidueScanner.Scan(new GameResidueScanOptions
        {
            SteamRoots = options.SteamRoots,
            ScanRoots = options.AppDataRoots,
            MaxCandidates = options.MaxCandidates
        });

        return candidates
            .Select(candidate => new SteamResidueCandidate
            {
                Path = candidate.Path,
                Name = candidate.Name,
                RootName = candidate.RootName,
                SizeBytes = candidate.SizeBytes,
                LastWriteLocal = candidate.LastWriteLocal,
                Confidence = candidate.Confidence,
                Reason = candidate.Reason
            })
            .ToList();
    }
}

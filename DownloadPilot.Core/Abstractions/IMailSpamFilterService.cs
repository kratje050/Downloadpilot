using DownloadPilot.Core.Models;

namespace DownloadPilot.Core.Abstractions;

public interface IMailSpamFilterService
{
    Task<MailSpamScanResult> ScanAsync(MailConnectionSettings settings, CancellationToken cancellationToken);

    Task<int> MoveToSpamAsync(
        MailConnectionSettings settings,
        IReadOnlyList<MailSpamCandidate> candidates,
        CancellationToken cancellationToken);
}

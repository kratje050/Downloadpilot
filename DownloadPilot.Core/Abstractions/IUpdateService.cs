using DownloadPilot.Core.Models;

namespace DownloadPilot.Core.Abstractions;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckLatestAsync(CancellationToken cancellationToken);

    Task<string?> DownloadUpdateAsync(UpdateCheckResult update, CancellationToken cancellationToken);
}

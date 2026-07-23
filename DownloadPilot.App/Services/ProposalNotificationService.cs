using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using Microsoft.Extensions.Logging;

namespace DownloadPilot.App.Services;

public sealed class ProposalNotificationService(ILogger<ProposalNotificationService> logger) : INotificationService, IDisposable
{
    private readonly object _sync = new();
    private System.Threading.Timer? _timer;
    private int _pendingProposalCount;

    public event EventHandler<int>? ProposalBatchReady;

    public void ShowProposal(FileAnalysisResult analysis)
    {
        logger.LogInformation("Nieuw voorstel: {Name} -> {Category}", analysis.OriginalFileName, analysis.SuggestedCategory);

        lock (_sync)
        {
            _pendingProposalCount++;
            _timer ??= new System.Threading.Timer(Flush, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timer.Change(TimeSpan.FromMilliseconds(900), Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void Flush(object? state)
    {
        int count;
        lock (_sync)
        {
            count = _pendingProposalCount;
            _pendingProposalCount = 0;
        }

        if (count > 0)
        {
            ProposalBatchReady?.Invoke(this, count);
        }
    }
}

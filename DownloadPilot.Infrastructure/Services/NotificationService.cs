using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using Microsoft.Extensions.Logging;

namespace DownloadPilot.Infrastructure.Services;

public sealed class NotificationService(ILogger<NotificationService> logger) : INotificationService
{
    public void ShowProposal(FileAnalysisResult analysis)
    {
        logger.LogInformation("Nieuw voorstel: {Name} -> {Category}", analysis.OriginalFileName, analysis.SuggestedCategory);
    }
}

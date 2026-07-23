using DownloadPilot.Core.Models;

namespace DownloadPilot.Core.Abstractions;

public interface INotificationService
{
    void ShowProposal(FileAnalysisResult analysis);
}

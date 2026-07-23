using DownloadPilot.Core.Models;

namespace DownloadPilot.Core.Abstractions;

public interface IFileOperationService
{
    Task<HistoryEntry> MoveAndRenameAsync(FileOperationRequest request, CancellationToken cancellationToken);
}

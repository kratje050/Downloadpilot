using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;

namespace DownloadPilot.Infrastructure.Services;

public sealed class UndoService(IHistoryService historyService) : IUndoService
{
    public async Task<bool> UndoLastAsync(CancellationToken cancellationToken)
    {
        var last = await historyService.GetLastUndoableAsync(cancellationToken);
        return last is not null && await UndoEntryAsync(last, cancellationToken);
    }

    public async Task<bool> UndoAsync(long historyEntryId, CancellationToken cancellationToken)
    {
        var entry = await historyService.GetByIdAsync(historyEntryId, cancellationToken);
        return entry is not null && await UndoEntryAsync(entry, cancellationToken);
    }

    private async Task<bool> UndoEntryAsync(HistoryEntry entry, CancellationToken cancellationToken)
    {
        if (!entry.CanUndo || entry.Status != HistoryStatus.Geslaagd)
        {
            return false;
        }

        if (!File.Exists(entry.NewPath))
        {
            await historyService.MarkUndoneAsync(entry.Id, cancellationToken);
            return false;
        }

        var originalFolder = Path.GetDirectoryName(entry.OriginalPath);
        if (string.IsNullOrWhiteSpace(originalFolder))
        {
            return false;
        }

        Directory.CreateDirectory(originalFolder);
        var restoreTarget = entry.OriginalPath;
        if (File.Exists(restoreTarget))
        {
            var extension = Path.GetExtension(entry.OriginalPath);
            var fileBase = Path.GetFileNameWithoutExtension(entry.OriginalPath);
            restoreTarget = Path.Combine(originalFolder, $"{fileBase}_undo{extension}");
        }

        File.Move(entry.NewPath, restoreTarget, overwrite: false);
        await historyService.MarkUndoneAsync(entry.Id, cancellationToken);
        return true;
    }
}

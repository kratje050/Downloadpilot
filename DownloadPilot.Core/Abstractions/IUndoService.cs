namespace DownloadPilot.Core.Abstractions;

public interface IUndoService
{
    Task<bool> UndoLastAsync(CancellationToken cancellationToken);

    Task<bool> UndoAsync(long historyEntryId, CancellationToken cancellationToken);
}

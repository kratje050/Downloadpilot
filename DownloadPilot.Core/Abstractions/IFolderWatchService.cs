namespace DownloadPilot.Core.Abstractions;

public interface IFolderWatchService : IAsyncDisposable
{
    event EventHandler<string>? FileReady;

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

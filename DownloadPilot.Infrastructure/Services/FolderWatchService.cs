using System.Collections.Concurrent;
using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using Microsoft.Extensions.Logging;

namespace DownloadPilot.Infrastructure.Services;

public sealed class FolderWatchService(
    ISettingsService settingsService,
    IFileStabilityService fileStabilityService,
    ILogger<FolderWatchService> logger) : IFolderWatchService
{
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, byte> _processing = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource _runCancellation = new();

    public event EventHandler<string>? FileReady;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_watchers.Count > 0)
        {
            return;
        }

        if (_runCancellation.IsCancellationRequested)
        {
            _runCancellation.Dispose();
            _runCancellation = new CancellationTokenSource();
        }

        var settings = await settingsService.LoadAsync(cancellationToken);

        foreach (var folder in settings.WatchedFolders.Where(f => f.IsEnabled))
        {
            if (!Directory.Exists(folder.Path))
            {
                logger.LogWarning("Bewaakte map bestaat niet: {Folder}", folder.Path);
                continue;
            }

            var watcher = new FileSystemWatcher(folder.Path)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            watcher.Created += OnCreated;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnWatcherError;

            _watchers.Add(watcher);
            logger.LogInformation("Bewaking gestart voor map: {Folder}", folder.Path);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _runCancellation.Cancel();

        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnCreated;
            watcher.Renamed -= OnRenamed;
            watcher.Error -= OnWatcherError;
            watcher.Dispose();
        }

        _watchers.Clear();
        _processing.Clear();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _runCancellation.Dispose();
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        _ = ProcessCandidateAsync(e.FullPath);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        _ = ProcessCandidateAsync(e.FullPath);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        logger.LogError(e.GetException(), "Fout in folderbewaking");
    }

    private async Task ProcessCandidateAsync(string fullPath)
    {
        var runToken = _runCancellation.Token;

        if (Directory.Exists(fullPath))
        {
            return;
        }

        if (!_processing.TryAdd(fullPath, 0))
        {
            return;
        }

        try
        {
            var stable = await fileStabilityService.WaitUntilStableAsync(fullPath, runToken);
            if (!stable)
            {
                return;
            }

            FileReady?.Invoke(this, fullPath);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kon bestand niet verwerken: {Path}", fullPath);
        }
        finally
        {
            _processing.TryRemove(fullPath, out _);
        }
    }
}

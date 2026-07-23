using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using System.Collections.Concurrent;
using System.IO;

namespace DownloadPilot.App.Services;

public readonly record struct ManualScanProgress(int SeenFiles, int ProcessedFiles, int ProposedFiles);

public sealed class ManualScanService(IFileStabilityService stabilityService, IFileAnalysisService analysisService)
{
    public async Task<IReadOnlyList<FileAnalysisResult>> ScanFolderAsync(
        string folderPath,
        CancellationToken cancellationToken,
        Action<ManualScanProgress>? onProgress = null)
    {
        if (!Directory.Exists(folderPath))
        {
            return [];
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System
        };

        var files = Directory.EnumerateFiles(folderPath, "*", options).ToArray();
        if (files.Length == 0)
        {
            onProgress?.Invoke(new ManualScanProgress(0, 0, 0));
            return [];
        }

        var results = new ConcurrentBag<FileAnalysisResult>();
        var seenFiles = files.Length;
        var processedFiles = 0;
        var proposedFiles = 0;

        onProgress?.Invoke(new ManualScanProgress(seenFiles, 0, 0));

        var maxDegree = Math.Clamp(Environment.ProcessorCount, 2, 6);
        await Parallel.ForEachAsync(
            files,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maxDegree
            },
            async (filePath, ct) =>
            {
                if (!File.Exists(filePath))
                {
                    ReportProcessed(ref processedFiles, proposedFiles, seenFiles, onProgress);
                    return;
                }

                bool stable;
                try
                {
                    stable = IsLikelyStableForManualScan(filePath) || await stabilityService.WaitUntilStableAsync(filePath, ct);
                }
                catch (IOException)
                {
                    ReportProcessed(ref processedFiles, proposedFiles, seenFiles, onProgress);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    ReportProcessed(ref processedFiles, proposedFiles, seenFiles, onProgress);
                    return;
                }

                if (!stable)
                {
                    ReportProcessed(ref processedFiles, proposedFiles, seenFiles, onProgress);
                    return;
                }

                var sourceFolder = Path.GetDirectoryName(filePath) ?? folderPath;
                try
                {
                    var analysis = await analysisService.AnalyzeAsync(filePath, sourceFolder, ct);
                    results.Add(analysis);
                    Interlocked.Increment(ref proposedFiles);
                }
                catch (IOException)
                {
                    // File is transient or locked; skip in manual scan.
                }
                catch (UnauthorizedAccessException)
                {
                    // File is not accessible; skip in manual scan.
                }

                ReportProcessed(ref processedFiles, proposedFiles, seenFiles, onProgress);
            });

        onProgress?.Invoke(new ManualScanProgress(seenFiles, processedFiles, proposedFiles));

        return results.ToList();
    }

    private static bool IsLikelyStableForManualScan(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".crdownload", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".part", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".download", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var info = new FileInfo(filePath);
        var age = DateTime.UtcNow - info.LastWriteTimeUtc;
        return age >= TimeSpan.FromSeconds(20);
    }

    private static void ReportProcessed(
        ref int processedFiles,
        int proposedFiles,
        int seenFiles,
        Action<ManualScanProgress>? onProgress)
    {
        var processed = Interlocked.Increment(ref processedFiles);

        if (processed == seenFiles || processed % 10 == 0)
        {
            onProgress?.Invoke(new ManualScanProgress(seenFiles, processed, proposedFiles));
        }
    }
}

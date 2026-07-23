using DownloadPilot.Core.Models;

namespace DownloadPilot.Core.Abstractions;

public interface IFileAnalysisService
{
    Task<FileAnalysisResult> AnalyzeAsync(string filePath, string sourceFolder, CancellationToken cancellationToken);
}

using DownloadPilot.Core.Models;

namespace DownloadPilot.App.ViewModels;

public sealed class RuleTestResultViewModel
{
    public RuleTestResultViewModel(FileAnalysisResult analysis, FileOperationRequest request)
    {
        OriginalFileName = analysis.OriginalFileName;
        Category = analysis.SuggestedCategory.ToString();
        Confidence = analysis.Confidence;
        TargetFolder = request.TargetFolder;
        TargetFileName = request.TargetFileName;
        FileSizeReadable = FormatFileSize(analysis.FileSizeBytes);
    }

    public string OriginalFileName { get; }

    public string Category { get; }

    public int Confidence { get; }

    public string TargetFolder { get; }

    public string TargetFileName { get; }

    public string FileSizeReadable { get; }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }
}

namespace DownloadPilot.App.ViewModels;

public sealed class DuplicateGroupViewModel
{
    public required string GroupName { get; init; }

    public int FileCount { get; init; }

    public long TotalSizeBytes { get; init; }

    public string TotalSizeReadable => FormatBytes(TotalSizeBytes);

    public required string SuggestedAction { get; init; }

    public required string ExampleFiles { get; init; }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)Math.Max(0, bytes);
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}

namespace DownloadPilot.App.ViewModels;

public sealed class ActionQueueItemViewModel
{
    public required string Action { get; init; }

    public required string FileName { get; init; }

    public required string TargetFolder { get; init; }

    public required string TargetFileName { get; init; }

    public required string Status { get; init; }

    public int Confidence { get; init; }
}

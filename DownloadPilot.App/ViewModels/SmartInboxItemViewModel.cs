namespace DownloadPilot.App.ViewModels;

public sealed class SmartInboxItemViewModel
{
    public required string Type { get; init; }

    public required string Title { get; init; }

    public required string Detail { get; init; }

    public required string Action { get; init; }

    public required string TargetFolder { get; init; }

    public required string Severity { get; init; }

    public int Confidence { get; init; }
}

namespace DownloadPilot.App.ViewModels;

public sealed class RuleScoreViewModel
{
    public required string RuleName { get; init; }

    public int Score { get; init; }

    public int Matches { get; init; }

    public required string Health { get; init; }

    public required string Detail { get; init; }
}

using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;

namespace DownloadPilot.App.ViewModels;

public sealed class RuleSuggestionViewModel : ObservableObject
{
    private bool _isSelected = true;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public required string Name { get; init; }

    public required string Extension { get; init; }

    public required string DestinationFolder { get; init; }

    public string? FixedDestinationFolder { get; init; }

    public FileCategory Category { get; init; }

    public int MatchCount { get; init; }

    public int Confidence { get; init; }

    public required string Reason { get; init; }

    public RuleDefinition ToRuleDefinition()
    {
        return new RuleDefinition
        {
            Name = Name,
            ExtensionEquals = string.IsNullOrWhiteSpace(Extension) ? null : Extension,
            AutoApply = Confidence >= 90,
            Priority = Confidence >= 90 ? 90 : 82,
            Category = Category,
            DestinationFolder = FixedDestinationFolder
        };
    }
}

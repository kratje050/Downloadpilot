namespace DownloadPilot.App.ViewModels;

public sealed class ToolInsightItemViewModel : ObservableObject
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public required string Title { get; init; }

    public required string Detail { get; init; }

    public required string Metric { get; init; }

    public required string Category { get; init; }

    public long SizeBytes { get; init; }

    public string? Path { get; init; }

    public string? Action { get; init; }
}

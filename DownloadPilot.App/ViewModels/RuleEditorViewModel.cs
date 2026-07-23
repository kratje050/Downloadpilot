using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;

namespace DownloadPilot.App.ViewModels;

public sealed class RuleEditorViewModel : ObservableObject
{
    private int _id;
    private string _name = string.Empty;
    private string? _extensionEquals;
    private string? _fileNameContains;
    private string? _sourceFolderContains;
    private bool _autoApply;
    private int _priority = 80;
    private FileCategory _category = FileCategory.Overig;
    private string? _destinationFolder;
    private string? _renameTemplate;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? ExtensionEquals
    {
        get => _extensionEquals;
        set => SetProperty(ref _extensionEquals, value);
    }

    public string? FileNameContains
    {
        get => _fileNameContains;
        set => SetProperty(ref _fileNameContains, value);
    }

    public string? SourceFolderContains
    {
        get => _sourceFolderContains;
        set => SetProperty(ref _sourceFolderContains, value);
    }

    public bool AutoApply
    {
        get => _autoApply;
        set => SetProperty(ref _autoApply, value);
    }

    public int Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
    }

    public FileCategory Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string? DestinationFolder
    {
        get => _destinationFolder;
        set => SetProperty(ref _destinationFolder, value);
    }

    public string? RenameTemplate
    {
        get => _renameTemplate;
        set => SetProperty(ref _renameTemplate, value);
    }

    public RuleDefinition ToRuleDefinition()
    {
        return new RuleDefinition
        {
            Id = Id,
            Name = Name.Trim(),
            ExtensionEquals = Normalize(ExtensionEquals),
            FileNameContains = Normalize(FileNameContains),
            SourceFolderContains = Normalize(SourceFolderContains),
            AutoApply = AutoApply,
            Priority = Priority,
            Category = Category,
            DestinationFolder = Normalize(DestinationFolder),
            RenameTemplate = Normalize(RenameTemplate)
        };
    }

    public void Reset()
    {
        Id = 0;
        Name = string.Empty;
        ExtensionEquals = null;
        FileNameContains = null;
        SourceFolderContains = null;
        AutoApply = false;
        Priority = 80;
        Category = FileCategory.Overig;
        DestinationFolder = null;
        RenameTemplate = null;
    }

    public void LoadFrom(RuleDefinition rule)
    {
        Id = rule.Id;
        Name = rule.Name;
        ExtensionEquals = rule.ExtensionEquals;
        FileNameContains = rule.FileNameContains;
        SourceFolderContains = rule.SourceFolderContains;
        AutoApply = rule.AutoApply;
        Priority = rule.Priority;
        Category = rule.Category;
        DestinationFolder = rule.DestinationFolder;
        RenameTemplate = rule.RenameTemplate;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

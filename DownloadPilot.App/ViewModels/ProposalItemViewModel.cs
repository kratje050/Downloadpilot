using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using System.IO;

namespace DownloadPilot.App.ViewModels;

public sealed class ProposalItemViewModel : ObservableObject
{
    private bool _isSelected;
    private string _targetFolder;
    private string _targetFileName;

    public ProposalItemViewModel(FileAnalysisResult analysis)
    {
        Analysis = analysis;
        _targetFolder = analysis.SuggestedDestinationFolder;
        _targetFileName = analysis.SuggestedFileName;
    }

    public FileAnalysisResult Analysis { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string OriginalFileName => Analysis.OriginalFileName;

    public string OriginalPath => Analysis.OriginalPath;

    public string SourceFolder => Analysis.SourceFolder;

    public string Category => Analysis.SuggestedCategory.ToString();

    public string Reason => Analysis.Reason;

    public int Confidence => Analysis.Confidence;

    public int SafetyScore => ComputeSafetyScore();

    public string SafetyLabel => SafetyScore >= 80
        ? "Veilig"
        : SafetyScore >= 55
            ? "Twijfel"
            : "Niet aanraden";

    public string SafetyExplanation => BuildSafetyExplanation();

    public string WhyFound => $"{Analysis.Reason}. Doel: {TargetFullPath}";

    public string DryRunAction => $"Zou verplaatsen naar {TargetFullPath}";

    public long FileSizeBytes => Analysis.FileSizeBytes;

    public string FileSizeReadable => FormatFileSize(Analysis.FileSizeBytes);

    public string CreatedLocalReadable => Analysis.CreatedLocal.ToString("dd-MM-yyyy HH:mm");

    public string TargetFullPath => Path.Combine(TargetFolder, TargetFileName);

    public string TargetFolder
    {
        get => _targetFolder;
        set
        {
            if (SetProperty(ref _targetFolder, value))
            {
                RaisePropertyChanged(nameof(TargetFullPath));
                RaiseDerivedActionProperties();
            }
        }
    }

    public string TargetFileName
    {
        get => _targetFileName;
        set
        {
            if (SetProperty(ref _targetFileName, value))
            {
                RaisePropertyChanged(nameof(TargetFullPath));
                RaiseDerivedActionProperties();
            }
        }
    }

    private void RaiseDerivedActionProperties()
    {
        RaisePropertyChanged(nameof(SafetyScore));
        RaisePropertyChanged(nameof(SafetyLabel));
        RaisePropertyChanged(nameof(SafetyExplanation));
        RaisePropertyChanged(nameof(WhyFound));
        RaisePropertyChanged(nameof(DryRunAction));
    }

    private int ComputeSafetyScore()
    {
        var score = Analysis.Confidence;
        if (IsRiskyExtension(Analysis.Extension))
        {
            score -= 35;
        }

        if (Analysis.Reason.Contains("duplicaat", StringComparison.OrdinalIgnoreCase))
        {
            score -= 20;
        }

        if (Analysis.SuggestedCategory == FileCategory.Overig)
        {
            score -= 15;
        }

        if (TargetFolder.StartsWith(SourceFolder, StringComparison.OrdinalIgnoreCase))
        {
            score -= 8;
        }

        if (!string.IsNullOrWhiteSpace(TargetFolder) && !string.IsNullOrWhiteSpace(TargetFileName))
        {
            score += 6;
        }

        return Math.Clamp(score, 0, 100);
    }

    private string BuildSafetyExplanation()
    {
        var parts = new List<string>
        {
            $"{SafetyLabel}: {SafetyScore}% veiligheid"
        };

        if (IsRiskyExtension(Analysis.Extension))
        {
            parts.Add("uitvoerbaar/scriptachtig bestand");
        }

        if (Analysis.Reason.Contains("duplicaat", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("mogelijk duplicaat");
        }

        if (Analysis.SuggestedCategory == FileCategory.Overig)
        {
            parts.Add("lage categoriezekerheid");
        }

        parts.Add(Analysis.Reason);
        return string.Join("; ", parts);
    }

    private static bool IsRiskyExtension(string extension)
    {
        return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".vbs", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".js", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".scr", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jar", StringComparison.OrdinalIgnoreCase);
    }

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

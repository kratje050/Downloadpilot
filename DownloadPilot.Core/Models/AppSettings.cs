namespace DownloadPilot.Core.Models;

public sealed class AppSettings
{
    public List<WatchedFolder> WatchedFolders { get; init; } = new();

    public List<string> ProtectedPaths { get; init; } = new();

    public List<string> IgnoredPaths { get; init; } = new();

    public List<string> ExtraScanPaths { get; init; } = new();

    public string DefaultDestinationRoot { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "DownloadPilot");

    public string Language { get; init; } = "nl-NL";

    public bool HasCompletedOnboarding { get; init; }

    public string Theme { get; init; } = "Windows";

    public bool StartWithWindows { get; init; }

    public bool NotificationsEnabled { get; init; } = true;

    public bool UpdateChecksEnabled { get; init; } = true;

    public bool AutoDownloadUpdates { get; init; }

    public string OrganizationProfile { get; init; } = "Veilig";

    public string CleanupSchedule { get; init; } = "Wekelijks";

    public bool PermissionNoticeAccepted { get; init; }

    public int MinAutoApplyConfidence { get; init; } = 85;

    public bool AutomaticBackupsEnabled { get; init; } = true;

    public int HistoryRetentionDays { get; init; } = 180;

    public bool StoreDocumentText { get; init; }

    public bool OcrEnabled { get; init; }

    public bool HashCheckEnabled { get; init; } = true;
}

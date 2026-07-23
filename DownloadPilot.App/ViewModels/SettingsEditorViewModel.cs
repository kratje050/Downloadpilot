namespace DownloadPilot.App.ViewModels;

public sealed class SettingsEditorViewModel : ObservableObject
{
    private int _minAutoApplyConfidence = 85;
    private string _defaultDestinationRoot = string.Empty;
    private string _theme = "Windows";
    private bool _automaticBackupsEnabled = true;
    private int _historyRetentionDays = 180;
    private bool _notificationsEnabled = true;
    private bool _updateChecksEnabled = true;
    private bool _autoDownloadUpdates;
    private string _organizationProfile = "Veilig";
    private string _cleanupSchedule = "Wekelijks";
    private bool _permissionNoticeAccepted;
    private bool _storeDocumentText;
    private bool _startWithWindows;
    private bool _ocrEnabled;
    private bool _hashCheckEnabled = true;

    public int MinAutoApplyConfidence
    {
        get => _minAutoApplyConfidence;
        set => SetProperty(ref _minAutoApplyConfidence, value);
    }

    public string DefaultDestinationRoot
    {
        get => _defaultDestinationRoot;
        set => SetProperty(ref _defaultDestinationRoot, value);
    }

    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    public bool AutomaticBackupsEnabled
    {
        get => _automaticBackupsEnabled;
        set => SetProperty(ref _automaticBackupsEnabled, value);
    }

    public int HistoryRetentionDays
    {
        get => _historyRetentionDays;
        set => SetProperty(ref _historyRetentionDays, value);
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public bool UpdateChecksEnabled
    {
        get => _updateChecksEnabled;
        set => SetProperty(ref _updateChecksEnabled, value);
    }

    public bool AutoDownloadUpdates
    {
        get => _autoDownloadUpdates;
        set => SetProperty(ref _autoDownloadUpdates, value);
    }

    public string OrganizationProfile
    {
        get => _organizationProfile;
        set => SetProperty(ref _organizationProfile, value);
    }

    public string CleanupSchedule
    {
        get => _cleanupSchedule;
        set => SetProperty(ref _cleanupSchedule, value);
    }

    public bool PermissionNoticeAccepted
    {
        get => _permissionNoticeAccepted;
        set => SetProperty(ref _permissionNoticeAccepted, value);
    }

    public bool StoreDocumentText
    {
        get => _storeDocumentText;
        set => SetProperty(ref _storeDocumentText, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    public bool OcrEnabled
    {
        get => _ocrEnabled;
        set => SetProperty(ref _ocrEnabled, value);
    }

    public bool HashCheckEnabled
    {
        get => _hashCheckEnabled;
        set => SetProperty(ref _hashCheckEnabled, value);
    }
}

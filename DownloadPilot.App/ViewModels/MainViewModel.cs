using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using DownloadPilot.App.Commands;
using DownloadPilot.App.Services;
using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IFolderWatchService _folderWatchService;
    private readonly IFileAnalysisService _fileAnalysisService;
    private readonly IRuleEngine _ruleEngine;
    private readonly ISettingsService _settingsService;
    private readonly IFileOperationService _fileOperationService;
    private readonly IUndoService _undoService;
    private readonly IHistoryService _historyService;
    private readonly INotificationService _notificationService;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly IStartupRegistrationService _startupRegistrationService;
    private readonly IMailSpamFilterService _mailSpamFilterService;
    private readonly IUpdateService _updateService;
    private readonly ManualScanService _manualScanService;

    private ProposalItemViewModel? _selectedProposal;
    private bool _isMonitoring;
    private string _statusMessage = "Klaar";
    private int _organizedFilesCount;
    private RuleDefinition? _selectedRule;
    private WatchedFolder? _selectedWatchedFolder;
    private HistoryEntry? _selectedHistoryEntry;
    private AppSettings? _appSettings;
    private readonly List<HistoryEntry> _historyCache = new();
    private readonly List<long> _currentSessionHistoryIds = new();
    private string _selectedHistoryFilter = "Alle";
    private int _scanFileCount;
    private long _scanTotalSizeBytes;
    private int _scanOldFilesCount;
    private int _scanLargeFilesCount;
    private int _scanArchiveCount;
    private int _scanInstallerCount;
    private int _scanUncategorizedCount;
    private int _scanPossibleDuplicatesCount;
    private int _scanLikelySafeMoveCount;
    private bool _isScanning;
    private bool _isProcessingAllProposals;
    private bool _isTestingRule;
    private string _proposalSearchText = string.Empty;
    private string _duplicateSearchText = string.Empty;
    private string _ruleSearchText = string.Empty;
    private string _historySearchText = string.Empty;
    private string _ruleTestSummary = "Nog niet getest";
    private string _selectedPreviewTitle = "Geen bestand geselecteerd";
    private string _selectedPreviewText = "Selecteer een voorstel om een snelle preview te zien.";
    private string? _selectedPreviewImagePath;
    private bool _isSelectedPreviewImageVisible;
    private bool _isSelectedPreviewTextVisible;
    private bool _isSelectedPreviewFallbackVisible = true;
    private int _previewRequestId;
    private bool _isOnboardingVisible;
    private int _onboardingStep;
    private bool _hasCompletedOnboarding;
    private string _selectedOnboardingMode = "Veilig handmatig";
    private string _selectedWorkflowMode = "Veilige modus";
    private string _workflowModeSummary = "Veilige modus: alleen voorstellen met hoge betrouwbaarheid komen in de snelle wachtrij.";
    private string _cleanupReportTitle = "Nog geen slimme scan";
    private string _cleanupReportText = "Scan je Downloads-map of wacht op nieuwe bestanden. Daarna maakt DownloadPilot hier een compact opruimrapport.";
    private string _selectedMailProvider = "Gmail";
    private string _mailAddress = string.Empty;
    private string _mailUserName = string.Empty;
    private string _mailPassword = string.Empty;
    private string _mailImapHost = "imap.gmail.com";
    private int _mailImapPort = 993;
    private bool _mailUseSsl = true;
    private string _mailFolderName = "INBOX";
    private string _mailSpamFolderName = "[Gmail]/Spam";
    private int _mailMaxMessages = 50;
    private string _mailStatusMessage = "Gmail: gebruik een app-wachtwoord. Outlook/Hotmail kan met een OAuth access token. Dit wordt niet opgeslagen.";
    private string _mailProviderHelpText = "Gmail ondersteunt app-wachtwoorden voor accounts met tweestapsverificatie. Hotmail/Outlook vereist meestal OAuth; deze versie toont daarvoor alvast de juiste waarschuwing.";
    private bool _isMailScanning;
    private int _mailScannedCount;
    private MailSpamItemViewModel? _selectedMailSpamItem;
    private string _naturalRuleInstruction = "Zet alle bonnetjes van Albert Heijn in boodschappen";
    private string _naturalRuleFeedback = "Typ een regel in gewone taal en laat DownloadPilot hem omzetten naar een echte regel.";
    private string _toolStatusMessage = "Slimme tools klaar";
    private string _weeklyReportText = "Maak een weekrapport om te zien wat DownloadPilot heeft georganiseerd.";
    private string _invoiceExportStatus = "Nog geen factuurexport gemaakt";
    private string _backupStatus = "Herstelpunten worden automatisch gemaakt bij grote acties.";
    private string _browserSourceStatus = "Scan voorstellen om download-herkomst uit Windows metadata te tonen.";
    private string _steamResidueStatus = "Nog niet gescand op game- en modrestanten.";
    private string _dryRunStatus = "Proefmodus is klaar: bekijk eerst wat er zou gebeuren.";
    private string _safetyStatus = "Veiligheidsscores worden automatisch bijgewerkt bij nieuwe voorstellen.";
    private string _appResidueStatus = "Nog niet gescand op app-restanten.";
    private string _driverCleanupStatus = "Nog niet gescand op driver-downloads.";
    private string _storageMapStatus = "Nog geen opslagkaart gemaakt.";
    private string _protectedPathsStatus = "Geen beschermde paden ingesteld.";
    private string _updateStatus = "Updatecheck via GitHub staat klaar.";
    private string _advancedAuditStatus = "Power-audit nog niet uitgevoerd.";
    private string _permissionSummary = "DownloadPilot scant alleen gekozen lokale mappen.";
    private string _cleanupScheduleStatus = "Nog geen opruimplanning geregistreerd.";
    private long _potentialCleanupSavingsBytes;
    private bool _isScanningSteamResidues;
    private bool _isCleanupAdvisorScanning;

    public MainViewModel(
        IFolderWatchService folderWatchService,
        IFileAnalysisService fileAnalysisService,
        IRuleEngine ruleEngine,
        ISettingsService settingsService,
        IFileOperationService fileOperationService,
        IUndoService undoService,
        IHistoryService historyService,
        INotificationService notificationService,
        IDuplicateDetectionService duplicateDetectionService,
        IStartupRegistrationService startupRegistrationService,
        IMailSpamFilterService mailSpamFilterService,
        IUpdateService updateService,
        ManualScanService manualScanService)
    {
        _folderWatchService = folderWatchService;
        _fileAnalysisService = fileAnalysisService;
        _ruleEngine = ruleEngine;
        _settingsService = settingsService;
        _fileOperationService = fileOperationService;
        _undoService = undoService;
        _historyService = historyService;
        _notificationService = notificationService;
        _duplicateDetectionService = duplicateDetectionService;
        _startupRegistrationService = startupRegistrationService;
        _mailSpamFilterService = mailSpamFilterService;
        _updateService = updateService;
        _manualScanService = manualScanService;

        _folderWatchService.FileReady += OnFileReady;

        StartMonitoringCommand = new AsyncRelayCommand(StartMonitoringAsync, () => !IsMonitoring);
        StopMonitoringCommand = new AsyncRelayCommand(StopMonitoringAsync, () => IsMonitoring);
        MoveSelectedCommand = new AsyncRelayCommand(MoveSelectedAsync, () => SelectedProposal is not null);
        IgnoreSelectedCommand = new RelayCommand(IgnoreSelected, () => SelectedProposal is not null);
        ChooseProposalTargetFolderCommand = new RelayCommand(ChooseProposalTargetFolder, () => SelectedProposal is not null);
        OpenSelectedOriginalCommand = new RelayCommand(OpenSelectedOriginal, CanOpenSelectedOriginal);
        RevealSelectedOriginalCommand = new RelayCommand(RevealSelectedOriginal, CanOpenSelectedOriginal);
        RememberRuleForSelectedCommand = new AsyncRelayCommand(RememberRuleForSelectedAsync, () => SelectedProposal is not null);
        AlwaysApplySelectedCommand = new AsyncRelayCommand(AlwaysApplySelectedAsync, () => SelectedProposal is not null);
        DecideLaterCommand = new RelayCommand(DecideLater, () => SelectedProposal is not null);
        UndoLastCommand = new AsyncRelayCommand(UndoLastAsync);
        UndoSelectedHistoryCommand = new AsyncRelayCommand(
            UndoSelectedHistoryAsync,
            () => SelectedHistoryEntry is { CanUndo: true, Status: HistoryStatus.Geslaagd });
        RollbackCurrentSessionCommand = new AsyncRelayCommand(
            RollbackCurrentSessionAsync,
            () => SessionUndoableCount > 0);
        ScanDownloadsCommand = new AsyncRelayCommand(ScanDownloadsAsync, () => !IsScanning);
        ProcessAllProposalsCommand = new AsyncRelayCommand(
            ProcessAllProposalsAsync,
            () => !_isProcessingAllProposals && NewFileProposals.Count > 0);
        ProcessSelectedProposalsCommand = new AsyncRelayCommand(
            ProcessSelectedProposalsAsync,
            () => !_isProcessingAllProposals && SelectedProposalsCount > 0);
        SelectAllProposalsCommand = new RelayCommand(SelectAllVisibleProposals, () => NewFileProposals.Count > 0);
        ClearProposalSelectionCommand = new RelayCommand(ClearProposalSelection, () => SelectedProposalsCount > 0);
        IgnoreSelectedProposalsCommand = new RelayCommand(IgnoreSelectedProposals, () => SelectedProposalsCount > 0);
        SelectAllDuplicatesCommand = new RelayCommand(SelectAllVisibleDuplicates, () => DuplicateProposals.Count > 0);
        ClearDuplicateSelectionCommand = new RelayCommand(ClearDuplicateSelection, () => SelectedDuplicateProposalsCount > 0);
        MoveSelectedDuplicatesToRecycleBinCommand = new AsyncRelayCommand(
            MoveSelectedDuplicatesToRecycleBinAsync,
            () => !_isProcessingAllProposals && SelectedDuplicateProposalsCount > 0);
        AddRuleCommand = new AsyncRelayCommand(AddRuleAsync, CanAddRule);
        DeleteSelectedRuleCommand = new AsyncRelayCommand(DeleteSelectedRuleAsync, () => SelectedRule is not null);
        RefreshRulesCommand = new AsyncRelayCommand(RefreshRulesAsync);
        TestRuleCommand = new AsyncRelayCommand(TestRuleAsync, CanTestRule);
        NewRuleCommand = new RelayCommand(StartNewRule);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ReloadSettingsCommand = new AsyncRelayCommand(ReloadSettingsAsync);
        ChooseDefaultDestinationRootCommand = new RelayCommand(ChooseDefaultDestinationRoot);
        ExportRulesCommand = new AsyncRelayCommand(ExportRulesAsync);
        ImportRulesCommand = new AsyncRelayCommand(ImportRulesAsync);
        SetupAutomaticOrganizationCommand = new AsyncRelayCommand(SetupAutomaticOrganizationAsync);
        RefreshSmartWorkflowCommand = new RelayCommand(RefreshSmartWorkflow);
        ProcessSafeQueueCommand = new AsyncRelayCommand(
            ProcessSafeQueueAsync,
            () => !_isProcessingAllProposals && SafeQueueCount > 0);
        ApplyWorkflowModeCommand = new AsyncRelayCommand(ApplySelectedWorkflowModeAsync);
        ApplyRuleSuggestionsCommand = new AsyncRelayCommand(
            ApplyRuleSuggestionsAsync,
            () => RuleSuggestions.Any(suggestion => suggestion.IsSelected));
        ApplyMailProviderPresetCommand = new RelayCommand(ApplyMailProviderPreset);
        ScanMailSpamCommand = new AsyncRelayCommand(ScanMailSpamAsync, () => !IsMailScanning);
        SelectHighConfidenceMailSpamCommand = new RelayCommand(SelectHighConfidenceMailSpam, () => MailSpamMessages.Count > 0);
        ClearMailSpamSelectionCommand = new RelayCommand(ClearMailSpamSelection, () => SelectedMailSpamCount > 0);
        MoveSelectedMailSpamCommand = new AsyncRelayCommand(
            MoveSelectedMailSpamAsync,
            () => !IsMailScanning && SelectedMailSpamCount > 0);
        MoveAllMailSpamCommand = new AsyncRelayCommand(
            MoveAllMailSpamAsync,
            () => !IsMailScanning && MailSpamMessages.Count > 0);
        GenerateNaturalRuleCommand = new RelayCommand(GenerateNaturalRule);
        ExportInvoicesCsvCommand = new AsyncRelayCommand(ExportInvoicesCsvAsync);
        ApplySmartNameRepairCommand = new RelayCommand(ApplySmartNameRepair, () => SmartRenameItems.Count > 0);
        RefreshSmartToolsCommand = new RelayCommand(RefreshSmartTools);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesManuallyAsync);
        OpenSelectedToolPathsCommand = new RelayCommand(OpenSelectedToolPaths);
        RevealSelectedToolPathsCommand = new RelayCommand(RevealSelectedToolPaths);
        IgnoreSelectedToolPathsCommand = new AsyncRelayCommand(IgnoreSelectedToolPathsAsync);
        ProtectSelectedToolPathsCommand = new AsyncRelayCommand(ProtectSelectedToolPathsAsync);
        MoveSelectedToolItemsToRecycleBinCommand = new AsyncRelayCommand(MoveSelectedToolItemsToRecycleBinAsync);
        MoveSelectedToolItemsToQuarantineCommand = new AsyncRelayCommand(MoveSelectedToolItemsToQuarantineAsync);
        AddExtraScanPathCommand = new AsyncRelayCommand(AddExtraScanPathAsync);
        RemoveSelectedExtraScanPathsCommand = new AsyncRelayCommand(RemoveSelectedExtraScanPathsAsync);
        RemoveSelectedIgnoredPathsCommand = new AsyncRelayCommand(RemoveSelectedIgnoredPathsAsync);
        RunDryRunCommand = new RelayCommand(RunDryRun, CanRunDryRun);
        RefreshCleanupAdvisorCommand = new AsyncRelayCommand(RefreshCleanupAdvisorAsync, () => !IsCleanupAdvisorScanning);
        ProtectSelectedProposalFolderCommand = new AsyncRelayCommand(
            ProtectSelectedProposalFoldersAsync,
            () => SelectedProposal is not null || SelectedProposalsCount > 0);
        RemoveSelectedProtectedPathsCommand = new AsyncRelayCommand(
            RemoveSelectedProtectedPathsAsync,
            () => ProtectedPathItems.Any(item => item.IsSelected));
        ExportMonthlyCleanupReportCommand = new AsyncRelayCommand(ExportMonthlyCleanupReportAsync);
        MoveRiskyDownloadsToQuarantineCommand = new AsyncRelayCommand(
            MoveRiskyDownloadsToQuarantineAsync,
            () => !_isProcessingAllProposals && QuarantineItems.Count > 0);
        ExportWeeklyCleanupReportCommand = new AsyncRelayCommand(ExportWeeklyCleanupReportAsync);
        RegisterWeeklyCleanupReportCommand = new AsyncRelayCommand(RegisterWeeklyCleanupReportAsync);
        RegisterCleanupScheduleCommand = new AsyncRelayCommand(RegisterCleanupScheduleAsync);
        RefreshBrowserDownloadSourcesCommand = new RelayCommand(RefreshBrowserDownloadSources);
        FindSimilarPhotosCommand = new RelayCommand(FindSimilarPhotos);
        CreateSmartBackupCommand = new AsyncRelayCommand(CreateManualSmartBackupAsync);
        ScanSteamResiduesCommand = new AsyncRelayCommand(ScanSteamResiduesAsync, () => !IsScanningSteamResidues);
        SelectAllSteamResiduesCommand = new RelayCommand(SelectAllSteamResidues, () => SteamResidueItems.Count > 0);
        ClearSteamResidueSelectionCommand = new RelayCommand(ClearSteamResidueSelection, () => SelectedSteamResidueCount > 0);
        MoveSelectedSteamResiduesToRecycleBinCommand = new AsyncRelayCommand(
            MoveSelectedSteamResiduesToRecycleBinAsync,
            () => !IsScanningSteamResidues && SelectedSteamResidueCount > 0);
        AddWatchedFolderCommand = new RelayCommand(AddWatchedFolder);
        RemoveWatchedFolderCommand = new RelayCommand(RemoveSelectedWatchedFolder, () => SelectedWatchedFolder is not null);
        SaveWatchedFoldersCommand = new AsyncRelayCommand(SaveWatchedFoldersAsync);
        NextOnboardingCommand = new RelayCommand(
            NextOnboardingStep,
            () => OnboardingStep < 4);
        PreviousOnboardingCommand = new RelayCommand(
            PreviousOnboardingStep,
            () => OnboardingStep > 0);
        CompleteOnboardingCommand = new AsyncRelayCommand(() => FinishOnboardingAsync(applySelectedMode: true));
        SkipOnboardingCommand = new AsyncRelayCommand(() => FinishOnboardingAsync(applySelectedMode: false));
        RestartOnboardingCommand = new RelayCommand(RestartOnboarding);

        NewFileProposalsView = new ListCollectionView(NewFileProposals)
        {
            Filter = item => MatchesProposal(item, ProposalSearchText)
        };
        DuplicateProposalsView = new ListCollectionView(DuplicateProposals)
        {
            Filter = item => MatchesProposal(item, DuplicateSearchText)
        };
        RulesView = new ListCollectionView(Rules)
        {
            Filter = item => MatchesRule(item, RuleSearchText)
        };
        RecentHistoryView = new ListCollectionView(RecentHistory)
        {
            Filter = item => MatchesHistory(item, HistorySearchText)
        };

        NewFileProposals.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(NewFilesCount));
            ProcessAllProposalsCommand.RaiseCanExecuteChanged();
            SelectAllProposalsCommand.RaiseCanExecuteChanged();
            UpdateProposalSelectionState();
            RefreshWorkflowInsights();
            RefreshSmartToolInsights();
        };

        DuplicateProposals.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(DuplicateFilesCount));
            SelectAllDuplicatesCommand.RaiseCanExecuteChanged();
            UpdateProposalSelectionState();
            RefreshWorkflowInsights();
            RefreshSmartToolInsights();
        };

        MailSpamMessages.CollectionChanged += (_, _) =>
        {
            RaiseMailSpamStateChanged();
        };
    }

    public ObservableCollection<ProposalItemViewModel> NewFileProposals { get; } = new();

    public ICollectionView NewFileProposalsView { get; }

    public ObservableCollection<ProposalItemViewModel> DuplicateProposals { get; } = new();

    public ICollectionView DuplicateProposalsView { get; }

    public ObservableCollection<HistoryEntry> RecentHistory { get; } = new();

    public ICollectionView RecentHistoryView { get; }

    public IReadOnlyList<string> HistoryFilters { get; } =
    [
        "Alle",
        "Vandaag",
        "Deze week",
        "Verplaatst",
        "Hernoemd",
        "Automatisch uitgevoerd",
        "Mislukt",
        "Teruggedraaid"
    ];

    public ObservableCollection<RuleDefinition> Rules { get; } = new();

    public ICollectionView RulesView { get; }

    public ObservableCollection<RuleTestResultViewModel> RuleTestResults { get; } = new();

    public ObservableCollection<SmartInboxItemViewModel> SmartInboxItems { get; } = new();

    public ObservableCollection<ActionQueueItemViewModel> ActionQueueItems { get; } = new();

    public ObservableCollection<FolderPreviewItemViewModel> FolderPreviewItems { get; } = new();

    public ObservableCollection<DuplicateGroupViewModel> DuplicateGroups { get; } = new();

    public ObservableCollection<RuleSuggestionViewModel> RuleSuggestions { get; } = new();

    public ObservableCollection<RuleScoreViewModel> RuleScores { get; } = new();

    public ObservableCollection<MailSpamItemViewModel> MailSpamMessages { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> SmartRenameItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> LargeFileCoachItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> QuarantineItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> SimilarPhotoItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> BrowserDownloadSourceItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> SteamResidueItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> DryRunItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> SafetyScoreItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> AppResidueItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> DriverCleanupItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> StorageMapItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> ProtectedPathItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> IgnoredPathItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> ExtraScanPathItems { get; } = new();

    public ObservableCollection<ToolInsightItemViewModel> AdvancedAuditItems { get; } = new();

    public ObservableCollection<InvoiceDashboardItemViewModel> InvoiceDashboardItems { get; } = new();

    public ObservableCollection<WatchedFolder> WatchedFolders { get; } = new();

    public IReadOnlyList<FileCategory> AvailableCategories { get; } = Enum.GetValues<FileCategory>();

    public IReadOnlyList<string> AvailableThemes { get; } = ["Windows", "Licht", "Donker"];

    public IReadOnlyList<string> OnboardingModes { get; } =
    [
        "Veilig handmatig",
        "Automatisch met basisregels"
    ];

    public IReadOnlyList<string> WorkflowModes { get; } =
    [
        "Alleen advies",
        "Veilige modus",
        "Normale modus",
        "Snelheidsmodus"
    ];

    public IReadOnlyList<string> CleanupSchedules { get; } =
    [
        "Geen planning",
        "Wekelijks",
        "Maandelijks"
    ];

    public IReadOnlyList<string> MailProviders { get; } =
    [
        "Gmail",
        "Hotmail / Outlook",
        "Eigen IMAP"
    ];

    public RuleEditorViewModel RuleEditor { get; } = new();

    public SettingsEditorViewModel SettingsEditor { get; } = new();

    public AsyncRelayCommand StartMonitoringCommand { get; }

    public AsyncRelayCommand StopMonitoringCommand { get; }

    public AsyncRelayCommand MoveSelectedCommand { get; }

    public RelayCommand IgnoreSelectedCommand { get; }

    public RelayCommand ChooseProposalTargetFolderCommand { get; }

    public RelayCommand OpenSelectedOriginalCommand { get; }

    public RelayCommand RevealSelectedOriginalCommand { get; }

    public AsyncRelayCommand RememberRuleForSelectedCommand { get; }

    public AsyncRelayCommand AlwaysApplySelectedCommand { get; }

    public RelayCommand DecideLaterCommand { get; }

    public AsyncRelayCommand UndoLastCommand { get; }

    public AsyncRelayCommand UndoSelectedHistoryCommand { get; }

    public AsyncRelayCommand RollbackCurrentSessionCommand { get; }

    public AsyncRelayCommand ScanDownloadsCommand { get; }

    public AsyncRelayCommand ProcessAllProposalsCommand { get; }

    public AsyncRelayCommand ProcessSelectedProposalsCommand { get; }

    public RelayCommand SelectAllProposalsCommand { get; }

    public RelayCommand ClearProposalSelectionCommand { get; }

    public RelayCommand IgnoreSelectedProposalsCommand { get; }

    public RelayCommand SelectAllDuplicatesCommand { get; }

    public RelayCommand ClearDuplicateSelectionCommand { get; }

    public AsyncRelayCommand MoveSelectedDuplicatesToRecycleBinCommand { get; }

    public AsyncRelayCommand AddRuleCommand { get; }

    public AsyncRelayCommand DeleteSelectedRuleCommand { get; }

    public AsyncRelayCommand RefreshRulesCommand { get; }

    public AsyncRelayCommand TestRuleCommand { get; }

    public RelayCommand NewRuleCommand { get; }

    public AsyncRelayCommand SaveSettingsCommand { get; }

    public AsyncRelayCommand ReloadSettingsCommand { get; }

    public RelayCommand ChooseDefaultDestinationRootCommand { get; }

    public AsyncRelayCommand ExportRulesCommand { get; }

    public AsyncRelayCommand ImportRulesCommand { get; }

    public AsyncRelayCommand SetupAutomaticOrganizationCommand { get; }

    public RelayCommand RefreshSmartWorkflowCommand { get; }

    public AsyncRelayCommand ProcessSafeQueueCommand { get; }

    public AsyncRelayCommand ApplyWorkflowModeCommand { get; }

    public AsyncRelayCommand ApplyRuleSuggestionsCommand { get; }

    public RelayCommand ApplyMailProviderPresetCommand { get; }

    public AsyncRelayCommand ScanMailSpamCommand { get; }

    public RelayCommand SelectHighConfidenceMailSpamCommand { get; }

    public RelayCommand ClearMailSpamSelectionCommand { get; }

    public AsyncRelayCommand MoveSelectedMailSpamCommand { get; }

    public AsyncRelayCommand MoveAllMailSpamCommand { get; }

    public RelayCommand GenerateNaturalRuleCommand { get; }

    public AsyncRelayCommand ExportInvoicesCsvCommand { get; }

    public RelayCommand ApplySmartNameRepairCommand { get; }

    public RelayCommand RefreshSmartToolsCommand { get; }

    public AsyncRelayCommand CheckForUpdatesCommand { get; }

    public RelayCommand OpenSelectedToolPathsCommand { get; }

    public RelayCommand RevealSelectedToolPathsCommand { get; }

    public AsyncRelayCommand IgnoreSelectedToolPathsCommand { get; }

    public AsyncRelayCommand ProtectSelectedToolPathsCommand { get; }

    public AsyncRelayCommand MoveSelectedToolItemsToRecycleBinCommand { get; }

    public AsyncRelayCommand MoveSelectedToolItemsToQuarantineCommand { get; }

    public AsyncRelayCommand AddExtraScanPathCommand { get; }

    public AsyncRelayCommand RemoveSelectedExtraScanPathsCommand { get; }

    public AsyncRelayCommand RemoveSelectedIgnoredPathsCommand { get; }

    public RelayCommand RunDryRunCommand { get; }

    public AsyncRelayCommand RefreshCleanupAdvisorCommand { get; }

    public AsyncRelayCommand ProtectSelectedProposalFolderCommand { get; }

    public AsyncRelayCommand RemoveSelectedProtectedPathsCommand { get; }

    public AsyncRelayCommand MoveRiskyDownloadsToQuarantineCommand { get; }

    public AsyncRelayCommand ExportWeeklyCleanupReportCommand { get; }

    public AsyncRelayCommand RegisterWeeklyCleanupReportCommand { get; }

    public AsyncRelayCommand RegisterCleanupScheduleCommand { get; }

    public AsyncRelayCommand ExportMonthlyCleanupReportCommand { get; }

    public RelayCommand RefreshBrowserDownloadSourcesCommand { get; }

    public RelayCommand FindSimilarPhotosCommand { get; }

    public AsyncRelayCommand CreateSmartBackupCommand { get; }

    public AsyncRelayCommand ScanSteamResiduesCommand { get; }

    public RelayCommand SelectAllSteamResiduesCommand { get; }

    public RelayCommand ClearSteamResidueSelectionCommand { get; }

    public AsyncRelayCommand MoveSelectedSteamResiduesToRecycleBinCommand { get; }

    public RelayCommand AddWatchedFolderCommand { get; }

    public RelayCommand RemoveWatchedFolderCommand { get; }

    public AsyncRelayCommand SaveWatchedFoldersCommand { get; }

    public RelayCommand NextOnboardingCommand { get; }

    public RelayCommand PreviousOnboardingCommand { get; }

    public AsyncRelayCommand CompleteOnboardingCommand { get; }

    public AsyncRelayCommand SkipOnboardingCommand { get; }

    public RelayCommand RestartOnboardingCommand { get; }

    public bool IsOnboardingVisible
    {
        get => _isOnboardingVisible;
        private set => SetProperty(ref _isOnboardingVisible, value);
    }

    public int OnboardingStep
    {
        get => _onboardingStep;
        private set
        {
            if (SetProperty(ref _onboardingStep, value))
            {
                RaisePropertyChanged(nameof(OnboardingProgress));
                RaisePropertyChanged(nameof(OnboardingStepNumber));
                NextOnboardingCommand.RaiseCanExecuteChanged();
                PreviousOnboardingCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public double OnboardingProgress => (OnboardingStep + 1) * 20d;

    public int OnboardingStepNumber => OnboardingStep + 1;

    public string SelectedOnboardingMode
    {
        get => _selectedOnboardingMode;
        set => SetProperty(ref _selectedOnboardingMode, value);
    }

    public string SelectedWorkflowMode
    {
        get => _selectedWorkflowMode;
        set
        {
            if (SetProperty(ref _selectedWorkflowMode, value))
            {
                UpdateWorkflowModeSummary();
            }
        }
    }

    public string WorkflowModeSummary
    {
        get => _workflowModeSummary;
        private set => SetProperty(ref _workflowModeSummary, value);
    }

    public string CleanupReportTitle
    {
        get => _cleanupReportTitle;
        private set => SetProperty(ref _cleanupReportTitle, value);
    }

    public string CleanupReportText
    {
        get => _cleanupReportText;
        private set => SetProperty(ref _cleanupReportText, value);
    }

    public string SelectedMailProvider
    {
        get => _selectedMailProvider;
        set
        {
            if (SetProperty(ref _selectedMailProvider, value))
            {
                ApplyMailProviderPreset();
            }
        }
    }

    public string MailAddress
    {
        get => _mailAddress;
        set
        {
            if (SetProperty(ref _mailAddress, value) && string.IsNullOrWhiteSpace(MailUserName))
            {
                MailUserName = value;
            }
        }
    }

    public string MailUserName
    {
        get => _mailUserName;
        set => SetProperty(ref _mailUserName, value);
    }

    public string MailPassword
    {
        get => _mailPassword;
        set => SetProperty(ref _mailPassword, value);
    }

    public string MailImapHost
    {
        get => _mailImapHost;
        set => SetProperty(ref _mailImapHost, value);
    }

    public int MailImapPort
    {
        get => _mailImapPort;
        set => SetProperty(ref _mailImapPort, value);
    }

    public bool MailUseSsl
    {
        get => _mailUseSsl;
        set => SetProperty(ref _mailUseSsl, value);
    }

    public string MailFolderName
    {
        get => _mailFolderName;
        set => SetProperty(ref _mailFolderName, value);
    }

    public string MailSpamFolderName
    {
        get => _mailSpamFolderName;
        set => SetProperty(ref _mailSpamFolderName, value);
    }

    public int MailMaxMessages
    {
        get => _mailMaxMessages;
        set => SetProperty(ref _mailMaxMessages, value);
    }

    public string MailStatusMessage
    {
        get => _mailStatusMessage;
        private set => SetProperty(ref _mailStatusMessage, value);
    }

    public string MailProviderHelpText
    {
        get => _mailProviderHelpText;
        private set => SetProperty(ref _mailProviderHelpText, value);
    }

    public bool IsMailScanning
    {
        get => _isMailScanning;
        private set
        {
            if (SetProperty(ref _isMailScanning, value))
            {
                RaiseMailSpamStateChanged();
            }
        }
    }

    public int MailScannedCount
    {
        get => _mailScannedCount;
        private set => SetProperty(ref _mailScannedCount, value);
    }

    public MailSpamItemViewModel? SelectedMailSpamItem
    {
        get => _selectedMailSpamItem;
        set => SetProperty(ref _selectedMailSpamItem, value);
    }

    public int MailSpamCandidateCount => MailSpamMessages.Count;

    public int HighConfidenceMailSpamCount => MailSpamMessages.Count(message => message.SpamScore >= 80);

    public int SelectedMailSpamCount => MailSpamMessages.Count(message => message.IsSelected);

    public string NaturalRuleInstruction
    {
        get => _naturalRuleInstruction;
        set => SetProperty(ref _naturalRuleInstruction, value);
    }

    public string NaturalRuleFeedback
    {
        get => _naturalRuleFeedback;
        private set => SetProperty(ref _naturalRuleFeedback, value);
    }

    public string ToolStatusMessage
    {
        get => _toolStatusMessage;
        private set => SetProperty(ref _toolStatusMessage, value);
    }

    public string WeeklyReportText
    {
        get => _weeklyReportText;
        private set => SetProperty(ref _weeklyReportText, value);
    }

    public string InvoiceExportStatus
    {
        get => _invoiceExportStatus;
        private set => SetProperty(ref _invoiceExportStatus, value);
    }

    public string BackupStatus
    {
        get => _backupStatus;
        private set => SetProperty(ref _backupStatus, value);
    }

    public string BrowserSourceStatus
    {
        get => _browserSourceStatus;
        private set => SetProperty(ref _browserSourceStatus, value);
    }

    public string SteamResidueStatus
    {
        get => _steamResidueStatus;
        private set => SetProperty(ref _steamResidueStatus, value);
    }

    public string DryRunStatus
    {
        get => _dryRunStatus;
        private set => SetProperty(ref _dryRunStatus, value);
    }

    public string SafetyStatus
    {
        get => _safetyStatus;
        private set => SetProperty(ref _safetyStatus, value);
    }

    public string AppResidueStatus
    {
        get => _appResidueStatus;
        private set => SetProperty(ref _appResidueStatus, value);
    }

    public string DriverCleanupStatus
    {
        get => _driverCleanupStatus;
        private set => SetProperty(ref _driverCleanupStatus, value);
    }

    public string StorageMapStatus
    {
        get => _storageMapStatus;
        private set => SetProperty(ref _storageMapStatus, value);
    }

    public string ProtectedPathsStatus
    {
        get => _protectedPathsStatus;
        private set => SetProperty(ref _protectedPathsStatus, value);
    }

    public string UpdateStatus
    {
        get => _updateStatus;
        private set => SetProperty(ref _updateStatus, value);
    }

    public string AdvancedAuditStatus
    {
        get => _advancedAuditStatus;
        private set => SetProperty(ref _advancedAuditStatus, value);
    }

    public string PermissionSummary
    {
        get => _permissionSummary;
        private set => SetProperty(ref _permissionSummary, value);
    }

    public string CleanupScheduleStatus
    {
        get => _cleanupScheduleStatus;
        private set => SetProperty(ref _cleanupScheduleStatus, value);
    }

    public bool IsScanningSteamResidues
    {
        get => _isScanningSteamResidues;
        private set
        {
            if (SetProperty(ref _isScanningSteamResidues, value))
            {
                RaiseSteamResidueStateChanged();
            }
        }
    }

    public bool IsCleanupAdvisorScanning
    {
        get => _isCleanupAdvisorScanning;
        private set
        {
            if (SetProperty(ref _isCleanupAdvisorScanning, value))
            {
                RefreshCleanupAdvisorCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int SmartRenameCount => SmartRenameItems.Count;

    public int LargeFileCoachCount => LargeFileCoachItems.Count;

    public int QuarantineCount => QuarantineItems.Count;

    public int SimilarPhotoCount => SimilarPhotoItems.Count;

    public int BrowserSourceCount => BrowserDownloadSourceItems.Count;

    public int SteamResidueCount => SteamResidueItems.Count;

    public int DryRunCount => DryRunItems.Count;

    public int SafetyScoreCount => SafetyScoreItems.Count;

    public int AppResidueCount => AppResidueItems.Count;

    public int DriverCleanupCount => DriverCleanupItems.Count;

    public int StorageMapCount => StorageMapItems.Count;

    public int ProtectedPathCount => ProtectedPathItems.Count;

    public int IgnoredPathCount => IgnoredPathItems.Count;

    public int ExtraScanPathCount => ExtraScanPathItems.Count;

    public int AdvancedAuditCount => AdvancedAuditItems.Count;

    public int SelectedAdvancedAuditCount => AdvancedAuditItems.Count(item => item.IsSelected);

    public int SessionUndoableCount => _currentSessionHistoryIds.Count;

    public int SelectedSteamResidueCount => SteamResidueItems.Count(item => item.IsSelected);

    public string SteamResidueSizeReadable => FormatBytes(SteamResidueItems.Sum(item => item.SizeBytes));

    public int InvoiceDashboardCount => InvoiceDashboardItems.Sum(item => item.Count);

    public int SmartInboxCount => SmartInboxItems.Count;

    public int ActionQueueCount => ActionQueueItems.Count;

    public int SafeQueueCount => NewFileProposals.Count(IsSafeQueueCandidate);

    public int ReviewQueueCount => Math.Max(0, NewFileProposals.Count - SafeQueueCount);

    public int FolderPreviewCount => FolderPreviewItems.Count;

    public int RuleSuggestionCount => RuleSuggestions.Count;

    public int SelectedRuleSuggestionCount => RuleSuggestions.Count(suggestion => suggestion.IsSelected);

    public int DuplicateGroupCount => DuplicateGroups.Count;

    public int SmartPhotoFolderCount => NewFileProposals.Count(IsSmartPhotoFolder);

    public string PotentialDuplicateSavingsReadable => FormatBytes(DuplicateProposals.Sum(proposal => proposal.FileSizeBytes));

    public string PotentialCleanupSavingsReadable => FormatBytes(_potentialCleanupSavingsBytes);

    public string ProposalSearchText
    {
        get => _proposalSearchText;
        set
        {
            if (SetProperty(ref _proposalSearchText, value))
            {
                NewFileProposalsView.Refresh();
            }
        }
    }

    public string DuplicateSearchText
    {
        get => _duplicateSearchText;
        set
        {
            if (SetProperty(ref _duplicateSearchText, value))
            {
                DuplicateProposalsView.Refresh();
            }
        }
    }

    public string RuleSearchText
    {
        get => _ruleSearchText;
        set
        {
            if (SetProperty(ref _ruleSearchText, value))
            {
                RulesView.Refresh();
            }
        }
    }

    public string HistorySearchText
    {
        get => _historySearchText;
        set
        {
            if (SetProperty(ref _historySearchText, value))
            {
                RecentHistoryView.Refresh();
            }
        }
    }

    public WatchedFolder? SelectedWatchedFolder
    {
        get => _selectedWatchedFolder;
        set
        {
            if (SetProperty(ref _selectedWatchedFolder, value))
            {
                RemoveWatchedFolderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RuleDefinition? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetProperty(ref _selectedRule, value))
            {
                DeleteSelectedRuleCommand.RaiseCanExecuteChanged();
                if (value is not null)
                {
                    RuleEditor.LoadFrom(value);
                }
                AddRuleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ProposalItemViewModel? SelectedProposal
    {
        get => _selectedProposal;
        set
        {
            if (SetProperty(ref _selectedProposal, value))
            {
                MoveSelectedCommand.RaiseCanExecuteChanged();
                IgnoreSelectedCommand.RaiseCanExecuteChanged();
                ChooseProposalTargetFolderCommand.RaiseCanExecuteChanged();
                OpenSelectedOriginalCommand.RaiseCanExecuteChanged();
                RevealSelectedOriginalCommand.RaiseCanExecuteChanged();
                RememberRuleForSelectedCommand.RaiseCanExecuteChanged();
                AlwaysApplySelectedCommand.RaiseCanExecuteChanged();
                DecideLaterCommand.RaiseCanExecuteChanged();
                ProtectSelectedProposalFolderCommand.RaiseCanExecuteChanged();
                _ = RefreshSelectedPreviewAsync(value);
            }
        }
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                StartMonitoringCommand.RaiseCanExecuteChanged();
                StopMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int NewFilesCount => NewFileProposals.Count;

    public int DuplicateFilesCount => DuplicateProposals.Count;

    public int SelectedProposalsCount => NewFileProposals.Count(proposal => proposal.IsSelected);

    public int SelectedDuplicateProposalsCount => DuplicateProposals.Count(proposal => proposal.IsSelected);

    public int OrganizedFilesCount
    {
        get => _organizedFilesCount;
        private set => SetProperty(ref _organizedFilesCount, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedHistoryFilter
    {
        get => _selectedHistoryFilter;
        set
        {
            if (SetProperty(ref _selectedHistoryFilter, value))
            {
                ApplyHistoryFilter();
            }
        }
    }

    public int ScanFileCount
    {
        get => _scanFileCount;
        private set => SetProperty(ref _scanFileCount, value);
    }

    public long ScanTotalSizeBytes
    {
        get => _scanTotalSizeBytes;
        private set => SetProperty(ref _scanTotalSizeBytes, value);
    }

    public int ScanLargeFilesCount
    {
        get => _scanLargeFilesCount;
        private set => SetProperty(ref _scanLargeFilesCount, value);
    }

    public HistoryEntry? SelectedHistoryEntry
    {
        get => _selectedHistoryEntry;
        set
        {
            if (SetProperty(ref _selectedHistoryEntry, value))
            {
                UndoSelectedHistoryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int ScanOldFilesCount
    {
        get => _scanOldFilesCount;
        private set => SetProperty(ref _scanOldFilesCount, value);
    }

    public int ScanArchiveCount
    {
        get => _scanArchiveCount;
        private set => SetProperty(ref _scanArchiveCount, value);
    }

    public int ScanInstallerCount
    {
        get => _scanInstallerCount;
        private set => SetProperty(ref _scanInstallerCount, value);
    }

    public int ScanUncategorizedCount
    {
        get => _scanUncategorizedCount;
        private set => SetProperty(ref _scanUncategorizedCount, value);
    }

    public int ScanPossibleDuplicatesCount
    {
        get => _scanPossibleDuplicatesCount;
        private set => SetProperty(ref _scanPossibleDuplicatesCount, value);
    }

    public int ScanLikelySafeMoveCount
    {
        get => _scanLikelySafeMoveCount;
        private set => SetProperty(ref _scanLikelySafeMoveCount, value);
    }

    public string ScanTotalSizeReadable => FormatBytes(ScanTotalSizeBytes);

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                ScanDownloadsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsTestingRule
    {
        get => _isTestingRule;
        private set
        {
            if (SetProperty(ref _isTestingRule, value))
            {
                TestRuleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string RuleTestSummary
    {
        get => _ruleTestSummary;
        private set => SetProperty(ref _ruleTestSummary, value);
    }

    public string SelectedPreviewTitle
    {
        get => _selectedPreviewTitle;
        private set => SetProperty(ref _selectedPreviewTitle, value);
    }

    public string SelectedPreviewText
    {
        get => _selectedPreviewText;
        private set => SetProperty(ref _selectedPreviewText, value);
    }

    public string? SelectedPreviewImagePath
    {
        get => _selectedPreviewImagePath;
        private set => SetProperty(ref _selectedPreviewImagePath, value);
    }

    public bool IsSelectedPreviewImageVisible
    {
        get => _isSelectedPreviewImageVisible;
        private set => SetProperty(ref _isSelectedPreviewImageVisible, value);
    }

    public bool IsSelectedPreviewTextVisible
    {
        get => _isSelectedPreviewTextVisible;
        private set => SetProperty(ref _isSelectedPreviewTextVisible, value);
    }

    public bool IsSelectedPreviewFallbackVisible
    {
        get => _isSelectedPreviewFallbackVisible;
        private set => SetProperty(ref _isSelectedPreviewFallbackVisible, value);
    }

    public async Task InitializeAsync()
    {
        await _historyService.InitializeAsync(CancellationToken.None);
        _appSettings = await _settingsService.LoadAsync(CancellationToken.None);
        _hasCompletedOnboarding = _appSettings.HasCompletedOnboarding;
        ApplySettingsToEditor(_appSettings);
        SelectedWorkflowMode = ResolveWorkflowMode(_appSettings);
        ApplyWatchedFolders(_appSettings.WatchedFolders);
        ApplyProtectedPaths(_appSettings.ProtectedPaths);
        ApplyIgnoredPaths(_appSettings.IgnoredPaths);
        ApplyExtraScanPaths(_appSettings.ExtraScanPaths);
        RefreshPermissionSummary();
        await RefreshHistoryAsync();
        await RefreshRulesAsync();
        RefreshWorkflowInsights();
        RefreshSmartToolInsights();
        await StartMonitoringAsync();
        CheckForInterruptedSession();

        OnboardingStep = 0;
        IsOnboardingVisible = !_hasCompletedOnboarding;

        RuleEditor.PropertyChanged += (_, _) =>
        {
            AddRuleCommand.RaiseCanExecuteChanged();
            TestRuleCommand.RaiseCanExecuteChanged();
        };
    }

    private async Task StartMonitoringAsync()
    {
        await _folderWatchService.StartAsync(CancellationToken.None);
        IsMonitoring = true;
        StatusMessage = "Bewaking actief";
    }

    private async Task StopMonitoringAsync()
    {
        await _folderWatchService.StopAsync(CancellationToken.None);
        IsMonitoring = false;
        StatusMessage = "Bewaking gepauzeerd";
    }

    private async Task MoveSelectedAsync()
    {
        var proposal = SelectedProposal;
        if (proposal is null)
        {
            return;
        }

        if (IsProtectedProposal(proposal))
        {
            StatusMessage = "Overgeslagen: dit bestand staat in de niet-aanraken-lijst";
            return;
        }

        var request = new FileOperationRequest
        {
            Analysis = proposal.Analysis,
            TargetFolder = proposal.TargetFolder,
            TargetFileName = proposal.TargetFileName,
            AppliedRuleName = null,
            IsAutoApplied = false
        };

        var entry = await _fileOperationService.MoveAndRenameAsync(request, CancellationToken.None);
        if (entry.Status == Core.Enums.HistoryStatus.Geslaagd)
        {
            RememberSessionEntry(entry);
            OrganizedFilesCount++;
            RemoveProposal(proposal);
            StatusMessage = "Bestand veilig verplaatst";
        }
        else
        {
            StatusMessage = $"Actie mislukt: {entry.ErrorMessage}";
        }

        await RefreshHistoryAsync();
    }

    private void IgnoreSelected()
    {
        if (SelectedProposal is null)
        {
            return;
        }

        RemoveProposal(SelectedProposal);
        StatusMessage = "Voorstel genegeerd";
    }

    private void ChooseProposalTargetFolder()
    {
        if (SelectedProposal is null)
        {
            return;
        }

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Kies doelmap voor dit voorstel",
            SelectedPath = Directory.Exists(SelectedProposal.TargetFolder)
                ? SelectedProposal.TargetFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        SelectedProposal.TargetFolder = dialog.SelectedPath;
        StatusMessage = "Doelmap aangepast";
    }

    private void OpenSelectedOriginal()
    {
        if (!CanOpenSelectedOriginal() || SelectedProposal is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(SelectedProposal.OriginalPath)
            {
                UseShellExecute = true
            });

            StatusMessage = "Bestand geopend";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Openen mislukt: {ex.Message}";
        }
    }

    private void RevealSelectedOriginal()
    {
        if (!CanOpenSelectedOriginal() || SelectedProposal is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{SelectedProposal.OriginalPath}\"")
            {
                UseShellExecute = true
            });

            StatusMessage = "Bestand in Verkenner geselecteerd";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Verkenner openen mislukt: {ex.Message}";
        }
    }

    private bool CanOpenSelectedOriginal()
    {
        return SelectedProposal is not null && File.Exists(SelectedProposal.OriginalPath);
    }

    private async Task RememberRuleForSelectedAsync()
    {
        if (SelectedProposal is null)
        {
            return;
        }

        var rule = BuildRuleFromSelectedProposal(autoApply: false);
        await _settingsService.UpsertRuleAsync(rule, CancellationToken.None);
        await RefreshRulesAsync();
        StatusMessage = "Regel onthouden voor volgende keer";
    }

    private async Task AlwaysApplySelectedAsync()
    {
        if (SelectedProposal is null)
        {
            return;
        }

        if (IsProtectedProposal(SelectedProposal))
        {
            StatusMessage = "Overgeslagen: dit bestand staat in de niet-aanraken-lijst";
            return;
        }

        var rule = BuildRuleFromSelectedProposal(autoApply: true);
        await _settingsService.UpsertRuleAsync(rule, CancellationToken.None);
        await RefreshRulesAsync();

        var proposal = SelectedProposal;
        var request = new FileOperationRequest
        {
            Analysis = proposal.Analysis,
            TargetFolder = proposal.TargetFolder,
            TargetFileName = proposal.TargetFileName,
            AppliedRuleName = rule.Name,
            IsAutoApplied = true
        };

        var entry = await _fileOperationService.MoveAndRenameAsync(request, CancellationToken.None);
        if (entry.Status == Core.Enums.HistoryStatus.Geslaagd)
        {
            RememberSessionEntry(entry);
            OrganizedFilesCount++;
            RemoveProposal(proposal);
            await RefreshHistoryAsync();
            StatusMessage = "Automatische regel opgeslagen en bestand verplaatst";
            return;
        }

        await RefreshHistoryAsync();
        StatusMessage = $"Regel opgeslagen, maar verplaatsen mislukte: {entry.ErrorMessage}";
    }

    private void DecideLater()
    {
        StatusMessage = "Voorstel blijft staan voor later";
    }

    private async Task ProcessAllProposalsAsync()
    {
        await ProcessProposalBatchAsync(NewFileProposals.ToList(), "Alles verwerken");
    }

    private async Task ProcessSelectedProposalsAsync()
    {
        await ProcessProposalBatchAsync(
            NewFileProposals.Where(proposal => proposal.IsSelected).ToList(),
            "Aangevinkte verwerken");
    }

    private async Task ProcessProposalBatchAsync(IReadOnlyList<ProposalItemViewModel> proposals, string actionLabel)
    {
        if (_isProcessingAllProposals || proposals.Count == 0)
        {
            return;
        }

        _isProcessingAllProposals = true;
        RaiseProposalCommandStates();
        string? pendingSessionPath = null;

        try
        {
            pendingSessionPath = await CreatePendingSessionAsync(proposals, actionLabel, CancellationToken.None);

            if (proposals.Count >= 20)
            {
                await CreateSmartBackupAsync(
                    proposals,
                    $"Automatisch herstelpunt voor {actionLabel}",
                    CancellationToken.None);
            }

            var moved = 0;
            var failed = 0;
            var skippedProtected = 0;

            foreach (var proposal in proposals)
            {
                StatusMessage = $"{actionLabel}: {moved + failed + 1}/{proposals.Count}";

                if (IsProtectedProposal(proposal))
                {
                    skippedProtected++;
                    continue;
                }

                var request = new FileOperationRequest
                {
                    Analysis = proposal.Analysis,
                    TargetFolder = proposal.TargetFolder,
                    TargetFileName = proposal.TargetFileName,
                    AppliedRuleName = $"Handmatig: {actionLabel}",
                    IsAutoApplied = false
                };

                var entry = await _fileOperationService.MoveAndRenameAsync(request, CancellationToken.None);
                if (entry.Status == Core.Enums.HistoryStatus.Geslaagd)
                {
                    RememberSessionEntry(entry);
                    moved++;
                    RemoveProposal(proposal);
                }
                else
                {
                    failed++;
                }
            }

            OrganizedFilesCount += moved;
            await RefreshHistoryAsync();

            StatusMessage = failed == 0
                ? $"{actionLabel} voltooid: {moved} bestanden verplaatst, {skippedProtected} beschermd overgeslagen"
                : $"{actionLabel} voltooid: {moved} verplaatst, {failed} mislukt, {skippedProtected} beschermd overgeslagen";
        }
        finally
        {
            TryDeletePendingSession(pendingSessionPath);
            _isProcessingAllProposals = false;
            RaiseProposalCommandStates();
        }
    }

    private void SelectAllVisibleProposals()
    {
        foreach (var proposal in NewFileProposalsView.Cast<ProposalItemViewModel>())
        {
            proposal.IsSelected = true;
        }

        StatusMessage = $"{SelectedProposalsCount} voorstellen aangevinkt";
    }

    private void ClearProposalSelection()
    {
        foreach (var proposal in NewFileProposals)
        {
            proposal.IsSelected = false;
        }

        StatusMessage = "Vinkjes gewist";
    }

    private void IgnoreSelectedProposals()
    {
        var snapshot = NewFileProposals
            .Where(proposal => proposal.IsSelected)
            .ToList();

        foreach (var proposal in snapshot)
        {
            RemoveProposal(proposal);
        }

        StatusMessage = snapshot.Count == 0
            ? "Geen aangevinkte voorstellen"
            : $"{snapshot.Count} voorstellen genegeerd";
    }

    private void SelectAllVisibleDuplicates()
    {
        foreach (var proposal in DuplicateProposalsView.Cast<ProposalItemViewModel>())
        {
            proposal.IsSelected = true;
        }

        StatusMessage = $"{SelectedDuplicateProposalsCount} duplicaten aangevinkt";
    }

    private void ClearDuplicateSelection()
    {
        foreach (var proposal in DuplicateProposals)
        {
            proposal.IsSelected = false;
        }

        StatusMessage = "Duplicaatselectie gewist";
    }

    private async Task MoveSelectedDuplicatesToRecycleBinAsync()
    {
        var snapshot = DuplicateProposals
            .Where(proposal => proposal.IsSelected)
            .ToList();

        if (snapshot.Count == 0)
        {
            StatusMessage = "Geen aangevinkte duplicaten";
            return;
        }

        var confirmation = MessageBox.Show(
            $"Verplaats {snapshot.Count} aangevinkte duplicaten naar de Windows Prullenbak?",
            "Duplicaten veilig opruimen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusMessage = "Opruimen geannuleerd";
            return;
        }

        await CreateSmartBackupAsync(snapshot, "Herstelpunt voor duplicaten opruimen", CancellationToken.None);

        var cleaned = 0;
        var failed = 0;
        _isProcessingAllProposals = true;
        RaiseProposalCommandStates();

        try
        {
            foreach (var proposal in snapshot)
            {
                StatusMessage = $"Duplicaten opruimen: {cleaned + failed + 1}/{snapshot.Count}";

                if (IsProtectedProposal(proposal))
                {
                    proposal.IsSelected = false;
                    failed++;
                    continue;
                }

                try
                {
                    if (!File.Exists(proposal.OriginalPath))
                    {
                        RemoveProposal(proposal);
                        cleaned++;
                        continue;
                    }

                    await Task.Run(() =>
                    {
                        FileSystem.DeleteFile(
                            proposal.OriginalPath,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin);
                    });

                    RemoveProposal(proposal);
                    cleaned++;
                }
                catch
                {
                    proposal.IsSelected = false;
                    failed++;
                }
            }

            StatusMessage = failed == 0
                ? $"{cleaned} duplicaten naar de Prullenbak verplaatst"
                : $"{cleaned} duplicaten opgeruimd, {failed} mislukt";
        }
        finally
        {
            _isProcessingAllProposals = false;
            RaiseProposalCommandStates();
        }
    }

    private async Task UndoLastAsync()
    {
        var lastUndoable = await _historyService.GetLastUndoableAsync(CancellationToken.None);
        var undone = await _undoService.UndoLastAsync(CancellationToken.None);
        if (undone && lastUndoable is not null)
        {
            _currentSessionHistoryIds.Remove(lastUndoable.Id);
            RaiseSessionRollbackStateChanged();
        }

        StatusMessage = undone ? "Laatste actie teruggedraaid" : "Geen actie om terug te draaien";
        await RefreshHistoryAsync();
    }

    private async Task UndoSelectedHistoryAsync()
    {
        if (SelectedHistoryEntry is null)
        {
            return;
        }

        var selectedId = SelectedHistoryEntry.Id;
        var undone = await _undoService.UndoAsync(selectedId, CancellationToken.None);
        if (undone)
        {
            _currentSessionHistoryIds.Remove(selectedId);
            RaiseSessionRollbackStateChanged();
        }

        SelectedHistoryEntry = null;
        StatusMessage = undone
            ? "Geselecteerde actie teruggedraaid"
            : "Deze actie kan niet meer worden teruggedraaid";
        await RefreshHistoryAsync();
    }

    private async Task RollbackCurrentSessionAsync()
    {
        if (_currentSessionHistoryIds.Count == 0)
        {
            StatusMessage = "Geen herstelbare acties in deze sessie";
            return;
        }

        var confirmation = MessageBox.Show(
            $"Draai {SessionUndoableCount} verplaatsactie(s) van deze app-sessie terug?",
            "Sessie terugdraaien",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusMessage = "Sessie-rollback geannuleerd";
            return;
        }

        var undone = 0;
        var failed = 0;
        foreach (var id in _currentSessionHistoryIds.AsEnumerable().Reverse().ToList())
        {
            var result = await _undoService.UndoAsync(id, CancellationToken.None);
            if (result)
            {
                undone++;
                _currentSessionHistoryIds.Remove(id);
            }
            else
            {
                failed++;
            }
        }

        StatusMessage = failed == 0
            ? $"{undone} acties uit deze sessie teruggedraaid"
            : $"{undone} acties teruggedraaid, {failed} konden niet meer terug";
        await RefreshHistoryAsync();
        RaiseSessionRollbackStateChanged();
    }

    private void RememberSessionEntry(HistoryEntry entry)
    {
        if (entry.Id <= 0 || entry.Status != HistoryStatus.Geslaagd || !entry.CanUndo)
        {
            return;
        }

        if (!_currentSessionHistoryIds.Contains(entry.Id))
        {
            _currentSessionHistoryIds.Add(entry.Id);
            RaiseSessionRollbackStateChanged();
        }
    }

    private void RaiseSessionRollbackStateChanged()
    {
        RaisePropertyChanged(nameof(SessionUndoableCount));
        RollbackCurrentSessionCommand.RaiseCanExecuteChanged();
    }

    private async Task ScanDownloadsAsync()
    {
        if (IsScanning)
        {
            return;
        }

        IsScanning = true;
        try
        {
            StatusMessage = "Opruimscan bezig...";

            var settings = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);

            var configuredFolder = settings.WatchedFolders
                .Where(f => f.IsEnabled)
                .Select(f => f.Path)
                .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path));

            var downloadsFallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            var folder = configuredFolder;
            if (string.IsNullOrWhiteSpace(folder) && Directory.Exists(downloadsFallback))
            {
                folder = downloadsFallback;
            }

            if (string.IsNullOrWhiteSpace(folder))
            {
                StatusMessage = "Geen scan-map gevonden. Voeg een bestaande map toe in 'Bewaakte mappen' of gebruik je Downloads-map.";
                return;
            }

            StatusMessage = $"Opruimscan bezig in: {folder}";

            var analyses = await _manualScanService.ScanFolderAsync(
                folder,
                CancellationToken.None,
                progress =>
                {
                    RunOnUiThread(() =>
                    {
                        StatusMessage = $"Opruimscan: gezien {progress.SeenFiles}, verwerkt {progress.ProcessedFiles}, voorstellen {progress.ProposedFiles}";
                    });
                });
            foreach (var analysis in analyses)
            {
                AddProposal(analysis);
            }

            UpdateScanSummary(analyses);

            StatusMessage = analyses.Count == 0
                ? "Scan voltooid: geen verwerkbare bestanden gevonden"
                : $"Scan voltooid: {analyses.Count} bestanden geanalyseerd";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scanfout: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task AddRuleAsync()
    {
        if (!CanAddRule())
        {
            return;
        }

        var rule = NormalizeRule(RuleEditor.ToRuleDefinition());

        if (rule.AutoApply && string.IsNullOrWhiteSpace(rule.DestinationFolder))
        {
            StatusMessage = "Automatische regels vereisen een doelmap";
            return;
        }

        if (rule.AutoApply && rule.Priority < 80)
        {
            StatusMessage = "Automatische regels vereisen prioriteit 80 of hoger";
            return;
        }

        await _settingsService.UpsertRuleAsync(rule, CancellationToken.None);
        RuleEditor.Reset();
        SelectedRule = null;
        await RefreshRulesAsync();
        StatusMessage = rule.Id > 0 ? "Regel bijgewerkt" : "Regel opgeslagen";
    }

    private async Task DeleteSelectedRuleAsync()
    {
        if (SelectedRule is null)
        {
            return;
        }

        await _settingsService.DeleteRuleAsync(SelectedRule.Id, CancellationToken.None);
        RuleEditor.Reset();
        SelectedRule = null;
        await RefreshRulesAsync();
        StatusMessage = "Regel verwijderd";
    }

    private async Task RefreshRulesAsync()
    {
        var loaded = await _settingsService.LoadRulesAsync(CancellationToken.None);

        RunOnUiThread(() =>
        {
            Rules.Clear();
            foreach (var rule in loaded)
            {
                Rules.Add(rule);
            }

            RefreshWorkflowInsights();
        });
    }

    private async Task TestRuleAsync()
    {
        if (!CanTestRule())
        {
            StatusMessage = "Geef de regel minimaal een naam voordat je test";
            return;
        }

        IsTestingRule = true;
        RuleTestResults.Clear();
        RuleTestSummary = "Regeltest bezig...";

        try
        {
            var rule = NormalizeRule(RuleEditor.ToRuleDefinition());
            var settings = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
            var folders = GetExistingScanFolders(settings);

            if (folders.Count == 0)
            {
                RuleTestSummary = "Geen bestaande bewaakte map gevonden";
                StatusMessage = "Regeltest gestopt: geen scan-map gevonden";
                return;
            }

            var totalAnalyzed = 0;
            var matches = new List<RuleTestResultViewModel>();

            foreach (var folder in folders)
            {
                StatusMessage = $"Regeltest bezig in: {folder}";
                var analyses = await _manualScanService.ScanFolderAsync(
                    folder,
                    CancellationToken.None,
                    progress =>
                    {
                        RunOnUiThread(() =>
                        {
                            StatusMessage = $"Regeltest: gezien {progress.SeenFiles}, verwerkt {progress.ProcessedFiles}";
                        });
                    });

                totalAnalyzed += analyses.Count;
                foreach (var analysis in analyses)
                {
                    var request = _ruleEngine.TryApplyRules(analysis, [rule]);
                    if (request is not null)
                    {
                        matches.Add(new RuleTestResultViewModel(analysis, request));
                    }
                }
            }

            foreach (var match in matches.Take(100))
            {
                RuleTestResults.Add(match);
            }

            var clipped = matches.Count > RuleTestResults.Count
                ? $" Eerste {RuleTestResults.Count} worden getoond."
                : string.Empty;

            RuleTestSummary = matches.Count == 0
                ? $"Geen matches op {totalAnalyzed} geanalyseerde bestanden."
                : $"{matches.Count} matches op {totalAnalyzed} geanalyseerde bestanden.{clipped}";
            StatusMessage = "Regeltest voltooid zonder bestanden te verplaatsen";
        }
        catch (Exception ex)
        {
            RuleTestSummary = $"Regeltest mislukt: {ex.Message}";
            StatusMessage = $"Regeltest mislukt: {ex.Message}";
        }
        finally
        {
            IsTestingRule = false;
        }
    }

    private bool CanAddRule()
    {
        return !string.IsNullOrWhiteSpace(RuleEditor.Name) && RuleEditor.Priority >= 0;
    }

    private bool CanTestRule()
    {
        return !IsTestingRule && CanAddRule();
    }

    private static RuleDefinition NormalizeRule(RuleDefinition rule)
    {
        var extension = rule.ExtensionEquals;
        if (!string.IsNullOrWhiteSpace(extension) && !extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        return new RuleDefinition
        {
            Id = rule.Id,
            Name = rule.Name,
            ExtensionEquals = extension,
            FileNameContains = rule.FileNameContains,
            SourceFolderContains = rule.SourceFolderContains,
            AutoApply = rule.AutoApply,
            Priority = rule.Priority,
            Category = rule.Category,
            DestinationFolder = rule.DestinationFolder,
            RenameTemplate = rule.RenameTemplate
        };
    }

    private static bool MatchesDefaultRuleTemplate(RuleDefinition rule, RuleDefinition template)
    {
        if (rule.Name.Equals(template.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var legacyName = template.Name switch
        {
            "Auto PDF naar slimme map" => "Auto PDF naar Documenten",
            "Auto JPG naar slimme map" => "Auto afbeeldingen",
            "Auto PNG naar slimme map" => "Auto PNG afbeeldingen",
            _ => null
        };

        return legacyName is not null && rule.Name.Equals(legacyName, StringComparison.OrdinalIgnoreCase);
    }

    private RuleDefinition BuildRuleFromSelectedProposal(bool autoApply)
    {
        var proposal = SelectedProposal ?? throw new InvalidOperationException("Geen voorstel geselecteerd");
        var extensionLabel = string.IsNullOrWhiteSpace(proposal.Analysis.Extension)
            ? "bestand"
            : proposal.Analysis.Extension;

        return new RuleDefinition
        {
            Name = autoApply
                ? $"Auto {proposal.Analysis.SuggestedCategory} {extensionLabel}"
                : $"Onthouden {proposal.Analysis.SuggestedCategory} {extensionLabel}",
            ExtensionEquals = string.IsNullOrWhiteSpace(proposal.Analysis.Extension)
                ? null
                : proposal.Analysis.Extension,
            AutoApply = autoApply,
            Priority = autoApply ? 90 : 80,
            Category = proposal.Analysis.SuggestedCategory,
            DestinationFolder = ShouldUseSmartDestinationForRule(proposal)
                ? null
                : proposal.TargetFolder
        };
    }

    private static bool ShouldUseSmartDestinationForRule(ProposalItemViewModel proposal)
    {
        if (proposal.Analysis.SuggestedCategory is not (FileCategory.Facturen or FileCategory.Afbeeldingen))
        {
            return false;
        }

        return proposal.TargetFolder.Equals(proposal.Analysis.SuggestedDestinationFolder, StringComparison.OrdinalIgnoreCase);
    }

    private void NextOnboardingStep()
    {
        OnboardingStep = Math.Min(4, OnboardingStep + 1);
    }

    private void PreviousOnboardingStep()
    {
        OnboardingStep = Math.Max(0, OnboardingStep - 1);
    }

    private void RestartOnboarding()
    {
        OnboardingStep = 0;
        IsOnboardingVisible = true;
    }

    private async Task FinishOnboardingAsync(bool applySelectedMode)
    {
        _hasCompletedOnboarding = true;

        if (applySelectedMode
            && SelectedOnboardingMode.Equals("Automatisch met basisregels", StringComparison.Ordinal))
        {
            await SetupAutomaticOrganizationAsync();
        }

        await SaveWatchedFoldersAsync();
        await SaveSettingsAsync();
        IsOnboardingVisible = false;
        StatusMessage = applySelectedMode
            ? "DownloadPilot is klaar voor gebruik"
            : "Introductie overgeslagen; instellingen kunnen later worden aangepast";
    }

    private async Task SaveSettingsAsync()
    {
        var confidence = Math.Clamp(SettingsEditor.MinAutoApplyConfidence, 0, 100);
        var retentionDays = Math.Clamp(SettingsEditor.HistoryRetentionDays, 0, 3650);

        var current = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
        var updated = new AppSettings
        {
            WatchedFolders = current.WatchedFolders,
            ProtectedPaths = GetProtectedPathsSnapshot(),
            IgnoredPaths = GetIgnoredPathsSnapshot(),
            ExtraScanPaths = GetExtraScanPathsSnapshot(),
            DefaultDestinationRoot = string.IsNullOrWhiteSpace(SettingsEditor.DefaultDestinationRoot)
                ? current.DefaultDestinationRoot
                : SettingsEditor.DefaultDestinationRoot.Trim(),
            Language = current.Language,
            HasCompletedOnboarding = _hasCompletedOnboarding,
            Theme = string.IsNullOrWhiteSpace(SettingsEditor.Theme) ? "Windows" : SettingsEditor.Theme,
            StartWithWindows = SettingsEditor.StartWithWindows,
            NotificationsEnabled = SettingsEditor.NotificationsEnabled,
            UpdateChecksEnabled = SettingsEditor.UpdateChecksEnabled,
            AutoDownloadUpdates = SettingsEditor.AutoDownloadUpdates,
            OrganizationProfile = string.IsNullOrWhiteSpace(SettingsEditor.OrganizationProfile)
                ? ResolveWorkflowModeFromConfidence(confidence)
                : SettingsEditor.OrganizationProfile.Trim(),
            CleanupSchedule = string.IsNullOrWhiteSpace(SettingsEditor.CleanupSchedule)
                ? "Wekelijks"
                : SettingsEditor.CleanupSchedule.Trim(),
            PermissionNoticeAccepted = SettingsEditor.PermissionNoticeAccepted,
            MinAutoApplyConfidence = confidence,
            AutomaticBackupsEnabled = SettingsEditor.AutomaticBackupsEnabled,
            HistoryRetentionDays = retentionDays,
            StoreDocumentText = SettingsEditor.StoreDocumentText,
            OcrEnabled = SettingsEditor.OcrEnabled,
            HashCheckEnabled = SettingsEditor.HashCheckEnabled
        };

        if (_startupRegistrationService.IsSupported)
        {
            var executablePath = Environment.ProcessPath
                ?? Path.Combine(AppContext.BaseDirectory, "DownloadPilot.App.exe");
            await _startupRegistrationService.SetEnabledAsync(updated.StartWithWindows, executablePath, CancellationToken.None);
        }

        await _settingsService.SaveAsync(updated, CancellationToken.None);
        _appSettings = updated;
        SettingsEditor.MinAutoApplyConfidence = confidence;
        SettingsEditor.HistoryRetentionDays = retentionDays;
        SelectedWorkflowMode = ResolveWorkflowMode(updated);
        RefreshWorkflowInsights();
        RefreshPermissionSummary();

        if (retentionDays > 0)
        {
            await _historyService.DeleteOlderThanAsync(DateTime.Now.AddDays(-retentionDays), CancellationToken.None);
            await RefreshHistoryAsync();
        }

        StatusMessage = "Instellingen opgeslagen";
    }

    private async Task ReloadSettingsAsync()
    {
        _appSettings = await _settingsService.LoadAsync(CancellationToken.None);
        _hasCompletedOnboarding = _appSettings.HasCompletedOnboarding;
        ApplySettingsToEditor(_appSettings);
        SelectedWorkflowMode = ResolveWorkflowMode(_appSettings);
        ApplyWatchedFolders(_appSettings.WatchedFolders);
        ApplyProtectedPaths(_appSettings.ProtectedPaths);
        ApplyIgnoredPaths(_appSettings.IgnoredPaths);
        ApplyExtraScanPaths(_appSettings.ExtraScanPaths);
        RefreshPermissionSummary();
        RefreshWorkflowInsights();
        StatusMessage = "Instellingen opnieuw geladen";
    }

    private void ChooseDefaultDestinationRoot()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Kies standaard doelmap",
            SelectedPath = Directory.Exists(SettingsEditor.DefaultDestinationRoot)
                ? SettingsEditor.DefaultDestinationRoot
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        SettingsEditor.DefaultDestinationRoot = dialog.SelectedPath;
        StatusMessage = "Standaard doelmap aangepast";
    }

    private async Task ExportRulesAsync()
    {
        var rules = await _settingsService.LoadRulesAsync(CancellationToken.None);

        using var dialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = "JSON bestanden (*.json)|*.json",
            FileName = "downloadpilot-regels.json",
            Title = "Regels exporteren"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(dialog.FileName, json);
        StatusMessage = "Regels geëxporteerd";
    }

    private async Task ImportRulesAsync()
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "JSON bestanden (*.json)|*.json",
            Title = "Regels importeren"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var json = await File.ReadAllTextAsync(dialog.FileName);
        var imported = JsonSerializer.Deserialize<List<RuleDefinition>>(json);
        if (imported is null || imported.Count == 0)
        {
            StatusMessage = "Geen regels gevonden in importbestand";
            return;
        }

        foreach (var rule in imported)
        {
            var clone = new RuleDefinition
            {
                Id = 0,
                Name = rule.Name,
                ExtensionEquals = rule.ExtensionEquals,
                FileNameContains = rule.FileNameContains,
                SourceFolderContains = rule.SourceFolderContains,
                AutoApply = rule.AutoApply,
                Priority = rule.Priority,
                Category = rule.Category,
                DestinationFolder = rule.DestinationFolder,
                RenameTemplate = rule.RenameTemplate
            };
            await _settingsService.UpsertRuleAsync(clone, CancellationToken.None);
        }

        await RefreshRulesAsync();
        StatusMessage = $"{imported.Count} regels geïmporteerd";
    }

    private async Task SetupAutomaticOrganizationAsync()
    {
        var current = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
        var downloadsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        var destinationRoot = string.IsNullOrWhiteSpace(SettingsEditor.DefaultDestinationRoot)
            ? current.DefaultDestinationRoot
            : SettingsEditor.DefaultDestinationRoot.Trim();

        var targetFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Documenten"] = Path.Combine(destinationRoot, "Documenten"),
            ["Afbeeldingen"] = Path.Combine(destinationRoot, "Afbeeldingen"),
            ["Archieven"] = Path.Combine(destinationRoot, "Archieven"),
            ["Installers"] = Path.Combine(destinationRoot, "Software", "Installatiebestanden"),
            ["Video"] = Path.Combine(destinationRoot, "Video"),
            ["Muziek"] = Path.Combine(destinationRoot, "Muziek")
        };

        foreach (var folder in targetFolders.Values)
        {
            Directory.CreateDirectory(folder);
        }

        var defaults = new List<RuleDefinition>
        {
            new()
            {
                Name = "Auto PDF naar slimme map",
                ExtensionEquals = ".pdf",
                AutoApply = true,
                Priority = 95,
                Category = FileCategory.Documenten,
                DestinationFolder = null
            },
            new()
            {
                Name = "Auto Office naar Documenten",
                ExtensionEquals = ".docx",
                AutoApply = true,
                Priority = 93,
                Category = FileCategory.Documenten,
                DestinationFolder = targetFolders["Documenten"]
            },
            new()
            {
                Name = "Auto Excel naar Documenten",
                ExtensionEquals = ".xlsx",
                AutoApply = true,
                Priority = 93,
                Category = FileCategory.Documenten,
                DestinationFolder = targetFolders["Documenten"]
            },
            new()
            {
                Name = "Auto JPG naar slimme map",
                ExtensionEquals = ".jpg",
                AutoApply = true,
                Priority = 90,
                Category = FileCategory.Afbeeldingen,
                DestinationFolder = null
            },
            new()
            {
                Name = "Auto PNG naar slimme map",
                ExtensionEquals = ".png",
                AutoApply = true,
                Priority = 90,
                Category = FileCategory.Afbeeldingen,
                DestinationFolder = null
            },
            new()
            {
                Name = "Auto JPEG naar slimme map",
                ExtensionEquals = ".jpeg",
                AutoApply = true,
                Priority = 90,
                Category = FileCategory.Afbeeldingen,
                DestinationFolder = null
            },
            new()
            {
                Name = "Auto WebP naar slimme map",
                ExtensionEquals = ".webp",
                AutoApply = true,
                Priority = 88,
                Category = FileCategory.Afbeeldingen,
                DestinationFolder = null
            },
            new()
            {
                Name = "Auto GIF naar slimme map",
                ExtensionEquals = ".gif",
                AutoApply = true,
                Priority = 88,
                Category = FileCategory.Afbeeldingen,
                DestinationFolder = null
            },
            new()
            {
                Name = "Auto HEIC naar slimme map",
                ExtensionEquals = ".heic",
                AutoApply = true,
                Priority = 88,
                Category = FileCategory.Afbeeldingen,
                DestinationFolder = null
            },
            new()
            {
                Name = "Auto archieven",
                ExtensionEquals = ".zip",
                AutoApply = true,
                Priority = 90,
                Category = FileCategory.Archieven,
                DestinationFolder = targetFolders["Archieven"]
            },
            new()
            {
                Name = "Auto installers exe",
                ExtensionEquals = ".exe",
                AutoApply = true,
                Priority = 90,
                Category = FileCategory.Installatiebestanden,
                DestinationFolder = targetFolders["Installers"]
            },
            new()
            {
                Name = "Auto installers msi",
                ExtensionEquals = ".msi",
                AutoApply = true,
                Priority = 90,
                Category = FileCategory.Installatiebestanden,
                DestinationFolder = targetFolders["Installers"]
            },
            new()
            {
                Name = "Auto videos",
                ExtensionEquals = ".mp4",
                AutoApply = true,
                Priority = 88,
                Category = FileCategory.Videos,
                DestinationFolder = targetFolders["Video"]
            },
            new()
            {
                Name = "Auto muziek",
                ExtensionEquals = ".mp3",
                AutoApply = true,
                Priority = 88,
                Category = FileCategory.Muziek,
                DestinationFolder = targetFolders["Muziek"]
            }
        };

        var existing = await _settingsService.LoadRulesAsync(CancellationToken.None);
        foreach (var template in defaults)
        {
            var existingRule = existing.FirstOrDefault(r =>
                MatchesDefaultRuleTemplate(r, template));

            var ruleToSave = existingRule is null
                ? template
                : new RuleDefinition
                {
                    Id = existingRule.Id,
                    Name = template.Name,
                    ExtensionEquals = template.ExtensionEquals,
                    FileNameContains = template.FileNameContains,
                    SourceFolderContains = template.SourceFolderContains,
                    AutoApply = template.AutoApply,
                    Priority = template.Priority,
                    Category = template.Category,
                    DestinationFolder = template.DestinationFolder,
                    RenameTemplate = template.RenameTemplate
                };

            await _settingsService.UpsertRuleAsync(ruleToSave, CancellationToken.None);
        }

        var watchedFolders = current.WatchedFolders
            .Select(f => new WatchedFolder { Path = f.Path, IsEnabled = f.IsEnabled })
            .ToList();

        var existingDownloadsIndex = watchedFolders.FindIndex(f =>
            f.Path.Equals(downloadsRoot, StringComparison.OrdinalIgnoreCase));

        if (existingDownloadsIndex < 0)
        {
            watchedFolders.Add(new WatchedFolder { Path = downloadsRoot, IsEnabled = true });
        }
        else
        {
            watchedFolders[existingDownloadsIndex] = new WatchedFolder
            {
                Path = downloadsRoot,
                IsEnabled = true
            };
        }

        var updated = new AppSettings
        {
            WatchedFolders = watchedFolders,
            ProtectedPaths = current.ProtectedPaths,
            IgnoredPaths = current.IgnoredPaths,
            ExtraScanPaths = current.ExtraScanPaths,
            DefaultDestinationRoot = destinationRoot,
            Language = current.Language,
            HasCompletedOnboarding = _hasCompletedOnboarding,
            Theme = current.Theme,
            StartWithWindows = current.StartWithWindows,
            NotificationsEnabled = current.NotificationsEnabled,
            UpdateChecksEnabled = current.UpdateChecksEnabled,
            AutoDownloadUpdates = current.AutoDownloadUpdates,
            OrganizationProfile = "Normale modus",
            CleanupSchedule = current.CleanupSchedule,
            PermissionNoticeAccepted = current.PermissionNoticeAccepted,
            MinAutoApplyConfidence = Math.Min(current.MinAutoApplyConfidence, 85),
            AutomaticBackupsEnabled = current.AutomaticBackupsEnabled,
            HistoryRetentionDays = current.HistoryRetentionDays,
            StoreDocumentText = current.StoreDocumentText,
            OcrEnabled = current.OcrEnabled,
            HashCheckEnabled = current.HashCheckEnabled
        };

        await _settingsService.SaveAsync(updated, CancellationToken.None);
        _appSettings = updated;
        ApplyWatchedFolders(updated.WatchedFolders);

        await RefreshRulesAsync();

        StatusMessage = "Auto-instellen gereed: standaardmappen en automatische regels zijn geactiveerd";
    }

    private void RefreshSmartWorkflow()
    {
        RefreshWorkflowInsights();
        StatusMessage = SmartInboxItems.Count == 0
            ? "Slimme inbox bijgewerkt: niets open"
            : $"Slimme inbox bijgewerkt: {SmartInboxItems.Count} aandachtspunten";
    }

    private async Task ProcessSafeQueueAsync()
    {
        var proposals = NewFileProposals
            .Where(IsSafeQueueCandidate)
            .ToList();

        if (proposals.Count == 0)
        {
            StatusMessage = "Geen veilige wachtrij-items gevonden";
            return;
        }

        var actionLabel = SelectedWorkflowMode.Equals("Snelheidsmodus", StringComparison.OrdinalIgnoreCase)
            ? "Snelheidswachtrij"
            : "Veilige wachtrij";
        await ProcessProposalBatchAsync(proposals, actionLabel);
    }

    private async Task ApplySelectedWorkflowModeAsync()
    {
        SettingsEditor.OrganizationProfile = SelectedWorkflowMode;

        if (SelectedWorkflowMode.Equals("Alleen advies", StringComparison.OrdinalIgnoreCase))
        {
            SettingsEditor.MinAutoApplyConfidence = 100;
            await SaveSettingsAsync();
            RefreshWorkflowInsights();
            StatusMessage = "Alleen advies actief: DownloadPilot doet voorstellen, maar voert niets automatisch uit";
            return;
        }

        if (SelectedWorkflowMode.Equals("Snelheidsmodus", StringComparison.OrdinalIgnoreCase))
        {
            SettingsEditor.MinAutoApplyConfidence = 80;
            await SetupAutomaticOrganizationAsync();
            await SaveSettingsAsync();
            RefreshWorkflowInsights();
            StatusMessage = "Snelheidsmodus actief: basisregels staan klaar en de wachtrij gebruikt 80% als grens";
            return;
        }

        if (SelectedWorkflowMode.Equals("Normale modus", StringComparison.OrdinalIgnoreCase))
        {
            SettingsEditor.MinAutoApplyConfidence = 85;
            await SetupAutomaticOrganizationAsync();
            await SaveSettingsAsync();
            RefreshWorkflowInsights();
            StatusMessage = "Normale modus actief: basisregels en 85% betrouwbaarheid";
            return;
        }

        SettingsEditor.MinAutoApplyConfidence = 95;
        await SaveSettingsAsync();
        RefreshWorkflowInsights();
        StatusMessage = "Veilige modus actief: automatische acties vragen minimaal 95% betrouwbaarheid";
    }

    private async Task ApplyRuleSuggestionsAsync()
    {
        var selected = RuleSuggestions
            .Where(suggestion => suggestion.IsSelected)
            .ToList();

        if (selected.Count == 0)
        {
            StatusMessage = "Geen leersuggesties aangevinkt";
            return;
        }

        var existingRules = (await _settingsService.LoadRulesAsync(CancellationToken.None)).ToList();
        var saved = 0;
        var skipped = 0;

        foreach (var suggestion in selected)
        {
            var rule = NormalizeRule(suggestion.ToRuleDefinition());
            if (existingRules.Any(existing => IsSimilarRule(existing, rule)))
            {
                skipped++;
                continue;
            }

            await _settingsService.UpsertRuleAsync(rule, CancellationToken.None);
            existingRules.Add(rule);
            saved++;
        }

        await RefreshRulesAsync();
        StatusMessage = skipped == 0
            ? $"{saved} leersuggesties opgeslagen als regel"
            : $"{saved} leersuggesties opgeslagen, {skipped} bestonden al";
    }

    private void RefreshWorkflowInsights()
    {
        RunOnUiThread(() =>
        {
            var proposals = NewFileProposals.ToList();
            var duplicates = DuplicateProposals.ToList();
            var duplicateSet = duplicates.ToHashSet();
            var threshold = GetWorkflowConfidenceThreshold();

            SmartInboxItems.Clear();
            ActionQueueItems.Clear();
            FolderPreviewItems.Clear();
            DuplicateGroups.Clear();
            ClearRuleSuggestions();
            RuleScores.Clear();

            foreach (var item in BuildSmartInboxItems(proposals, duplicates, duplicateSet, threshold))
            {
                SmartInboxItems.Add(item);
            }

            foreach (var item in proposals
                         .OrderBy(proposal => duplicateSet.Contains(proposal) ? 1 : 0)
                         .ThenByDescending(proposal => proposal.Confidence)
                         .Take(100))
            {
                ActionQueueItems.Add(new ActionQueueItemViewModel
                {
                    Action = duplicateSet.Contains(item) ? "Niet automatisch" : "Verplaatsen",
                    FileName = item.OriginalFileName,
                    TargetFolder = item.TargetFolder,
                    TargetFileName = item.TargetFileName,
                    Confidence = item.Confidence,
                    Status = duplicateSet.Contains(item)
                        ? "Duplicaat"
                        : IsSafeQueueCandidate(item)
                            ? "Gereed"
                            : "Controle"
                });
            }

            foreach (var item in BuildFolderPreviewItems(proposals))
            {
                FolderPreviewItems.Add(item);
            }

            foreach (var item in BuildDuplicateGroups(duplicates))
            {
                DuplicateGroups.Add(item);
            }

            foreach (var suggestion in BuildRuleSuggestions(proposals, duplicateSet))
            {
                suggestion.PropertyChanged += OnRuleSuggestionPropertyChanged;
                RuleSuggestions.Add(suggestion);
            }

            foreach (var item in BuildRuleScores(proposals))
            {
                RuleScores.Add(item);
            }

            UpdateCleanupReport(proposals, duplicates);
            RaiseWorkflowStateChanged();
        });
    }

    private IEnumerable<SmartInboxItemViewModel> BuildSmartInboxItems(
        IReadOnlyList<ProposalItemViewModel> proposals,
        IReadOnlyList<ProposalItemViewModel> duplicates,
        HashSet<ProposalItemViewModel> duplicateSet,
        int threshold)
    {
        foreach (var duplicate in duplicates.OrderByDescending(proposal => proposal.FileSizeBytes).Take(20))
        {
            yield return new SmartInboxItemViewModel
            {
                Type = "Duplicaat",
                Title = duplicate.OriginalFileName,
                Detail = $"Mogelijk dubbel bestand van {duplicate.FileSizeReadable}. Controleer eerst welke versie je wilt houden.",
                Action = "Bekijk duplicate-groep",
                TargetFolder = duplicate.SourceFolder,
                Severity = "Controle",
                Confidence = duplicate.Confidence
            };
        }

        foreach (var proposal in proposals
                     .Where(proposal => !duplicateSet.Contains(proposal))
                     .OrderBy(proposal => proposal.Confidence >= threshold ? 1 : 0)
                     .ThenBy(proposal => proposal.Confidence)
                     .Take(60))
        {
            var isSmartPhoto = IsSmartPhotoFolder(proposal);
            var isSmartInvoice = IsSmartInvoiceFolder(proposal);
            var needsReview = proposal.Confidence < threshold || proposal.Analysis.SuggestedCategory == FileCategory.Overig;

            yield return new SmartInboxItemViewModel
            {
                Type = isSmartPhoto
                    ? "Foto-map"
                    : isSmartInvoice
                        ? "Factuur-map"
                        : "Voorstel",
                Title = proposal.OriginalFileName,
                Detail = BuildSmartInboxDetail(proposal, isSmartPhoto, isSmartInvoice),
                Action = needsReview ? "Handmatig controleren" : "Kan via wachtrij",
                TargetFolder = proposal.TargetFolder,
                Severity = needsReview ? "Check" : "Klaar",
                Confidence = proposal.Confidence
            };
        }
    }

    private static IEnumerable<FolderPreviewItemViewModel> BuildFolderPreviewItems(IReadOnlyList<ProposalItemViewModel> proposals)
    {
        return proposals
            .Where(proposal => !string.IsNullOrWhiteSpace(proposal.TargetFolder))
            .GroupBy(proposal => proposal.TargetFolder.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
            .Take(24)
            .Select(group => new FolderPreviewItemViewModel
            {
                FolderName = GetFolderDisplayName(group.Key),
                FolderPath = group.Key,
                FileCount = group.Count(),
                TotalSizeBytes = group.Sum(proposal => proposal.FileSizeBytes),
                ExampleFiles = string.Join(", ", group.Take(3).Select(proposal => proposal.OriginalFileName))
            });
    }

    private static IEnumerable<DuplicateGroupViewModel> BuildDuplicateGroups(IReadOnlyList<ProposalItemViewModel> duplicates)
    {
        return duplicates
            .GroupBy(proposal => $"{proposal.Analysis.Extension}|{proposal.FileSizeBytes}")
            .OrderByDescending(group => group.Sum(proposal => proposal.FileSizeBytes))
            .ThenByDescending(group => group.Count())
            .Take(24)
            .Select(group =>
            {
                var first = group.First();
                var extension = string.IsNullOrWhiteSpace(first.Analysis.Extension)
                    ? "bestand"
                    : first.Analysis.Extension.TrimStart('.').ToUpperInvariant();

                return new DuplicateGroupViewModel
                {
                    GroupName = $"{extension} - {first.FileSizeReadable}",
                    FileCount = group.Count(),
                    TotalSizeBytes = group.Sum(proposal => proposal.FileSizeBytes),
                    SuggestedAction = group.Count() > 1
                        ? $"Behoud 1 bestand; controleer {group.Count() - 1} duplicaten"
                        : "Exacte match gevonden; controleer voor opruimen",
                    ExampleFiles = string.Join(", ", group.Take(3).Select(proposal => proposal.OriginalFileName))
                };
            });
    }

    private IEnumerable<RuleSuggestionViewModel> BuildRuleSuggestions(
        IReadOnlyList<ProposalItemViewModel> proposals,
        HashSet<ProposalItemViewModel> duplicateSet)
    {
        var existingRules = Rules.ToList();

        return proposals
            .Where(proposal => !duplicateSet.Contains(proposal))
            .Where(proposal => !string.IsNullOrWhiteSpace(proposal.Analysis.Extension))
            .Select(proposal => new
            {
                Proposal = proposal,
                FixedDestination = ShouldUseSmartDestinationForRule(proposal) ? null : proposal.TargetFolder
            })
            .GroupBy(item =>
                $"{item.Proposal.Analysis.Extension}|{item.Proposal.Analysis.SuggestedCategory}|{item.FixedDestination ?? "<smart>"}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                var extension = first.Proposal.Analysis.Extension;
                var category = first.Proposal.Analysis.SuggestedCategory;
                var confidence = (int)Math.Round(group.Average(item => item.Proposal.Confidence));
                var fixedDestination = first.FixedDestination;
                var displayDestination = fixedDestination ?? "Slimme map per inhoud";
                var name = fixedDestination is null
                    ? $"Leer {category} {extension} slim"
                    : $"Leer {category} {extension} naar {GetFolderDisplayName(fixedDestination)}";

                return new RuleSuggestionViewModel
                {
                    Name = name,
                    Extension = extension,
                    Category = category,
                    DestinationFolder = displayDestination,
                    FixedDestinationFolder = fixedDestination,
                    MatchCount = group.Count(),
                    Confidence = confidence,
                    Reason = group.Count() == 1
                        ? "Sterke losse match"
                        : $"Patroon gevonden in {group.Count()} voorstellen"
                };
            })
            .Where(suggestion => suggestion.MatchCount >= 2 || suggestion.Confidence >= 92)
            .Where(suggestion => !existingRules.Any(rule => IsSimilarRule(rule, suggestion.ToRuleDefinition())))
            .OrderByDescending(suggestion => suggestion.MatchCount)
            .ThenByDescending(suggestion => suggestion.Confidence)
            .Take(8);
    }

    private IEnumerable<RuleScoreViewModel> BuildRuleScores(IReadOnlyList<ProposalItemViewModel> proposals)
    {
        return Rules
            .OrderByDescending(rule => rule.Priority)
            .Take(30)
            .Select(rule =>
            {
                var matches = proposals.Count(proposal => RuleMatchesProposal(proposal, rule));
                var hasCriteria = !string.IsNullOrWhiteSpace(rule.ExtensionEquals)
                    || !string.IsNullOrWhiteSpace(rule.FileNameContains)
                    || !string.IsNullOrWhiteSpace(rule.SourceFolderContains);
                var hasSmartDestination = string.IsNullOrWhiteSpace(rule.DestinationFolder)
                    && rule.Category is FileCategory.Facturen or FileCategory.Afbeeldingen;
                var score = 45
                    + Math.Min(matches * 12, 30)
                    + (rule.AutoApply ? 10 : 0)
                    + (rule.Priority >= 80 ? 10 : 0)
                    + (!string.IsNullOrWhiteSpace(rule.DestinationFolder) || hasSmartDestination ? 10 : 0)
                    - (hasCriteria ? 0 : 20);
                score = Math.Clamp(score, 0, 100);

                var health = !hasCriteria
                    ? "Breed"
                    : matches > 0
                        ? "Actief"
                        : rule.AutoApply
                            ? "Rustig"
                            : "Handmatig";

                return new RuleScoreViewModel
                {
                    RuleName = rule.Name,
                    Score = score,
                    Matches = matches,
                    Health = health,
                    Detail = BuildRuleScoreDetail(rule, matches, hasSmartDestination)
                };
            });
    }

    private void UpdateCleanupReport(
        IReadOnlyList<ProposalItemViewModel> proposals,
        IReadOnlyList<ProposalItemViewModel> duplicates)
    {
        if (proposals.Count == 0)
        {
            CleanupReportTitle = "Alles rustig";
            CleanupReportText = "Er staan geen open voorstellen. Start een scan of laat de bewaking aanstaan om nieuwe bestanden automatisch te analyseren.";
            return;
        }

        var safe = proposals.Count(IsSafeQueueCandidate);
        var review = Math.Max(0, proposals.Count - safe);
        var smartFolders = proposals.Count(proposal => IsSmartPhotoFolder(proposal) || IsSmartInvoiceFolder(proposal));
        var duplicateSavings = FormatBytes(duplicates.Sum(proposal => proposal.FileSizeBytes));

        CleanupReportTitle = $"{proposals.Count} bestanden in beeld";
        CleanupReportText = $"{safe} kunnen direct via de wachtrij, {review} vragen controle. " +
            $"{FolderPreviewItems.Count} doelmappen worden geraakt, {smartFolders} bestanden krijgen een slimme inhoudsmap. " +
            $"Duplicaten kunnen ongeveer {duplicateSavings} besparen na bevestiging.";
    }

    private void RaiseWorkflowStateChanged()
    {
        RaisePropertyChanged(nameof(SmartInboxCount));
        RaisePropertyChanged(nameof(ActionQueueCount));
        RaisePropertyChanged(nameof(SafeQueueCount));
        RaisePropertyChanged(nameof(ReviewQueueCount));
        RaisePropertyChanged(nameof(FolderPreviewCount));
        RaisePropertyChanged(nameof(RuleSuggestionCount));
        RaisePropertyChanged(nameof(SelectedRuleSuggestionCount));
        RaisePropertyChanged(nameof(DuplicateGroupCount));
        RaisePropertyChanged(nameof(SmartPhotoFolderCount));
        RaisePropertyChanged(nameof(PotentialDuplicateSavingsReadable));
        ProcessSafeQueueCommand.RaiseCanExecuteChanged();
        ApplyRuleSuggestionsCommand.RaiseCanExecuteChanged();
    }

    private void ClearRuleSuggestions()
    {
        foreach (var suggestion in RuleSuggestions)
        {
            suggestion.PropertyChanged -= OnRuleSuggestionPropertyChanged;
        }

        RuleSuggestions.Clear();
    }

    private void OnRuleSuggestionPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(RuleSuggestionViewModel.IsSelected))
        {
            RaisePropertyChanged(nameof(SelectedRuleSuggestionCount));
            ApplyRuleSuggestionsCommand.RaiseCanExecuteChanged();
        }
    }

    private bool IsSafeQueueCandidate(ProposalItemViewModel proposal)
    {
        return !IsDuplicateProposal(proposal)
            && proposal.Confidence >= GetWorkflowConfidenceThreshold()
            && !string.IsNullOrWhiteSpace(proposal.TargetFolder)
            && !string.IsNullOrWhiteSpace(proposal.TargetFileName);
    }

    private int GetWorkflowConfidenceThreshold()
    {
        return SelectedWorkflowMode switch
        {
            "Alleen advies" => 101,
            "Snelheidsmodus" => 80,
            "Normale modus" => 85,
            _ => 95
        };
    }

    private static string ResolveWorkflowMode(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OrganizationProfile)
            && settings.OrganizationProfile is "Alleen advies" or "Veilige modus" or "Normale modus" or "Snelheidsmodus")
        {
            return settings.OrganizationProfile;
        }

        return ResolveWorkflowModeFromConfidence(settings.MinAutoApplyConfidence);
    }

    private static string ResolveWorkflowModeFromConfidence(int confidence)
    {
        return confidence switch
        {
            >= 100 => "Alleen advies",
            >= 95 => "Veilige modus",
            <= 80 => "Snelheidsmodus",
            _ => "Normale modus"
        };
    }

    private bool IsDuplicateProposal(ProposalItemViewModel proposal)
    {
        return DuplicateProposals.Contains(proposal)
            || proposal.Reason.Contains("duplicaat", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSmartPhotoFolder(ProposalItemViewModel proposal)
    {
        return proposal.Analysis.SuggestedCategory == FileCategory.Afbeeldingen
            && HasCategorySubfolder(proposal.TargetFolder, FileCategory.Afbeeldingen);
    }

    private static bool IsSmartInvoiceFolder(ProposalItemViewModel proposal)
    {
        return proposal.Analysis.SuggestedCategory == FileCategory.Facturen
            && HasCategorySubfolder(proposal.TargetFolder, FileCategory.Facturen);
    }

    private static bool HasCategorySubfolder(string targetFolder, FileCategory category)
    {
        var parts = targetFolder.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        var categoryIndex = Array.FindLastIndex(parts, part =>
            part.Equals(category.ToString(), StringComparison.OrdinalIgnoreCase));
        return categoryIndex >= 0 && categoryIndex < parts.Length - 1;
    }

    private static string BuildSmartInboxDetail(
        ProposalItemViewModel proposal,
        bool isSmartPhoto,
        bool isSmartInvoice)
    {
        if (isSmartPhoto)
        {
            return $"Foto wordt geplaatst in {GetFolderDisplayName(proposal.TargetFolder)} op basis van beeld/OCR/naam.";
        }

        if (isSmartInvoice)
        {
            return $"Factuur wordt geplaatst in {GetFolderDisplayName(proposal.TargetFolder)} op basis van herkende bedrijfsnaam.";
        }

        return proposal.Reason;
    }

    private static string BuildRuleScoreDetail(RuleDefinition rule, int matches, bool hasSmartDestination)
    {
        var destination = hasSmartDestination
            ? "slimme doelmap"
            : string.IsNullOrWhiteSpace(rule.DestinationFolder)
                ? "geen vaste doelmap"
                : GetFolderDisplayName(rule.DestinationFolder);
        var mode = rule.AutoApply ? "automatisch" : "handmatig";
        return $"{matches} huidige matches, {mode}, {destination}";
    }

    private static bool RuleMatchesProposal(ProposalItemViewModel proposal, RuleDefinition rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.ExtensionEquals)
            && !proposal.Analysis.Extension.Equals(rule.ExtensionEquals, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.FileNameContains)
            && !proposal.OriginalFileName.Contains(rule.FileNameContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.SourceFolderContains)
            && !proposal.SourceFolder.Contains(rule.SourceFolderContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool IsSimilarRule(RuleDefinition existing, RuleDefinition candidate)
    {
        return string.Equals(existing.ExtensionEquals, candidate.ExtensionEquals, StringComparison.OrdinalIgnoreCase)
            && string.Equals(existing.FileNameContains, candidate.FileNameContains, StringComparison.OrdinalIgnoreCase)
            && string.Equals(existing.SourceFolderContains, candidate.SourceFolderContains, StringComparison.OrdinalIgnoreCase)
            && existing.Category == candidate.Category
            && string.Equals(existing.DestinationFolder, candidate.DestinationFolder, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFolderDisplayName(string folder)
    {
        var trimmed = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(name) ? trimmed : name;
    }

    private void UpdateWorkflowModeSummary()
    {
        WorkflowModeSummary = SelectedWorkflowMode switch
        {
            "Alleen advies" => "Alleen advies: DownloadPilot scant en stelt voor, maar voert niets automatisch uit.",
            "Snelheidsmodus" => "Snelheidsmodus: basisregels en 80% betrouwbaarheid maken sneller verwerken mogelijk.",
            "Normale modus" => "Normale modus: basisregels met 85% betrouwbaarheid, geschikt voor dagelijks gebruik.",
            _ => "Veilige modus: alleen voorstellen met zeer hoge betrouwbaarheid komen in de snelle wachtrij."
        };
        RefreshWorkflowInsights();
    }

    private void ApplyMailProviderPreset()
    {
        switch (SelectedMailProvider)
        {
            case "Gmail":
                MailImapHost = "imap.gmail.com";
                MailImapPort = 993;
                MailUseSsl = true;
                MailFolderName = "INBOX";
                MailSpamFolderName = "[Gmail]/Spam";
                MailProviderHelpText = "Gmail: gebruik een app-wachtwoord voor accounts met tweestapsverificatie. DownloadPilot bewaart dit niet.";
                MailStatusMessage = "Gmail preset toegepast";
                break;

            case "Hotmail / Outlook":
                MailImapHost = "outlook.office365.com";
                MailImapPort = 993;
                MailUseSsl = true;
                MailFolderName = "INBOX";
                MailSpamFolderName = "Junk Email";
                MailProviderHelpText = "Hotmail/Outlook: Microsoft vereist meestal OAuth. Vul hier een OAuth access token in; een normale wachtwoord-login kan weigeren.";
                MailStatusMessage = "Outlook preset toegepast; OAuth token kan vereist zijn";
                break;

            default:
                MailImapHost = string.Empty;
                MailImapPort = 993;
                MailUseSsl = true;
                MailFolderName = "INBOX";
                MailSpamFolderName = string.Empty;
                MailProviderHelpText = "Eigen IMAP: vul host, poort, gebruikersnaam en app-wachtwoord/token in.";
                MailStatusMessage = "Eigen IMAP preset toegepast";
                break;
        }
    }

    private async Task ScanMailSpamAsync()
    {
        if (IsMailScanning)
        {
            return;
        }

        IsMailScanning = true;
        MailStatusMessage = "Mailbox wordt gescand...";

        try
        {
            var result = await _mailSpamFilterService.ScanAsync(BuildMailConnectionSettings(), CancellationToken.None);

            foreach (var item in MailSpamMessages)
            {
                item.PropertyChanged -= OnMailSpamItemPropertyChanged;
            }

            MailSpamMessages.Clear();
            foreach (var candidate in result.Candidates)
            {
                var item = new MailSpamItemViewModel(candidate);
                item.PropertyChanged += OnMailSpamItemPropertyChanged;
                MailSpamMessages.Add(item);
            }

            MailScannedCount = result.ScannedCount;
            MailStatusMessage = result.CandidateCount == 0
                ? $"Scan klaar: {result.ScannedCount} mails bekeken, geen duidelijke spam gevonden"
                : $"Scan klaar: {result.CandidateCount} verdachte mails gevonden, {result.HighConfidenceCount} hoog risico";
        }
        catch (Exception ex)
        {
            MailStatusMessage = $"Mail-scan mislukt: {SimplifyMailError(ex.Message)}";
        }
        finally
        {
            IsMailScanning = false;
            RaiseMailSpamStateChanged();
        }
    }

    private void SelectHighConfidenceMailSpam()
    {
        foreach (var item in MailSpamMessages)
        {
            item.IsSelected = item.SpamScore >= 80;
        }

        MailStatusMessage = $"{SelectedMailSpamCount} hoog-risico mails aangevinkt";
    }

    private void ClearMailSpamSelection()
    {
        foreach (var item in MailSpamMessages)
        {
            item.IsSelected = false;
        }

        MailStatusMessage = "Mailselectie gewist";
    }

    private async Task MoveSelectedMailSpamAsync()
    {
        var selected = MailSpamMessages
            .Where(item => item.IsSelected)
            .ToList();

        await MoveMailSpamAsync(selected, "Geselecteerde spam");
    }

    private async Task MoveAllMailSpamAsync()
    {
        await MoveMailSpamAsync(MailSpamMessages.ToList(), "Alle spamkandidaten");
    }

    private async Task MoveMailSpamAsync(IReadOnlyList<MailSpamItemViewModel> items, string label)
    {
        if (items.Count == 0)
        {
            MailStatusMessage = "Geen mails geselecteerd";
            return;
        }

        var confirmation = MessageBox.Show(
            $"Verplaats {items.Count} mails naar de spam-/junkmap? De mails worden niet verwijderd.",
            "Spam filteren",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            MailStatusMessage = "Mailactie geannuleerd";
            return;
        }

        IsMailScanning = true;
        MailStatusMessage = $"{label} wordt verplaatst...";

        try
        {
            var moved = await _mailSpamFilterService.MoveToSpamAsync(
                BuildMailConnectionSettings(),
                items.Select(item => item.Candidate).ToList(),
                CancellationToken.None);

            foreach (var item in items)
            {
                item.PropertyChanged -= OnMailSpamItemPropertyChanged;
                MailSpamMessages.Remove(item);
            }

            MailStatusMessage = $"{moved} mails naar spam/junk verplaatst";
        }
        catch (Exception ex)
        {
            MailStatusMessage = $"Verplaatsen mislukt: {SimplifyMailError(ex.Message)}";
        }
        finally
        {
            IsMailScanning = false;
            RaiseMailSpamStateChanged();
        }
    }

    private MailConnectionSettings BuildMailConnectionSettings()
    {
        return new MailConnectionSettings
        {
            Provider = SelectedMailProvider,
            EmailAddress = MailAddress.Trim(),
            UserName = string.IsNullOrWhiteSpace(MailUserName) ? MailAddress.Trim() : MailUserName.Trim(),
            Password = MailPassword,
            ImapHost = MailImapHost.Trim(),
            ImapPort = Math.Clamp(MailImapPort, 1, 65535),
            UseSsl = MailUseSsl,
            FolderName = string.IsNullOrWhiteSpace(MailFolderName) ? "INBOX" : MailFolderName.Trim(),
            SpamFolderName = MailSpamFolderName.Trim(),
            MaxMessages = Math.Clamp(MailMaxMessages, 1, 500)
        };
    }

    private void OnMailSpamItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MailSpamItemViewModel.IsSelected))
        {
            RaiseMailSpamStateChanged();
        }
    }

    private void RaiseMailSpamStateChanged()
    {
        RaisePropertyChanged(nameof(MailSpamCandidateCount));
        RaisePropertyChanged(nameof(HighConfidenceMailSpamCount));
        RaisePropertyChanged(nameof(SelectedMailSpamCount));
        ScanMailSpamCommand.RaiseCanExecuteChanged();
        SelectHighConfidenceMailSpamCommand.RaiseCanExecuteChanged();
        ClearMailSpamSelectionCommand.RaiseCanExecuteChanged();
        MoveSelectedMailSpamCommand.RaiseCanExecuteChanged();
        MoveAllMailSpamCommand.RaiseCanExecuteChanged();
    }

    private static string SimplifyMailError(string message)
    {
        if (message.Contains("Authentication", StringComparison.OrdinalIgnoreCase)
            || message.Contains("AUTHENTICATE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Invalid credentials", StringComparison.OrdinalIgnoreCase))
        {
            return "inloggen geweigerd. Gebruik bij Gmail een app-wachtwoord; bij Outlook/Hotmail is vaak OAuth nodig.";
        }

        return message;
    }

    private void GenerateNaturalRule()
    {
        try
        {
            var destinationRoot = SettingsEditor.DefaultDestinationRoot;
            if (string.IsNullOrWhiteSpace(destinationRoot))
            {
                destinationRoot = _appSettings?.DefaultDestinationRoot
                    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DownloadPilot");
            }

            var result = NaturalLanguageRuleBuilder.Build(NaturalRuleInstruction, destinationRoot);
            RuleEditor.LoadFrom(result.Rule);
            NaturalRuleFeedback = result.Feedback;
            ToolStatusMessage = "Lokale AI-regelmaker heeft de regel ingevuld bij Regels";
        }
        catch (Exception ex)
        {
            NaturalRuleFeedback = ex.Message;
            ToolStatusMessage = "Regelmaker kon de tekst niet omzetten";
        }
    }

    private async Task ExportInvoicesCsvAsync()
    {
        var records = BuildInvoiceRecords().ToList();
        if (records.Count == 0)
        {
            InvoiceExportStatus = "Geen facturen of bonnetjes gevonden in voorstellen/geschiedenis";
            ToolStatusMessage = InvoiceExportStatus;
            return;
        }

        using var dialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = "CSV bestanden (*.csv)|*.csv",
            FileName = $"downloadpilot-facturen-{DateTime.Now:yyyy-MM-dd}.csv",
            Title = "Facturen exporteren"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            InvoiceExportStatus = "Factuurexport geannuleerd";
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Datum;Bedrijf;Bedrag;Bestand;Doelmap");
        foreach (var record in records)
        {
            builder.AppendLine(string.Join(
                ';',
                EscapeCsv(record.Date.ToString("yyyy-MM-dd")),
                EscapeCsv(record.Company),
                EscapeCsv(record.Amount <= 0 ? string.Empty : record.Amount.ToString("0.00")),
                EscapeCsv(record.FileName),
                EscapeCsv(record.TargetFolder)));
        }

        await File.WriteAllTextAsync(dialog.FileName, builder.ToString(), Encoding.UTF8);
        InvoiceExportStatus = $"{records.Count} factuurregels geexporteerd naar {dialog.FileName}";
        ToolStatusMessage = "Factuur-export klaar";
    }

    private void ApplySmartNameRepair()
    {
        var changed = 0;
        foreach (var proposal in NewFileProposals.Where(NeedsSmartNameRepair))
        {
            proposal.TargetFileName = proposal.Analysis.SuggestedFileName;
            changed++;
        }

        RefreshSmartToolInsights();
        ToolStatusMessage = changed == 0
            ? "Geen slechte bestandsnamen gevonden"
            : $"{changed} slimme namen opnieuw toegepast op open voorstellen";
    }

    private void RefreshSmartTools()
    {
        RefreshSmartToolInsights();
        ToolStatusMessage = "Slimme tools bijgewerkt";
    }

    public async Task CheckForUpdatesOnStartupAsync()
    {
        var settings = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
        if (!settings.UpdateChecksEnabled)
        {
            UpdateStatus = "Automatische updatecheck staat uit";
            return;
        }

        await CheckForUpdatesAsync(showUpToDateMessage: false);
    }

    private async Task CheckForUpdatesManuallyAsync()
    {
        await CheckForUpdatesAsync(showUpToDateMessage: true);
    }

    private async Task CheckForUpdatesAsync(bool showUpToDateMessage)
    {
        UpdateStatus = "GitHub wordt gecontroleerd op nieuwe releases...";
        try
        {
            var result = await _updateService.CheckLatestAsync(CancellationToken.None);
            UpdateStatus = result.Message ?? "Updatecheck afgerond";
            ToolStatusMessage = UpdateStatus;

            if (!result.IsUpdateAvailable)
            {
                if (showUpToDateMessage)
                {
                    MessageBox.Show(
                        UpdateStatus,
                        "DownloadPilot updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return;
            }

            var settings = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
            if (settings.AutoDownloadUpdates && !string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                UpdateStatus = "Nieuwe update gevonden; download wordt alvast klaargezet...";
                var autoDownloadedPath = await _updateService.DownloadUpdateAsync(result, CancellationToken.None);
                if (!string.IsNullOrWhiteSpace(autoDownloadedPath))
                {
                    var installResponse = MessageBox.Show(
                        $"Update {result.LatestVersion} is gedownload.\n\nNu installeren en DownloadPilot opnieuw starten?",
                        "DownloadPilot update klaar",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (installResponse == MessageBoxResult.Yes)
                    {
                        var shouldShutdownAfterAutoDownload = GitHubUpdateService.StartDownloadedUpdate(
                            autoDownloadedPath,
                            AppContext.BaseDirectory,
                            Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "DownloadPilot.App.exe"),
                            Environment.ProcessId);
                        if (shouldShutdownAfterAutoDownload)
                        {
                            Application.Current.Shutdown();
                        }
                    }

                    UpdateStatus = $"Update gedownload: {autoDownloadedPath}";
                    return;
                }
            }

            var prompt = string.IsNullOrWhiteSpace(result.DownloadUrl)
                ? $"Er is een nieuwe versie beschikbaar: {result.LatestVersion}. Open de GitHub releasepagina?"
                : $"Er is een nieuwe versie beschikbaar: {result.LatestVersion}.\n\nDownload en installeren?";
            var response = MessageBox.Show(
                prompt,
                "Nieuwe DownloadPilot versie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (response != MessageBoxResult.Yes)
            {
                UpdateStatus = "Update overgeslagen";
                return;
            }

            if (string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                OpenUrl(result.ReleaseUrl ?? "https://github.com/kratje050/Downloadpilot/releases");
                return;
            }

            UpdateStatus = "Update wordt gedownload...";
            var downloadedPath = await _updateService.DownloadUpdateAsync(result, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(downloadedPath))
            {
                UpdateStatus = "Geen downloadbare release-asset gevonden";
                OpenUrl(result.ReleaseUrl ?? "https://github.com/kratje050/Downloadpilot/releases");
                return;
            }

            UpdateStatus = $"Update gedownload: {downloadedPath}";
            var shouldShutdown = GitHubUpdateService.StartDownloadedUpdate(
                downloadedPath,
                AppContext.BaseDirectory,
                Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "DownloadPilot.App.exe"),
                Environment.ProcessId);
            if (shouldShutdown)
            {
                Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Updatecheck mislukt: {ex.Message}";
            ToolStatusMessage = UpdateStatus;
            if (showUpToDateMessage)
            {
                MessageBox.Show(
                    UpdateStatus,
                    "DownloadPilot updates",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void OpenSelectedToolPaths()
    {
        var items = GetSelectedToolItems();
        if (items.Count == 0)
        {
            ToolStatusMessage = "Geen auditregels aangevinkt";
            return;
        }

        var opened = items.Count(item => OpenPathOrUrl(item.Path));
        ToolStatusMessage = opened == 0
            ? "Geen bestaande paden of urls om te openen"
            : $"{opened} item(s) geopend";
    }

    private void RevealSelectedToolPaths()
    {
        var items = GetSelectedToolItems();
        if (items.Count == 0)
        {
            ToolStatusMessage = "Geen auditregels aangevinkt";
            return;
        }

        var opened = items.Count(item => RevealPath(item.Path));
        ToolStatusMessage = opened == 0
            ? "Geen bestaande paden om in Verkenner te tonen"
            : $"{opened} item(s) in Verkenner getoond";
    }

    private async Task IgnoreSelectedToolPathsAsync()
    {
        var paths = GetSelectedExistingToolPaths().ToList();
        if (paths.Count == 0)
        {
            ToolStatusMessage = "Geen bestaande auditpaden om te negeren";
            return;
        }

        var ignored = GetIgnoredPathsSnapshot();
        foreach (var path in paths)
        {
            if (!ignored.Any(existing => PathsOverlap(existing, path)))
            {
                ignored.Add(path);
            }
        }

        await SaveIgnoredPathsAsync(ignored);
        RemoveToolItemsByPath(paths);
        ToolStatusMessage = $"{paths.Count} auditpad(en) genegeerd";
    }

    private async Task ProtectSelectedToolPathsAsync()
    {
        var paths = GetSelectedExistingToolPaths()
            .Select(path => Directory.Exists(path) ? path : Path.GetDirectoryName(path))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (paths.Count == 0)
        {
            ToolStatusMessage = "Geen auditmappen om te beschermen";
            return;
        }

        var protectedPaths = GetProtectedPathsSnapshot();
        foreach (var path in paths)
        {
            if (!protectedPaths.Any(existing => PathsOverlap(existing, path)))
            {
                protectedPaths.Add(path);
            }
        }

        await SaveProtectedPathsAsync(protectedPaths);
        ToolStatusMessage = $"{paths.Count} map(pen) toegevoegd aan niet-aanraken";
    }

    private async Task AddExtraScanPathAsync()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Kies extra map voor Power-audit en opslagkaart"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            ToolStatusMessage = "Extra scanmap toevoegen geannuleerd";
            return;
        }

        var paths = GetExtraScanPathsSnapshot();
        if (!paths.Any(path => PathsOverlap(path, dialog.SelectedPath)))
        {
            paths.Add(dialog.SelectedPath);
        }

        await SaveExtraScanPathsAsync(paths);
        ToolStatusMessage = "Extra auditmap toegevoegd";
    }

    private async Task RemoveSelectedExtraScanPathsAsync()
    {
        var selected = ExtraScanPathItems
            .Where(item => item.IsSelected && !string.IsNullOrWhiteSpace(item.Path))
            .Select(item => item.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selected.Count == 0)
        {
            ToolStatusMessage = "Geen extra scanmappen aangevinkt";
            return;
        }

        var remaining = GetExtraScanPathsSnapshot()
            .Where(path => !selected.Contains(path))
            .ToList();

        await SaveExtraScanPathsAsync(remaining);
        ToolStatusMessage = $"{selected.Count} extra scanmap(pen) verwijderd";
    }

    private async Task RemoveSelectedIgnoredPathsAsync()
    {
        var selected = IgnoredPathItems
            .Where(item => item.IsSelected && !string.IsNullOrWhiteSpace(item.Path))
            .Select(item => item.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selected.Count == 0)
        {
            ToolStatusMessage = "Geen genegeerde paden aangevinkt";
            return;
        }

        var remaining = GetIgnoredPathsSnapshot()
            .Where(path => !selected.Contains(path))
            .ToList();

        await SaveIgnoredPathsAsync(remaining);
        ToolStatusMessage = $"{selected.Count} genegeerde pad(en) verwijderd";
    }

    private async Task MoveSelectedToolItemsToRecycleBinAsync()
    {
        var items = GetSelectedToolItems()
            .Where(item => !string.IsNullOrWhiteSpace(item.Path))
            .ToList();

        if (items.Count == 0)
        {
            ToolStatusMessage = "Geen auditregels aangevinkt";
            return;
        }

        var confirmation = MessageBox.Show(
            $"Verplaats {items.Count} audititem(s) naar de Windows Prullenbak?",
            "Audititems opruimen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            ToolStatusMessage = "Audit-opruiming geannuleerd";
            return;
        }

        await CreateToolBackupAsync(items, "Herstelpunt voor audititems naar Prullenbak", CancellationToken.None);

        var moved = 0;
        var failed = 0;
        foreach (var item in items)
        {
            try
            {
                if (IsProtectedPath(item.Path))
                {
                    failed++;
                    continue;
                }

                if (File.Exists(item.Path))
                {
                    FileSystem.DeleteFile(item.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    moved++;
                }
                else if (Directory.Exists(item.Path))
                {
                    FileSystem.DeleteDirectory(item.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    moved++;
                }
            }
            catch
            {
                failed++;
            }
        }

        RemoveToolItemsByPath(items.Select(item => item.Path!).ToList());
        ToolStatusMessage = failed == 0
            ? $"{moved} audititem(s) naar de Prullenbak verplaatst"
            : $"{moved} audititem(s) verplaatst, {failed} mislukt of beschermd";
    }

    private async Task MoveSelectedToolItemsToQuarantineAsync()
    {
        var items = GetSelectedToolItems()
            .Where(item => !string.IsNullOrWhiteSpace(item.Path))
            .Where(item => File.Exists(item.Path) || Directory.Exists(item.Path))
            .ToList();

        if (items.Count == 0)
        {
            ToolStatusMessage = "Geen bestaande audititems voor Quarantaine";
            return;
        }

        var confirmation = MessageBox.Show(
            $"Verplaats {items.Count} audititem(s) naar Quarantaine? Ze worden niet verwijderd.",
            "Audititems naar Quarantaine",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            ToolStatusMessage = "Quarantaine geannuleerd";
            return;
        }

        await CreateToolBackupAsync(items, "Herstelpunt voor audititems naar Quarantaine", CancellationToken.None);
        var quarantineRoot = Path.Combine(GetDestinationRoot(), "Quarantaine", "Audit", DateTime.Now.ToString("yyyy-MM"));
        Directory.CreateDirectory(quarantineRoot);

        var moved = 0;
        var failed = 0;
        foreach (var item in items)
        {
            try
            {
                if (IsProtectedPath(item.Path))
                {
                    failed++;
                    continue;
                }

                var target = BuildUniqueToolPath(quarantineRoot, item.Path!);
                if (File.Exists(item.Path))
                {
                    File.Move(item.Path, target);
                    moved++;
                }
                else if (Directory.Exists(item.Path))
                {
                    Directory.Move(item.Path, target);
                    moved++;
                }
            }
            catch
            {
                failed++;
            }
        }

        RemoveToolItemsByPath(items.Select(item => item.Path!).ToList());
        ToolStatusMessage = failed == 0
            ? $"{moved} audititem(s) naar Quarantaine verplaatst"
            : $"{moved} audititem(s) verplaatst, {failed} mislukt of beschermd";
    }

    private bool CanRunDryRun()
    {
        return NewFileProposals.Count > 0
            || DuplicateProposals.Any(proposal => proposal.IsSelected)
            || SteamResidueItems.Any(item => item.IsSelected);
    }

    private void RunDryRun()
    {
        DryRunItems.Clear();

        var proposals = NewFileProposals.Any(proposal => proposal.IsSelected)
            ? NewFileProposals.Where(proposal => proposal.IsSelected).ToList()
            : NewFileProposals.ToList();

        foreach (var proposal in proposals.Take(120))
        {
            DryRunItems.Add(new ToolInsightItemViewModel
            {
                Title = proposal.OriginalFileName,
                Detail = proposal.DryRunAction,
                Metric = proposal.FileSizeReadable,
                Category = proposal.SafetyLabel,
                Path = proposal.OriginalPath,
                Action = proposal.SafetyExplanation,
                SizeBytes = proposal.FileSizeBytes
            });
        }

        foreach (var item in SteamResidueItems.Where(item => item.IsSelected).Take(80))
        {
            DryRunItems.Add(new ToolInsightItemViewModel
            {
                Title = item.Title,
                Detail = "Zou naar de Windows Prullenbak worden verplaatst",
                Metric = item.Metric,
                Category = "Twijfel",
                Path = item.Path,
                Action = item.Action,
                SizeBytes = item.SizeBytes
            });
        }

        DryRunStatus = DryRunItems.Count == 0
            ? "Geen open acties voor proefmodus"
            : $"{DryRunItems.Count} acties gesimuleerd; er is niets verplaatst of verwijderd.";
        ToolStatusMessage = DryRunStatus;
        RaiseSmartToolsStateChanged();
    }

    private async Task RefreshCleanupAdvisorAsync()
    {
        if (IsCleanupAdvisorScanning)
        {
            return;
        }

        IsCleanupAdvisorScanning = true;
        AppResidueStatus = "App-restanten worden gescand...";
        DriverCleanupStatus = "Driver-downloads worden gescand...";
        StorageMapStatus = "Opslagkaart wordt opgebouwd...";
        AdvancedAuditStatus = "Power-audit scant privacy, cloud, cache, shortcuts, installers en opstartitems...";
        ToolStatusMessage = "PC-audit bezig...";

        try
        {
            var destinationRoot = GetDestinationRoot();
            var extraRoots = GetExtraScanPathsSnapshot()
                .Where(Directory.Exists)
                .ToList();
            var result = await Task.Run(() =>
            {
                var storageRoots = new List<string>();
                if (Directory.Exists(destinationRoot))
                {
                    storageRoots.Add(destinationRoot);
                }

                storageRoots.AddRange(extraRoots);

                return new
                {
                    Apps = CleanupAdvisor.ScanAppResidues(maxCandidates: 80),
                    Drivers = CleanupAdvisor.ScanDriverDownloads(maxCandidates: 80),
                    Storage = CleanupAdvisor.BuildStorageMap(
                        storageRoots.Count == 0 ? null : storageRoots,
                        maxCandidates: 12),
                    Advanced = CleanupAdvisor.RunPowerAudit(
                        storageRoots.Count == 0 ? null : storageRoots,
                        maxCandidates: 180)
                };
            });

            ReplaceToolItems(AppResidueItems, result.Apps);
            ReplaceToolItems(DriverCleanupItems, result.Drivers);
            ReplaceToolItems(StorageMapItems, result.Storage);
            ReplaceToolItems(AdvancedAuditItems, result.Advanced);
            UpdatePotentialCleanupSavings();

            AppResidueStatus = AppResidueItems.Count == 0
                ? "Geen duidelijke app-restanten gevonden"
                : $"{AppResidueItems.Count} app/cache-locaties in beeld";
            DriverCleanupStatus = DriverCleanupItems.Count == 0
                ? "Geen oude driver-downloads gevonden"
                : $"{DriverCleanupItems.Count} driver-downloads/cachelocaties gevonden";
            StorageMapStatus = StorageMapItems.Count == 0
                ? "Geen opslaglocaties met meetbare inhoud gevonden"
                : $"{StorageMapItems.Count} grote opslaglocaties in kaart";
            AdvancedAuditStatus = AdvancedAuditItems.Count == 0
                ? "Power-audit vond geen extra aandachtspunten"
                : $"{AdvancedAuditItems.Count} extra power-audit punten gevonden";
            ToolStatusMessage = "PC-audit klaar";
        }
        catch (Exception ex)
        {
            ToolStatusMessage = $"PC-audit mislukt: {ex.Message}";
            AppResidueStatus = ToolStatusMessage;
        }
        finally
        {
            IsCleanupAdvisorScanning = false;
            RaiseSmartToolsStateChanged();
        }
    }

    private async Task ProtectSelectedProposalFoldersAsync()
    {
        var proposals = NewFileProposals
            .Where(proposal => proposal.IsSelected)
            .ToList();

        if (proposals.Count == 0 && SelectedProposal is not null)
        {
            proposals.Add(SelectedProposal);
        }

        var paths = proposals
            .Select(proposal => proposal.SourceFolder)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (paths.Count == 0)
        {
            ProtectedPathsStatus = "Geen voorstel geselecteerd om te beschermen";
            ToolStatusMessage = ProtectedPathsStatus;
            return;
        }

        var existing = GetProtectedPathsSnapshot();
        var added = 0;
        foreach (var path in paths)
        {
            if (existing.Any(current => PathsOverlap(current, path)))
            {
                continue;
            }

            existing.Add(path);
            added++;
        }

        await SaveProtectedPathsAsync(existing);
        ProtectedPathsStatus = added == 0
            ? "Deze map stond al in de niet-aanraken-lijst"
            : $"{added} map(pen) toegevoegd aan niet-aanraken";
        ToolStatusMessage = ProtectedPathsStatus;
    }

    private async Task RemoveSelectedProtectedPathsAsync()
    {
        var selected = ProtectedPathItems
            .Where(item => item.IsSelected && !string.IsNullOrWhiteSpace(item.Path))
            .Select(item => item.Path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selected.Count == 0)
        {
            ProtectedPathsStatus = "Geen beschermde paden aangevinkt";
            return;
        }

        var remaining = GetProtectedPathsSnapshot()
            .Where(path => !selected.Contains(path))
            .ToList();

        await SaveProtectedPathsAsync(remaining);
        ProtectedPathsStatus = $"{selected.Count} beschermde pad(en) verwijderd";
        ToolStatusMessage = ProtectedPathsStatus;
    }

    private async Task MoveRiskyDownloadsToQuarantineAsync()
    {
        var risky = NewFileProposals
            .Where(IsRiskyDownload)
            .Where(proposal => !IsProtectedProposal(proposal))
            .ToList();

        if (risky.Count == 0)
        {
            ToolStatusMessage = "Geen risicodownloads gevonden";
            return;
        }

        var confirmation = MessageBox.Show(
            $"Verplaats {risky.Count} risicodownloads naar Quarantaine? Ze worden niet verwijderd.",
            "Veilige quarantaine",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            ToolStatusMessage = "Quarantaine geannuleerd";
            return;
        }

        await CreateSmartBackupAsync(risky, "Herstelpunt voor quarantaine", CancellationToken.None);

        var quarantineFolder = Path.Combine(GetDestinationRoot(), "Quarantaine", DateTime.Now.ToString("yyyy-MM"));
        var moved = 0;
        var failed = 0;

        _isProcessingAllProposals = true;
        RaiseProposalCommandStates();

        try
        {
            foreach (var proposal in risky)
            {
                var request = new FileOperationRequest
                {
                    Analysis = proposal.Analysis,
                    TargetFolder = quarantineFolder,
                    TargetFileName = proposal.TargetFileName,
                    AppliedRuleName = "Veilige quarantaine",
                    IsAutoApplied = false
                };

                var entry = await _fileOperationService.MoveAndRenameAsync(request, CancellationToken.None);
                if (entry.Status == HistoryStatus.Geslaagd)
                {
                    RememberSessionEntry(entry);
                    moved++;
                    RemoveProposal(proposal);
                    continue;
                }

                failed++;
            }
        }
        finally
        {
            _isProcessingAllProposals = false;
            RaiseProposalCommandStates();
        }

        await RefreshHistoryAsync();
        RefreshSmartToolInsights();
        ToolStatusMessage = failed == 0
            ? $"{moved} bestanden naar Quarantaine verplaatst"
            : $"{moved} bestanden verplaatst, {failed} mislukt";
    }

    private async Task ExportWeeklyCleanupReportAsync()
    {
        using var dialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = "Markdown bestanden (*.md)|*.md|Tekstbestanden (*.txt)|*.txt",
            FileName = $"downloadpilot-weekrapport-{DateTime.Now:yyyy-MM-dd}.md",
            Title = "Weekrapport opslaan"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            ToolStatusMessage = "Weekrapport geannuleerd";
            return;
        }

        var text = await BuildWeeklyReportTextAsync(CancellationToken.None);
        await File.WriteAllTextAsync(dialog.FileName, text, Encoding.UTF8);
        WeeklyReportText = text;
        ToolStatusMessage = $"Weekrapport opgeslagen: {dialog.FileName}";
    }

    private async Task ExportMonthlyCleanupReportAsync()
    {
        using var dialog = new System.Windows.Forms.SaveFileDialog
        {
            Filter = "Markdown bestanden (*.md)|*.md|Tekstbestanden (*.txt)|*.txt",
            FileName = $"downloadpilot-maandrapport-{DateTime.Now:yyyy-MM-dd}.md",
            Title = "Maandrapport opslaan"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            ToolStatusMessage = "Maandrapport geannuleerd";
            return;
        }

        var text = await BuildCleanupReportTextAsync(30, "maandrapport", CancellationToken.None);
        await File.WriteAllTextAsync(dialog.FileName, text, Encoding.UTF8);
        WeeklyReportText = text;
        ToolStatusMessage = $"Maandrapport opgeslagen: {dialog.FileName}";
    }

    public async Task<string> ExportWeeklyCleanupReportToDirectoryAsync(
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"downloadpilot-weekrapport-{DateTime.Now:yyyy-MM-dd}.md");
        var text = await BuildWeeklyReportTextAsync(cancellationToken);
        await File.WriteAllTextAsync(outputPath, text, Encoding.UTF8, cancellationToken);
        return outputPath;
    }

    private async Task RegisterWeeklyCleanupReportAsync()
    {
        SettingsEditor.CleanupSchedule = "Wekelijks";
        await RegisterCleanupScheduleAsync();
    }

    private async Task RegisterCleanupScheduleAsync()
    {
        var reportDirectory = Path.Combine(SqlitePaths.DataDirectory, "Reports");
        Directory.CreateDirectory(reportDirectory);
        var executablePath = Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, "DownloadPilot.App.exe");
        var scriptPath = Path.Combine(SqlitePaths.DataDirectory, "DownloadPilotOnderhoud.cmd");
        await File.WriteAllTextAsync(
            scriptPath,
            $"@echo off{Environment.NewLine}{Quote(executablePath)} --weekly-report {Quote(reportDirectory)}{Environment.NewLine}",
            Encoding.ASCII);

        var schedule = string.IsNullOrWhiteSpace(SettingsEditor.CleanupSchedule)
            ? "Wekelijks"
            : SettingsEditor.CleanupSchedule.Trim();
        if (schedule.Equals("Geen planning", StringComparison.OrdinalIgnoreCase))
        {
            using var deleteProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = "/Delete /TN \"DownloadPilot Onderhoud\" /F",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (deleteProcess is not null)
            {
                await deleteProcess.WaitForExitAsync();
            }

            CleanupScheduleStatus = "Opruimplanning uitgeschakeld";
            ToolStatusMessage = CleanupScheduleStatus;
            await SaveSettingsAsync();
            return;
        }

        var scheduleArgs = schedule.Equals("Maandelijks", StringComparison.OrdinalIgnoreCase)
            ? "/SC MONTHLY /D 1 /ST 18:00"
            : "/SC WEEKLY /D SUN /ST 18:00";

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = $"/Create /TN \"DownloadPilot Onderhoud\" {scheduleArgs} /TR {Quote(scriptPath)} /F",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (process is null)
        {
            ToolStatusMessage = "Weekrapport planning kon niet worden gestart";
            return;
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        CleanupScheduleStatus = process.ExitCode == 0
            ? $"{schedule} onderhoud gepland in {reportDirectory}"
            : $"Planning mislukt: {SimplifyProcessOutput(error, output)}";
        ToolStatusMessage = CleanupScheduleStatus;
        if (process.ExitCode == 0)
        {
            await SaveSettingsAsync();
        }
    }

    private void RefreshBrowserDownloadSources()
    {
        BrowserDownloadSourceItems.Clear();

        foreach (var item in BuildBrowserDownloadSourceItems(NewFileProposals))
        {
            BrowserDownloadSourceItems.Add(item);
        }

        BrowserSourceStatus = BrowserDownloadSourceItems.Count == 0
            ? "Geen browser-herkomst gevonden in Windows Zone.Identifier metadata"
            : $"{BrowserDownloadSourceItems.Count} herkomstgroepen gevonden";
        RaiseSmartToolsStateChanged();
    }

    private void FindSimilarPhotos()
    {
        SimilarPhotoItems.Clear();

        var fingerprints = NewFileProposals
            .Where(proposal => IsImagePreviewExtension(proposal.Analysis.Extension))
            .Select(proposal => new { Proposal = proposal, Hash = TryComputeAverageHash(proposal.OriginalPath) })
            .Where(item => item.Hash is not null)
            .Select(item => new { item.Proposal, Hash = item.Hash!.Value })
            .ToList();

        var used = new HashSet<ProposalItemViewModel>();
        foreach (var item in fingerprints)
        {
            if (used.Contains(item.Proposal))
            {
                continue;
            }

            var group = fingerprints
                .Where(candidate => !ReferenceEquals(candidate.Proposal, item.Proposal)
                    && HammingDistance(item.Hash, candidate.Hash) <= 8)
                .Select(candidate => candidate.Proposal)
                .Prepend(item.Proposal)
                .Where(proposal => used.Add(proposal))
                .ToList();

            if (group.Count <= 1)
            {
                continue;
            }

            SimilarPhotoItems.Add(new ToolInsightItemViewModel
            {
                Title = $"{group.Count} vergelijkbare foto's",
                Detail = string.Join(", ", group.Take(4).Select(proposal => proposal.OriginalFileName)),
                Metric = FormatBytes(group.Sum(proposal => proposal.FileSizeBytes)),
                Category = "Foto's",
                Path = group.First().SourceFolder,
                Action = "Controleer voor opruimen"
            });
        }

        ToolStatusMessage = SimilarPhotoItems.Count == 0
            ? "Geen bijna-dubbele foto's gevonden in open voorstellen"
            : $"{SimilarPhotoItems.Count} groepen met bijna-dubbele foto's gevonden";
        RaiseSmartToolsStateChanged();
    }

    private async Task ScanSteamResiduesAsync()
    {
        if (IsScanningSteamResidues)
        {
            return;
        }

        IsScanningSteamResidues = true;
        SteamResidueStatus = "Game launchers, modmanagers en AppData worden gescand...";
        ToolStatusMessage = SteamResidueStatus;

        try
        {
            var candidates = await Task.Run(() =>
                GameResidueScanner.Scan(new GameResidueScanOptions { MaxCandidates = 250 }));

            foreach (var item in SteamResidueItems)
            {
                item.PropertyChanged -= OnSteamResidueItemPropertyChanged;
            }

            SteamResidueItems.Clear();
            foreach (var candidate in candidates)
            {
                var item = new ToolInsightItemViewModel
                {
                    IsSelected = false,
                    Title = candidate.Name,
                    Detail = $"{candidate.Source} - {candidate.RootName} - laatst gewijzigd {candidate.LastWriteLocal:dd-MM-yyyy}",
                    Metric = FormatBytes(candidate.SizeBytes),
                    Category = $"{candidate.Confidence}% zeker",
                    Path = candidate.Path,
                    Action = candidate.Reason,
                    SizeBytes = candidate.SizeBytes
                };
                item.PropertyChanged += OnSteamResidueItemPropertyChanged;
                SteamResidueItems.Add(item);
            }

            SteamResidueStatus = candidates.Count == 0
                ? "Geen duidelijke game- of modrestanten gevonden"
                : $"{candidates.Count} mogelijke game/mod restmappen gevonden ({SteamResidueSizeReadable})";
            ToolStatusMessage = SteamResidueStatus;
        }
        catch (Exception ex)
        {
            SteamResidueStatus = $"Game-restantenscan mislukt: {ex.Message}";
            ToolStatusMessage = SteamResidueStatus;
        }
        finally
        {
            IsScanningSteamResidues = false;
            RaiseSteamResidueStateChanged();
            RaiseSmartToolsStateChanged();
        }
    }

    private void SelectAllSteamResidues()
    {
        foreach (var item in SteamResidueItems)
        {
            item.IsSelected = true;
        }

        SteamResidueStatus = $"{SelectedSteamResidueCount} game/mod restmappen aangevinkt";
    }

    private void ClearSteamResidueSelection()
    {
        foreach (var item in SteamResidueItems)
        {
            item.IsSelected = false;
        }

        SteamResidueStatus = "Game-restantenselectie gewist";
    }

    private async Task MoveSelectedSteamResiduesToRecycleBinAsync()
    {
        var selected = SteamResidueItems
            .Where(item => item.IsSelected && !string.IsNullOrWhiteSpace(item.Path))
            .ToList();

        if (selected.Count == 0)
        {
            SteamResidueStatus = "Geen game/mod restmappen aangevinkt";
            return;
        }

        var confirmation = MessageBox.Show(
            $"Verplaats {selected.Count} game/mod restmappen naar de Windows Prullenbak? Dit verwijdert ze niet definitief.",
            "Game restmappen opruimen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            SteamResidueStatus = "Game-restanten opruimen geannuleerd";
            return;
        }

        await CreateToolBackupAsync(selected, "Herstelpunt voor game/mod restmappen", CancellationToken.None);

        var moved = 0;
        var failed = 0;
        IsScanningSteamResidues = true;

        try
        {
            foreach (var item in selected)
            {
                try
                {
                    if (IsProtectedPath(item.Path))
                    {
                        item.IsSelected = false;
                        failed++;
                        continue;
                    }

                    if (item.Path is null || !Directory.Exists(item.Path))
                    {
                        SteamResidueItems.Remove(item);
                        moved++;
                        continue;
                    }

                    await Task.Run(() =>
                    {
                        FileSystem.DeleteDirectory(
                            item.Path,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin);
                    });

                    item.PropertyChanged -= OnSteamResidueItemPropertyChanged;
                    SteamResidueItems.Remove(item);
                    moved++;
                }
                catch
                {
                    item.IsSelected = false;
                    failed++;
                }
            }

            SteamResidueStatus = failed == 0
                ? $"{moved} game/mod restmappen naar de Prullenbak verplaatst"
                : $"{moved} game/mod restmappen verplaatst, {failed} mislukt";
            ToolStatusMessage = SteamResidueStatus;
        }
        finally
        {
            IsScanningSteamResidues = false;
            RaiseSteamResidueStateChanged();
            RaiseSmartToolsStateChanged();
        }
    }

    private void OnSteamResidueItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ToolInsightItemViewModel.IsSelected))
        {
            RaiseSteamResidueStateChanged();
        }
    }

    private void RaiseSteamResidueStateChanged()
    {
        RaisePropertyChanged(nameof(SteamResidueCount));
        RaisePropertyChanged(nameof(SelectedSteamResidueCount));
        RaisePropertyChanged(nameof(SteamResidueSizeReadable));
        ScanSteamResiduesCommand.RaiseCanExecuteChanged();
        SelectAllSteamResiduesCommand.RaiseCanExecuteChanged();
        ClearSteamResidueSelectionCommand.RaiseCanExecuteChanged();
        MoveSelectedSteamResiduesToRecycleBinCommand.RaiseCanExecuteChanged();
        RunDryRunCommand.RaiseCanExecuteChanged();
    }

    private async Task CreateManualSmartBackupAsync()
    {
        var proposals = NewFileProposals.Where(proposal => proposal.IsSelected).ToList();
        if (proposals.Count == 0)
        {
            proposals = NewFileProposals.ToList();
        }

        if (proposals.Count == 0)
        {
            BackupStatus = "Geen open voorstellen om in een herstelpunt te zetten";
            ToolStatusMessage = BackupStatus;
            return;
        }

        var path = await CreateSmartBackupAsync(proposals, "Handmatig herstelpunt", CancellationToken.None);
        ToolStatusMessage = $"Herstelpunt gemaakt: {path}";
    }

    private void RefreshSmartToolInsights()
    {
        SmartRenameItems.Clear();
        LargeFileCoachItems.Clear();
        QuarantineItems.Clear();
        InvoiceDashboardItems.Clear();
        SafetyScoreItems.Clear();

        foreach (var proposal in NewFileProposals.Where(NeedsSmartNameRepair).Take(50))
        {
            SmartRenameItems.Add(new ToolInsightItemViewModel
            {
                Title = proposal.OriginalFileName,
                Detail = proposal.Analysis.SuggestedFileName,
                Metric = proposal.Category,
                Category = "Naam",
                Path = proposal.OriginalPath,
                Action = "Slimme naam toepassen"
            });
        }

        foreach (var proposal in NewFileProposals
                     .OrderBy(proposal => proposal.SafetyScore)
                     .ThenBy(proposal => proposal.OriginalFileName, StringComparer.CurrentCultureIgnoreCase)
                     .Take(60))
        {
            SafetyScoreItems.Add(new ToolInsightItemViewModel
            {
                Title = proposal.OriginalFileName,
                Detail = proposal.WhyFound,
                Metric = $"{proposal.SafetyScore}%",
                Category = proposal.SafetyLabel,
                Path = proposal.OriginalPath,
                Action = proposal.SafetyExplanation,
                SizeBytes = proposal.FileSizeBytes
            });
        }

        foreach (var proposal in NewFileProposals
                     .Where(proposal => proposal.FileSizeBytes >= 100L * 1024L * 1024L || proposal.Analysis.CreatedLocal <= DateTime.Now.AddDays(-30))
                     .OrderByDescending(proposal => proposal.FileSizeBytes)
                     .Take(50))
        {
            LargeFileCoachItems.Add(new ToolInsightItemViewModel
            {
                Title = proposal.OriginalFileName,
                Detail = proposal.Analysis.CreatedLocal <= DateTime.Now.AddDays(-30)
                    ? "Ouder dan 30 dagen"
                    : "Groot bestand",
                Metric = proposal.FileSizeReadable,
                Category = proposal.Category,
                Path = proposal.OriginalPath,
                Action = "Bekijk doelmap"
            });
        }

        foreach (var proposal in NewFileProposals.Where(IsRiskyDownload).Take(50))
        {
            QuarantineItems.Add(new ToolInsightItemViewModel
            {
                Title = proposal.OriginalFileName,
                Detail = "Uitvoerbaar/scriptachtig bestand of risiconaam",
                Metric = proposal.Analysis.Extension.ToUpperInvariant(),
                Category = "Quarantaine",
                Path = proposal.OriginalPath,
                Action = "Naar Quarantaine"
            });
        }

        foreach (var group in BuildInvoiceRecords()
                     .GroupBy(record => new { record.Company, Period = record.Date.ToString("yyyy-MM") })
                     .OrderByDescending(group => group.Key.Period)
                     .ThenBy(group => group.Key.Company, StringComparer.CurrentCultureIgnoreCase)
                     .Take(50))
        {
            InvoiceDashboardItems.Add(new InvoiceDashboardItemViewModel
            {
                Company = group.Key.Company,
                Period = group.Key.Period,
                Count = group.Count(),
                TotalAmount = group.Sum(record => record.Amount),
                Examples = string.Join(", ", group.Take(3).Select(record => record.FileName))
            });
        }

        SafetyStatus = SafetyScoreItems.Count == 0
            ? "Geen open voorstellen om veiligheid te scoren"
            : $"{SafetyScoreItems.Count} voorstellen met veiligheidsuitleg bijgewerkt";

        if (BrowserDownloadSourceItems.Count == 0)
        {
            foreach (var item in BuildBrowserDownloadSourceItems(NewFileProposals))
            {
                BrowserDownloadSourceItems.Add(item);
            }
        }

        ToolStatusMessage = "Slimme tools bijgewerkt";
        RaiseSmartToolsStateChanged();
    }

    private void RaiseSmartToolsStateChanged()
    {
        UpdatePotentialCleanupSavings();
        RaisePropertyChanged(nameof(SmartRenameCount));
        RaisePropertyChanged(nameof(LargeFileCoachCount));
        RaisePropertyChanged(nameof(QuarantineCount));
        RaisePropertyChanged(nameof(SimilarPhotoCount));
        RaisePropertyChanged(nameof(BrowserSourceCount));
        RaisePropertyChanged(nameof(SteamResidueCount));
        RaisePropertyChanged(nameof(DryRunCount));
        RaisePropertyChanged(nameof(SafetyScoreCount));
        RaisePropertyChanged(nameof(AppResidueCount));
        RaisePropertyChanged(nameof(DriverCleanupCount));
        RaisePropertyChanged(nameof(StorageMapCount));
        RaisePropertyChanged(nameof(ProtectedPathCount));
        RaisePropertyChanged(nameof(AdvancedAuditCount));
        RaisePropertyChanged(nameof(SelectedAdvancedAuditCount));
        RaisePropertyChanged(nameof(SelectedSteamResidueCount));
        RaisePropertyChanged(nameof(SteamResidueSizeReadable));
        RaisePropertyChanged(nameof(InvoiceDashboardCount));
        RaisePropertyChanged(nameof(PotentialCleanupSavingsReadable));
        RaisePropertyChanged(nameof(IgnoredPathCount));
        RaisePropertyChanged(nameof(ExtraScanPathCount));
        ApplySmartNameRepairCommand.RaiseCanExecuteChanged();
        RunDryRunCommand.RaiseCanExecuteChanged();
        RefreshCleanupAdvisorCommand.RaiseCanExecuteChanged();
        ProtectSelectedProposalFolderCommand.RaiseCanExecuteChanged();
        RemoveSelectedProtectedPathsCommand.RaiseCanExecuteChanged();
        MoveRiskyDownloadsToQuarantineCommand.RaiseCanExecuteChanged();
        SelectAllSteamResiduesCommand.RaiseCanExecuteChanged();
        ClearSteamResidueSelectionCommand.RaiseCanExecuteChanged();
        MoveSelectedSteamResiduesToRecycleBinCommand.RaiseCanExecuteChanged();
    }

    private void UpdatePotentialCleanupSavings()
    {
        _potentialCleanupSavingsBytes =
            DuplicateProposals.Sum(proposal => proposal.FileSizeBytes)
            + SteamResidueItems.Sum(item => item.SizeBytes)
            + AppResidueItems.Sum(item => item.SizeBytes)
            + DriverCleanupItems.Sum(item => item.SizeBytes)
            + AdvancedAuditItems.Sum(item => item.SizeBytes);
    }

    private IEnumerable<InvoiceRecord> BuildInvoiceRecords()
    {
        foreach (var proposal in NewFileProposals.Where(IsInvoiceLike))
        {
            yield return ExtractInvoiceRecord(
                proposal.TargetFileName,
                proposal.TargetFolder,
                proposal.Analysis.CreatedLocal,
                proposal.OriginalFileName);
        }

        foreach (var entry in _historyCache.Where(IsInvoiceLike))
        {
            yield return ExtractInvoiceRecord(
                entry.NewName,
                Path.GetDirectoryName(entry.NewPath) ?? string.Empty,
                entry.TimestampLocal,
                entry.OriginalName);
        }
    }

    private static InvoiceRecord ExtractInvoiceRecord(
        string fileName,
        string targetFolder,
        DateTime date,
        string sourceName)
    {
        var company = ExtractCompanyName(fileName, targetFolder);
        var amount = ExtractAmount(fileName);
        return new InvoiceRecord(company, date, amount, sourceName, targetFolder);
    }

    private static string ExtractCompanyName(string fileName, string targetFolder)
    {
        var folderName = Path.GetFileName(targetFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!string.IsNullOrWhiteSpace(folderName)
            && !folderName.Equals(FileCategory.Facturen.ToString(), StringComparison.OrdinalIgnoreCase)
            && !folderName.Equals(FileCategory.Documenten.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return folderName;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var parts = nameWithoutExtension.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 3 ? parts[2] : "Onbekend";
    }

    private static decimal ExtractAmount(string fileName)
    {
        var match = Regex.Match(fileName, @"_(?<amount>\d{1,6}-\d{2})(?:_|\.|$)");
        if (!match.Success)
        {
            return 0;
        }

        return decimal.TryParse(
            match.Groups["amount"].Value.Replace('-', '.'),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var amount)
            ? amount
            : 0;
    }

    private async Task<string> BuildWeeklyReportTextAsync(CancellationToken cancellationToken)
    {
        return await BuildCleanupReportTextAsync(7, "weekrapport", cancellationToken);
    }

    private async Task<string> BuildCleanupReportTextAsync(
        int days,
        string reportName,
        CancellationToken cancellationToken)
    {
        var history = await _historyService.GetRecentAsync(1000, cancellationToken);
        var since = DateTime.Now.AddDays(-days);
        var week = history.Where(entry => entry.TimestampLocal >= since).ToList();
        var moved = week.Count(entry => entry.Status == HistoryStatus.Geslaagd);
        var automatic = week.Count(entry => entry.IsAutoApplied);
        var failed = week.Count(entry => entry.Status == HistoryStatus.Mislukt);
        var categories = week
            .Where(entry => !string.IsNullOrWhiteSpace(entry.RuleName))
            .GroupBy(entry => entry.RuleName!)
            .OrderByDescending(group => group.Count())
            .Take(8)
            .Select(group => $"- {group.Key}: {group.Count()} acties");

        var text = new StringBuilder();
        text.AppendLine($"# DownloadPilot {reportName}");
        text.AppendLine();
        text.AppendLine($"Periode: {since:dd-MM-yyyy} t/m {DateTime.Now:dd-MM-yyyy}");
        text.AppendLine();
        text.AppendLine($"- Georganiseerd: {moved}");
        text.AppendLine($"- Automatisch uitgevoerd: {automatic}");
        text.AppendLine($"- Mislukt: {failed}");
        text.AppendLine($"- Open voorstellen: {NewFilesCount}");
        text.AppendLine($"- Duplicaten in beeld: {DuplicateFilesCount}");
        text.AppendLine($"- Potentiele duplicaatruimte: {PotentialDuplicateSavingsReadable}");
        text.AppendLine();
        text.AppendLine("## Meest gebruikte regels");
        text.AppendLine(categories.Any() ? string.Join(Environment.NewLine, categories) : "- Nog geen regelacties deze week");
        text.AppendLine();
        text.AppendLine("## Advies");
        text.AppendLine(ReviewQueueCount > 0
            ? $"- Controleer {ReviewQueueCount} voorstellen met lagere betrouwbaarheid."
            : "- Geen open controlepunten met lage betrouwbaarheid.");
        text.AppendLine(QuarantineCount > 0
            ? $"- Zet {QuarantineCount} risicodownloads in Quarantaine."
            : "- Geen risicodownloads in beeld.");
        text.AppendLine(SteamResidueCount > 0
            ? $"- Controleer {SteamResidueCount} mogelijke game/mod-restmappen."
            : "- Geen game/mod-restmappen in beeld.");
        text.AppendLine(DriverCleanupCount > 0
            ? $"- Controleer {DriverCleanupCount} oude driver-downloads of cachelocaties."
            : "- Geen driver-downloads in beeld.");
        text.AppendLine(ProtectedPathCount > 0
            ? $"- Niet-aanraken actief voor {ProtectedPathCount} pad(en)."
            : "- Geen niet-aanraken paden ingesteld.");

        var report = text.ToString();
        WeeklyReportText = report;
        return report;
    }

    private IEnumerable<ToolInsightItemViewModel> BuildBrowserDownloadSourceItems(IEnumerable<ProposalItemViewModel> proposals)
    {
        return proposals
            .Select(proposal => new { Proposal = proposal, Source = TryReadBrowserSource(proposal.OriginalPath) })
            .Where(item => item.Source is not null)
            .GroupBy(item => item.Source!.Domain, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Take(30)
            .Select(group => new ToolInsightItemViewModel
            {
                Title = group.Key,
                Detail = string.Join(", ", group.Take(3).Select(item => item.Proposal.OriginalFileName)),
                Metric = $"{group.Count()} bestanden",
                Category = "Browser",
                Path = group.First().Source!.Url,
                Action = "Gebruik als regelhint"
            });
    }

    private static BrowserSource? TryReadBrowserSource(string path)
    {
        try
        {
            var zonePath = path + ":Zone.Identifier";
            if (!File.Exists(path))
            {
                return null;
            }

            var lines = File.ReadAllLines(zonePath);
            var url = lines
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0].Equals("HostUrl", StringComparison.OrdinalIgnoreCase)
                    || parts[0].Equals("ReferrerUrl", StringComparison.OrdinalIgnoreCase))
                .Select(parts => parts[1])
                .FirstOrDefault(value => Uri.TryCreate(value, UriKind.Absolute, out _));

            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return new BrowserSource(uri.Host, url);
        }
        catch
        {
            return null;
        }
    }

    private static ulong? TryComputeAverageHash(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var stream = File.OpenRead(path);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
            var source = decoder.Frames[0];
            if (source.PixelWidth <= 0 || source.PixelHeight <= 0)
            {
                return null;
            }

            var resized = new TransformedBitmap(
                source,
                new ScaleTransform(8d / source.PixelWidth, 8d / source.PixelHeight));
            var gray = new FormatConvertedBitmap(resized, PixelFormats.Gray8, null, 0);
            var pixels = new byte[64];
            gray.CopyPixels(pixels, 8, 0);
            var average = pixels.Average(value => value);
            ulong hash = 0;
            for (var index = 0; index < pixels.Length; index++)
            {
                if (pixels[index] >= average)
                {
                    hash |= 1UL << index;
                }
            }

            return hash;
        }
        catch
        {
            return null;
        }
    }

    private static int HammingDistance(ulong left, ulong right)
    {
        var value = left ^ right;
        var count = 0;
        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }

    private async Task<string> CreateSmartBackupAsync(
        IReadOnlyList<ProposalItemViewModel> proposals,
        string label,
        CancellationToken cancellationToken)
    {
        var backupDirectory = Path.Combine(SqlitePaths.DataDirectory, "Backups");
        Directory.CreateDirectory(backupDirectory);
        var path = Path.Combine(backupDirectory, $"herstelpunt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        var manifest = new
        {
            Label = label,
            CreatedLocal = DateTime.Now,
            Count = proposals.Count,
            Files = proposals.Select(proposal => new
            {
                proposal.OriginalPath,
                proposal.TargetFolder,
                proposal.TargetFileName,
                proposal.Category,
                proposal.Confidence,
                proposal.Reason
            })
        };

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);

        BackupStatus = $"Herstelpunt gemaakt: {path}";
        return path;
    }

    private async Task<string> CreatePendingSessionAsync(
        IReadOnlyList<ProposalItemViewModel> proposals,
        string label,
        CancellationToken cancellationToken)
    {
        var sessionDirectory = Path.Combine(SqlitePaths.DataDirectory, "Sessions");
        Directory.CreateDirectory(sessionDirectory);
        var path = Path.Combine(sessionDirectory, "pending-session.json");
        var manifest = new
        {
            Label = label,
            StartedLocal = DateTime.Now,
            Count = proposals.Count,
            Files = proposals.Select(proposal => new
            {
                proposal.OriginalPath,
                proposal.TargetFolder,
                proposal.TargetFileName,
                proposal.Confidence,
                proposal.Category
            })
        };

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);

        return path;
    }

    private void CheckForInterruptedSession()
    {
        var path = Path.Combine(SqlitePaths.DataDirectory, "Sessions", "pending-session.json");
        if (!File.Exists(path))
        {
            return;
        }

        BackupStatus = $"Onderbroken sessie gevonden: {path}";
        StatusMessage = "DownloadPilot vond een vorige actie die mogelijk is onderbroken; controleer Geschiedenis en herstelpunten.";
    }

    private static void TryDeletePendingSession(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Crashherstel mag gewone verwerking niet blokkeren.
        }
    }

    private async Task<string> CreateToolBackupAsync(
        IReadOnlyList<ToolInsightItemViewModel> items,
        string label,
        CancellationToken cancellationToken)
    {
        var backupDirectory = Path.Combine(SqlitePaths.DataDirectory, "Backups");
        Directory.CreateDirectory(backupDirectory);
        var path = Path.Combine(backupDirectory, $"herstelpunt-tools-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        var manifest = new
        {
            Label = label,
            CreatedLocal = DateTime.Now,
            Count = items.Count,
            Items = items.Select(item => new
            {
                item.Title,
                item.Detail,
                item.Metric,
                item.Category,
                item.Path,
                item.Action,
                item.SizeBytes
            })
        };

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);

        BackupStatus = $"Herstelpunt gemaakt: {path}";
        return path;
    }

    private static bool NeedsSmartNameRepair(ProposalItemViewModel proposal)
    {
        if (proposal.TargetFileName.Equals(proposal.Analysis.SuggestedFileName, StringComparison.OrdinalIgnoreCase))
        {
            return IsGenericFileName(Path.GetFileNameWithoutExtension(proposal.OriginalFileName));
        }

        return IsGenericFileName(Path.GetFileNameWithoutExtension(proposal.TargetFileName))
            || proposal.TargetFileName.Equals(proposal.OriginalFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericFileName(string name)
    {
        return Regex.IsMatch(
            name,
            @"^(scan|img|image|photo|foto|screenshot|document|download|whatsapp|pasted|untitled)[\s_\-]?\d*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool IsRiskyDownload(ProposalItemViewModel proposal)
    {
        var extension = proposal.Analysis.Extension;
        if (extension is ".exe" or ".msi" or ".bat" or ".cmd" or ".ps1" or ".vbs" or ".js" or ".scr" or ".jar")
        {
            return true;
        }

        var name = proposal.OriginalFileName;
        return name.Contains("crack", StringComparison.OrdinalIgnoreCase)
            || name.Contains("keygen", StringComparison.OrdinalIgnoreCase)
            || name.Contains("patch", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInvoiceLike(ProposalItemViewModel proposal)
    {
        return proposal.Analysis.SuggestedCategory == FileCategory.Facturen
            || proposal.OriginalFileName.Contains("factuur", StringComparison.OrdinalIgnoreCase)
            || proposal.OriginalFileName.Contains("invoice", StringComparison.OrdinalIgnoreCase)
            || proposal.OriginalFileName.Contains("bon", StringComparison.OrdinalIgnoreCase)
            || proposal.Reason.Contains("factuur", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInvoiceLike(HistoryEntry entry)
    {
        return Contains(entry.OriginalName, "factuur")
            || Contains(entry.OriginalName, "invoice")
            || Contains(entry.OriginalName, "bon")
            || Contains(entry.NewName, "factuur")
            || Contains(entry.NewName, "invoice")
            || Contains(entry.NewPath, "Facturen");
    }

    private void ReplaceToolItems(
        ObservableCollection<ToolInsightItemViewModel> target,
        IEnumerable<CleanupAdvisorCandidate> candidates)
    {
        foreach (var item in target)
        {
            item.PropertyChanged -= OnToolInsightItemPropertyChanged;
        }

        target.Clear();
        foreach (var candidate in candidates)
        {
            if (IsIgnoredPath(candidate.Path))
            {
                continue;
            }

            var item = new ToolInsightItemViewModel
            {
                Title = candidate.Title,
                Detail = candidate.Detail,
                Metric = candidate.Metric,
                Category = candidate.Category,
                Path = candidate.Path,
                Action = $"{candidate.SafetyLabel}: {candidate.Action}",
                SizeBytes = candidate.SizeBytes
            };
            item.PropertyChanged += OnToolInsightItemPropertyChanged;
            target.Add(item);
        }
    }

    private void OnToolInsightItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ToolInsightItemViewModel.IsSelected))
        {
            RaisePropertyChanged(nameof(SelectedAdvancedAuditCount));
        }
    }

    private List<string> GetProtectedPathsSnapshot()
    {
        return ProtectedPathItems
            .Select(item => item.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task SaveProtectedPathsAsync(List<string> paths)
    {
        var current = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
        var updated = new AppSettings
        {
            WatchedFolders = current.WatchedFolders,
            ProtectedPaths = paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            IgnoredPaths = current.IgnoredPaths,
            ExtraScanPaths = current.ExtraScanPaths,
            DefaultDestinationRoot = current.DefaultDestinationRoot,
            Language = current.Language,
            HasCompletedOnboarding = current.HasCompletedOnboarding,
            Theme = current.Theme,
            StartWithWindows = current.StartWithWindows,
            NotificationsEnabled = current.NotificationsEnabled,
            UpdateChecksEnabled = current.UpdateChecksEnabled,
            AutoDownloadUpdates = current.AutoDownloadUpdates,
            OrganizationProfile = current.OrganizationProfile,
            CleanupSchedule = current.CleanupSchedule,
            PermissionNoticeAccepted = current.PermissionNoticeAccepted,
            MinAutoApplyConfidence = current.MinAutoApplyConfidence,
            AutomaticBackupsEnabled = current.AutomaticBackupsEnabled,
            HistoryRetentionDays = current.HistoryRetentionDays,
            StoreDocumentText = current.StoreDocumentText,
            OcrEnabled = current.OcrEnabled,
            HashCheckEnabled = current.HashCheckEnabled
        };

        await _settingsService.SaveAsync(updated, CancellationToken.None);
        _appSettings = updated;
        ApplyProtectedPaths(updated.ProtectedPaths);
        RefreshPermissionSummary();
    }

    private void ApplyProtectedPaths(IEnumerable<string> paths)
    {
        foreach (var item in ProtectedPathItems)
        {
            item.PropertyChanged -= OnProtectedPathItemPropertyChanged;
        }

        ProtectedPathItems.Clear();
        foreach (var path in paths
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase))
        {
            var item = new ToolInsightItemViewModel
            {
                Title = GetFolderDisplayName(path),
                Detail = path,
                Metric = Directory.Exists(path) ? "Bestaat" : "Niet gevonden",
                Category = "Beschermd",
                Path = path,
                Action = "Niet automatisch verwerken"
            };
            item.PropertyChanged += OnProtectedPathItemPropertyChanged;
            ProtectedPathItems.Add(item);
        }

        ProtectedPathsStatus = ProtectedPathItems.Count == 0
            ? "Geen beschermde paden ingesteld"
            : $"{ProtectedPathItems.Count} pad(en) worden niet aangeraakt";
        RaiseSmartToolsStateChanged();
    }

    private void ApplyIgnoredPaths(IEnumerable<string> paths)
    {
        IgnoredPathItems.Clear();
        foreach (var path in paths
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase))
        {
            IgnoredPathItems.Add(new ToolInsightItemViewModel
            {
                Title = GetFolderDisplayName(path),
                Detail = path,
                Metric = Directory.Exists(path) || File.Exists(path) ? "Bestaat" : "Niet gevonden",
                Category = "Genegeerd",
                Path = path,
                Action = "Verbergen uit advieslijsten"
            });
        }

        RaiseSmartToolsStateChanged();
    }

    private void ApplyExtraScanPaths(IEnumerable<string> paths)
    {
        ExtraScanPathItems.Clear();
        foreach (var path in paths
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase))
        {
            ExtraScanPathItems.Add(new ToolInsightItemViewModel
            {
                Title = GetFolderDisplayName(path),
                Detail = path,
                Metric = Directory.Exists(path) ? "Actief" : "Niet gevonden",
                Category = "Extra scan",
                Path = path,
                Action = "Meenemen in strenge scans"
            });
        }

        RaiseSmartToolsStateChanged();
    }

    private List<string> GetIgnoredPathsSnapshot()
    {
        return IgnoredPathItems
            .Select(item => item.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<string> GetExtraScanPathsSnapshot()
    {
        return ExtraScanPathItems
            .Select(item => item.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool IsIgnoredPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return IgnoredPathItems
            .Select(item => item.Path)
            .Where(ignoredPath => !string.IsNullOrWhiteSpace(ignoredPath))
            .Any(ignoredPath => PathsOverlap(ignoredPath!, path));
    }

    private async Task SaveIgnoredPathsAsync(List<string> paths)
    {
        var current = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
        var updated = CloneSettings(
            current,
            ignoredPaths: paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase)
                .ToList());

        await _settingsService.SaveAsync(updated, CancellationToken.None);
        _appSettings = updated;
        ApplyIgnoredPaths(updated.IgnoredPaths);
        RefreshPermissionSummary();
    }

    private async Task SaveExtraScanPathsAsync(List<string> paths)
    {
        var current = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
        var updated = CloneSettings(
            current,
            extraScanPaths: paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase)
                .ToList());

        await _settingsService.SaveAsync(updated, CancellationToken.None);
        _appSettings = updated;
        ApplyExtraScanPaths(updated.ExtraScanPaths);
        RefreshPermissionSummary();
    }

    private void OnProtectedPathItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ToolInsightItemViewModel.IsSelected))
        {
            RemoveSelectedProtectedPathsCommand.RaiseCanExecuteChanged();
        }
    }

    private bool IsProtectedProposal(ProposalItemViewModel proposal)
    {
        return IsProtectedPath(proposal.OriginalPath)
            || IsProtectedPath(proposal.SourceFolder)
            || IsIgnoredPath(proposal.OriginalPath)
            || IsIgnoredPath(proposal.SourceFolder);
    }

    private void RefreshPermissionSummary()
    {
        var watchedCount = WatchedFolders.Count(folder => folder.IsEnabled);
        PermissionSummary =
            $"{watchedCount} bewaakte map(pen), {ProtectedPathCount} niet-aanraken pad(en), {IgnoredPathCount} genegeerd, {ExtraScanPathCount} extra auditpad(en).";
        RaisePropertyChanged(nameof(IgnoredPathCount));
        RaisePropertyChanged(nameof(ExtraScanPathCount));
    }

    private bool IsProtectedPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return ProtectedPathItems
            .Select(item => item.Path)
            .Where(protectedPath => !string.IsNullOrWhiteSpace(protectedPath))
            .Any(protectedPath => PathsOverlap(protectedPath!, path));
    }

    private static bool PathsOverlap(string protectedPath, string candidatePath)
    {
        var protectedFullPath = NormalizePathForCompare(protectedPath);
        var candidateFullPath = NormalizePathForCompare(candidatePath);
        return candidateFullPath.Equals(protectedFullPath, StringComparison.OrdinalIgnoreCase)
            || candidateFullPath.StartsWith(
                protectedFullPath + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
            || candidateFullPath.StartsWith(
                protectedFullPath + Path.AltDirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePathForCompare(string path)
    {
        try
        {
            return Path.GetFullPath(path.Trim())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    private IReadOnlyList<ToolInsightItemViewModel> GetSelectedToolItems()
    {
        return AdvancedAuditItems
            .Concat(AppResidueItems)
            .Concat(DriverCleanupItems)
            .Concat(StorageMapItems)
            .Concat(DryRunItems)
            .Where(item => item.IsSelected)
            .Distinct()
            .ToList();
    }

    private IEnumerable<string> GetSelectedExistingToolPaths()
    {
        return GetSelectedToolItems()
            .Select(item => item.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Where(path => Directory.Exists(path) || File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private void RemoveToolItemsByPath(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return;
        }

        RemoveToolItemsByPath(AdvancedAuditItems, paths);
        RemoveToolItemsByPath(AppResidueItems, paths);
        RemoveToolItemsByPath(DriverCleanupItems, paths);
        RemoveToolItemsByPath(StorageMapItems, paths);
        RemoveToolItemsByPath(DryRunItems, paths);
        RaiseSmartToolsStateChanged();
    }

    private static void RemoveToolItemsByPath(ObservableCollection<ToolInsightItemViewModel> items, IReadOnlyList<string> paths)
    {
        foreach (var item in items
                     .Where(item => item.Path is not null && paths.Any(path => PathsOverlap(path, item.Path!)))
                     .ToList())
        {
            items.Remove(item);
        }
    }

    private static bool OpenPathOrUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri)
                && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                OpenUrl(path);
                return true;
            }

            if (File.Exists(path) || Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool RevealPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                return true;
            }

            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string BuildUniqueToolPath(string targetFolder, string sourcePath)
    {
        var name = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "item";
        }

        var target = Path.Combine(targetFolder, name);
        if (!File.Exists(target) && !Directory.Exists(target))
        {
            return target;
        }

        var extension = Path.GetExtension(name);
        var baseName = string.IsNullOrWhiteSpace(extension) ? name : Path.GetFileNameWithoutExtension(name);
        for (var index = 1; index < 1000; index++)
        {
            var candidate = Path.Combine(targetFolder, $"{baseName}_{index}{extension}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(targetFolder, $"{baseName}_{Guid.NewGuid():N}{extension}");
    }

    private string GetDestinationRoot()
    {
        return string.IsNullOrWhiteSpace(SettingsEditor.DefaultDestinationRoot)
            ? _appSettings?.DefaultDestinationRoot
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DownloadPilot")
            : SettingsEditor.DefaultDestinationRoot.Trim();
    }

    private static AppSettings CloneSettings(
        AppSettings current,
        List<WatchedFolder>? watchedFolders = null,
        List<string>? protectedPaths = null,
        List<string>? ignoredPaths = null,
        List<string>? extraScanPaths = null)
    {
        return new AppSettings
        {
            WatchedFolders = watchedFolders ?? current.WatchedFolders,
            ProtectedPaths = protectedPaths ?? current.ProtectedPaths,
            IgnoredPaths = ignoredPaths ?? current.IgnoredPaths,
            ExtraScanPaths = extraScanPaths ?? current.ExtraScanPaths,
            DefaultDestinationRoot = current.DefaultDestinationRoot,
            Language = current.Language,
            HasCompletedOnboarding = current.HasCompletedOnboarding,
            Theme = current.Theme,
            StartWithWindows = current.StartWithWindows,
            NotificationsEnabled = current.NotificationsEnabled,
            UpdateChecksEnabled = current.UpdateChecksEnabled,
            AutoDownloadUpdates = current.AutoDownloadUpdates,
            OrganizationProfile = current.OrganizationProfile,
            CleanupSchedule = current.CleanupSchedule,
            PermissionNoticeAccepted = current.PermissionNoticeAccepted,
            MinAutoApplyConfidence = current.MinAutoApplyConfidence,
            AutomaticBackupsEnabled = current.AutomaticBackupsEnabled,
            HistoryRetentionDays = current.HistoryRetentionDays,
            StoreDocumentText = current.StoreDocumentText,
            OcrEnabled = current.OcrEnabled,
            HashCheckEnabled = current.HashCheckEnabled
        };
    }

    private static string EscapeCsv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static string SimplifyProcessOutput(string error, string output)
    {
        var value = string.IsNullOrWhiteSpace(error) ? output : error;
        return string.IsNullOrWhiteSpace(value) ? "onbekende fout" : value.Trim();
    }

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private sealed record InvoiceRecord(
        string Company,
        DateTime Date,
        decimal Amount,
        string FileName,
        string TargetFolder);

    private sealed record BrowserSource(string Domain, string Url);

    private void AddWatchedFolder()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Kies map om te bewaken"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        if (WatchedFolders.Any(f => f.Path.Equals(dialog.SelectedPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "Map staat al in de lijst";
            return;
        }

        WatchedFolders.Add(new WatchedFolder { Path = dialog.SelectedPath, IsEnabled = true });
        RefreshPermissionSummary();
        StatusMessage = "Map toegevoegd";
    }

    private void RemoveSelectedWatchedFolder()
    {
        if (SelectedWatchedFolder is null)
        {
            return;
        }

        WatchedFolders.Remove(SelectedWatchedFolder);
        SelectedWatchedFolder = null;
        RefreshPermissionSummary();
        StatusMessage = "Map verwijderd";
    }

    private async Task SaveWatchedFoldersAsync()
    {
        var current = _appSettings ?? await _settingsService.LoadAsync(CancellationToken.None);
        var updated = new AppSettings
        {
            WatchedFolders = [.. WatchedFolders],
            ProtectedPaths = GetProtectedPathsSnapshot(),
            IgnoredPaths = GetIgnoredPathsSnapshot(),
            ExtraScanPaths = GetExtraScanPathsSnapshot(),
            DefaultDestinationRoot = current.DefaultDestinationRoot,
            Language = current.Language,
            HasCompletedOnboarding = _hasCompletedOnboarding,
            Theme = current.Theme,
            StartWithWindows = current.StartWithWindows,
            NotificationsEnabled = current.NotificationsEnabled,
            UpdateChecksEnabled = current.UpdateChecksEnabled,
            AutoDownloadUpdates = current.AutoDownloadUpdates,
            OrganizationProfile = current.OrganizationProfile,
            CleanupSchedule = current.CleanupSchedule,
            PermissionNoticeAccepted = current.PermissionNoticeAccepted,
            MinAutoApplyConfidence = current.MinAutoApplyConfidence,
            AutomaticBackupsEnabled = current.AutomaticBackupsEnabled,
            HistoryRetentionDays = current.HistoryRetentionDays,
            StoreDocumentText = current.StoreDocumentText,
            OcrEnabled = current.OcrEnabled,
            HashCheckEnabled = current.HashCheckEnabled
        };

        await _settingsService.SaveAsync(updated, CancellationToken.None);
        _appSettings = updated;
        RefreshPermissionSummary();

        if (IsMonitoring)
        {
            await StopMonitoringAsync();
            await StartMonitoringAsync();
        }

        StatusMessage = "Bewaakte mappen opgeslagen";
    }

    private void StartNewRule()
    {
        SelectedRule = null;
        RuleEditor.Reset();
        StatusMessage = "Nieuwe regel";
    }

    private async void OnFileReady(object? sender, string filePath)
    {
        try
        {
            var sourceFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
            if (IsProtectedPath(filePath) || IsProtectedPath(sourceFolder) || IsIgnoredPath(filePath) || IsIgnoredPath(sourceFolder))
            {
                RunOnUiThread(() => StatusMessage = $"Niet aangeraakt door beschermde/genegeerde map: {Path.GetFileName(filePath)}");
                return;
            }

            var analysis = await _fileAnalysisService.AnalyzeAsync(filePath, sourceFolder, CancellationToken.None);

            if (_appSettings?.HashCheckEnabled ?? true)
            {
                var isDuplicate = await _duplicateDetectionService.IsExactDuplicateAsync(filePath, CancellationToken.None);
                if (isDuplicate)
                {
                    analysis = new FileAnalysisResult
                    {
                        OriginalPath = analysis.OriginalPath,
                        OriginalFileName = analysis.OriginalFileName,
                        SourceFolder = analysis.SourceFolder,
                        Extension = analysis.Extension,
                        FileSizeBytes = analysis.FileSizeBytes,
                        CreatedLocal = analysis.CreatedLocal,
                        SuggestedCategory = analysis.SuggestedCategory,
                        SuggestedDestinationFolder = analysis.SuggestedDestinationFolder,
                        SuggestedFileName = analysis.SuggestedFileName,
                        Reason = "Mogelijk duplicaat (SHA-256): " + analysis.Reason,
                        Confidence = Math.Min(analysis.Confidence, 70)
                    };
                }
            }

            var rules = await _settingsService.LoadRulesAsync(CancellationToken.None);
            var matchedRule = _ruleEngine.TryApplyRules(analysis, rules);
            var minConfidence = _appSettings?.MinAutoApplyConfidence ?? 85;
            var adviceOnly = ResolveWorkflowMode(_appSettings ?? new AppSettings()).Equals("Alleen advies", StringComparison.OrdinalIgnoreCase);
            if (matchedRule is not null
                && matchedRule.IsAutoApplied
                && !adviceOnly
                && analysis.Confidence >= minConfidence
                && !IsProtectedPath(analysis.OriginalPath))
            {
                var entry = await _fileOperationService.MoveAndRenameAsync(matchedRule, CancellationToken.None);
                RememberSessionEntry(entry);
                RunOnUiThread(() =>
                {
                    if (entry.Status == HistoryStatus.Geslaagd)
                    {
                        OrganizedFilesCount++;
                        StatusMessage = $"Automatisch verwerkt: {analysis.OriginalFileName}";
                    }
                    else
                    {
                        StatusMessage = $"Automatische actie mislukt: {entry.ErrorMessage}";
                    }
                });
                await RefreshHistoryAsync();
                return;
            }

            AddProposal(analysis);
            if (_appSettings?.NotificationsEnabled ?? true)
            {
                _notificationService.ShowProposal(analysis);
            }
            RunOnUiThread(() => StatusMessage = $"Nieuw voorstel: {analysis.OriginalFileName}");
        }
        catch (Exception ex)
        {
            RunOnUiThread(() => StatusMessage = $"Analysefout: {ex.Message}");
        }
    }

    private void AddProposal(FileAnalysisResult analysis)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (IsProtectedPath(analysis.OriginalPath)
                || IsProtectedPath(analysis.SourceFolder)
                || IsIgnoredPath(analysis.OriginalPath)
                || IsIgnoredPath(analysis.SourceFolder))
            {
                StatusMessage = $"Voorstel genegeerd door niet-aanraken/negeerlijst: {analysis.OriginalFileName}";
                return;
            }

            if (NewFileProposals.Any(proposal =>
                proposal.OriginalPath.Equals(analysis.OriginalPath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var proposal = new ProposalItemViewModel(analysis);
            proposal.PropertyChanged += OnProposalPropertyChanged;
            NewFileProposals.Add(proposal);

            if (analysis.Reason.Contains("duplicaat", StringComparison.OrdinalIgnoreCase))
            {
                DuplicateProposals.Add(proposal);
            }
        });
    }

    private void RemoveProposal(ProposalItemViewModel proposal)
    {
        proposal.PropertyChanged -= OnProposalPropertyChanged;
        proposal.IsSelected = false;
        NewFileProposals.Remove(proposal);
        DuplicateProposals.Remove(proposal);

        if (ReferenceEquals(SelectedProposal, proposal))
        {
            SelectedProposal = null;
        }

        UpdateProposalSelectionState();
    }

    private void OnProposalPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ProposalItemViewModel.IsSelected))
        {
            UpdateProposalSelectionState();
            return;
        }

        if (args.PropertyName is nameof(ProposalItemViewModel.TargetFolder)
            or nameof(ProposalItemViewModel.TargetFileName))
        {
            RefreshWorkflowInsights();
        }
    }

    private void UpdateProposalSelectionState()
    {
        RaisePropertyChanged(nameof(SelectedProposalsCount));
        RaisePropertyChanged(nameof(SelectedDuplicateProposalsCount));
        RaiseProposalCommandStates();
    }

    private void RaiseProposalCommandStates()
    {
        ProcessAllProposalsCommand.RaiseCanExecuteChanged();
        ProcessSelectedProposalsCommand.RaiseCanExecuteChanged();
        SelectAllProposalsCommand.RaiseCanExecuteChanged();
        ClearProposalSelectionCommand.RaiseCanExecuteChanged();
        IgnoreSelectedProposalsCommand.RaiseCanExecuteChanged();
        SelectAllDuplicatesCommand.RaiseCanExecuteChanged();
        ClearDuplicateSelectionCommand.RaiseCanExecuteChanged();
        MoveSelectedDuplicatesToRecycleBinCommand.RaiseCanExecuteChanged();
        ProcessSafeQueueCommand.RaiseCanExecuteChanged();
        RunDryRunCommand.RaiseCanExecuteChanged();
        ProtectSelectedProposalFolderCommand.RaiseCanExecuteChanged();
    }

    private async Task RefreshHistoryAsync()
    {
        var items = await _historyService.GetRecentAsync(200, CancellationToken.None);

        _historyCache.Clear();
        _historyCache.AddRange(items);
        ApplyHistoryFilter();
        RefreshSmartToolInsights();
    }

    private void ApplyHistoryFilter()
    {
        IEnumerable<HistoryEntry> filtered = _historyCache;

        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-((int)today.DayOfWeek + 6) % 7);

        filtered = SelectedHistoryFilter switch
        {
            "Vandaag" => filtered.Where(i => i.TimestampLocal.Date == today),
            "Deze week" => filtered.Where(i => i.TimestampLocal.Date >= startOfWeek),
            "Verplaatst" => filtered.Where(i => i.ActionType == HistoryActionType.Verplaats),
            "Hernoemd" => filtered.Where(i => i.ActionType == HistoryActionType.Hernoem),
            "Automatisch uitgevoerd" => filtered.Where(i => i.IsAutoApplied),
            "Mislukt" => filtered.Where(i => i.Status == HistoryStatus.Mislukt),
            "Teruggedraaid" => filtered.Where(i => i.Status == HistoryStatus.Teruggedraaid),
            _ => filtered
        };

        Application.Current.Dispatcher.Invoke(() =>
        {
            SelectedHistoryEntry = null;
            RecentHistory.Clear();
            foreach (var item in filtered.Take(50))
            {
                RecentHistory.Add(item);
            }
        });
    }

    private static bool MatchesProposal(object item, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        if (item is not ProposalItemViewModel proposal)
        {
            return false;
        }

        return Contains(proposal.OriginalFileName, query)
            || Contains(proposal.Category, query)
            || Contains(proposal.Reason, query)
            || Contains(proposal.TargetFolder, query)
            || Contains(proposal.TargetFileName, query);
    }

    private static bool MatchesRule(object item, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        if (item is not RuleDefinition rule)
        {
            return false;
        }

        return Contains(rule.Name, query)
            || Contains(rule.ExtensionEquals, query)
            || Contains(rule.FileNameContains, query)
            || Contains(rule.Category.ToString(), query)
            || Contains(rule.DestinationFolder, query);
    }

    private static bool MatchesHistory(object item, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        if (item is not HistoryEntry entry)
        {
            return false;
        }

        return Contains(entry.OriginalName, query)
            || Contains(entry.NewName, query)
            || Contains(entry.OriginalPath, query)
            || Contains(entry.NewPath, query)
            || Contains(entry.RuleName, query)
            || Contains(entry.ActionType.ToString(), query)
            || Contains(entry.Status.ToString(), query)
            || Contains(entry.ErrorMessage, query);
    }

    private async Task RefreshSelectedPreviewAsync(ProposalItemViewModel? proposal)
    {
        var requestId = Interlocked.Increment(ref _previewRequestId);

        if (proposal is null)
        {
            SetPreviewFallback(
                "Geen bestand geselecteerd",
                "Selecteer een voorstel om een snelle preview te zien.");
            return;
        }

        if (!File.Exists(proposal.OriginalPath))
        {
            SetPreviewFallback(
                "Bestand niet gevonden",
                "Het bestand staat niet meer op de oorspronkelijke locatie.");
            OpenSelectedOriginalCommand.RaiseCanExecuteChanged();
            RevealSelectedOriginalCommand.RaiseCanExecuteChanged();
            return;
        }

        var extension = proposal.Analysis.Extension.ToLowerInvariant();
        if (IsImagePreviewExtension(extension))
        {
            if (requestId != _previewRequestId)
            {
                return;
            }

            SelectedPreviewTitle = "Afbeeldingspreview";
            SelectedPreviewText = $"{proposal.FileSizeReadable} - gemaakt op {proposal.CreatedLocalReadable}";
            SelectedPreviewImagePath = proposal.OriginalPath;
            IsSelectedPreviewImageVisible = true;
            IsSelectedPreviewTextVisible = false;
            IsSelectedPreviewFallbackVisible = false;
            return;
        }

        if (IsTextPreviewExtension(extension))
        {
            try
            {
                var preview = await ReadTextPreviewAsync(proposal.OriginalPath, CancellationToken.None);
                if (requestId != _previewRequestId)
                {
                    return;
                }

                SelectedPreviewTitle = "Tekstpreview";
                SelectedPreviewText = string.IsNullOrWhiteSpace(preview)
                    ? "Geen leesbare tekst gevonden."
                    : preview;
                SelectedPreviewImagePath = null;
                IsSelectedPreviewImageVisible = false;
                IsSelectedPreviewTextVisible = true;
                IsSelectedPreviewFallbackVisible = false;
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (requestId == _previewRequestId)
                {
                    SetPreviewFallback("Preview niet beschikbaar", ex.Message);
                }

                return;
            }
        }

        SetPreviewFallback(
            GetFallbackPreviewTitle(extension),
            $"{proposal.Reason}\n\nOrigineel: {proposal.OriginalPath}\nDoel: {proposal.TargetFullPath}");
    }

    private void SetPreviewFallback(string title, string text)
    {
        SelectedPreviewTitle = title;
        SelectedPreviewText = text;
        SelectedPreviewImagePath = null;
        IsSelectedPreviewImageVisible = false;
        IsSelectedPreviewTextVisible = false;
        IsSelectedPreviewFallbackVisible = true;
    }

    private static async Task<string> ReadTextPreviewAsync(string path, CancellationToken cancellationToken)
    {
        const int maxChars = 6000;
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            useAsync: true);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: false);

        var buffer = new char[maxChars + 1];
        var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        var text = new string(buffer, 0, Math.Min(read, maxChars));
        text = RemoveUnreadableControlCharacters(text);

        return read > maxChars
            ? text + Environment.NewLine + Environment.NewLine + "... preview afgekapt ..."
            : text;
    }

    private static string RemoveUnreadableControlCharacters(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (!char.IsControl(character) || character is '\r' or '\n' or '\t')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static bool IsImagePreviewExtension(string extension)
    {
        return extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tif" or ".tiff";
    }

    private static bool IsTextPreviewExtension(string extension)
    {
        return extension is ".txt" or ".csv" or ".json" or ".xml" or ".log" or ".md" or ".ini"
            or ".config" or ".yaml" or ".yml" or ".ps1" or ".bat" or ".cmd" or ".cs" or ".xaml";
    }

    private static string GetFallbackPreviewTitle(string extension)
    {
        return extension switch
        {
            ".pdf" => "PDF-bestand",
            ".zip" or ".rar" or ".7z" => "Archiefbestand",
            ".exe" or ".msi" => "Installatiebestand",
            _ => "Bestandspreview"
        };
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase) == true;
    }

    private static void RunOnUiThread(Action action)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        Application.Current.Dispatcher.Invoke(action);
    }

    private void ApplySettingsToEditor(AppSettings settings)
    {
        SettingsEditor.DefaultDestinationRoot = settings.DefaultDestinationRoot;
        SettingsEditor.Theme = settings.Theme;
        SettingsEditor.MinAutoApplyConfidence = settings.MinAutoApplyConfidence;
        SettingsEditor.AutomaticBackupsEnabled = settings.AutomaticBackupsEnabled;
        SettingsEditor.HistoryRetentionDays = settings.HistoryRetentionDays;
        SettingsEditor.NotificationsEnabled = settings.NotificationsEnabled;
        SettingsEditor.UpdateChecksEnabled = settings.UpdateChecksEnabled;
        SettingsEditor.AutoDownloadUpdates = settings.AutoDownloadUpdates;
        SettingsEditor.OrganizationProfile = ResolveWorkflowMode(settings);
        SettingsEditor.CleanupSchedule = string.IsNullOrWhiteSpace(settings.CleanupSchedule)
            ? "Wekelijks"
            : settings.CleanupSchedule;
        CleanupScheduleStatus = $"Planning: {SettingsEditor.CleanupSchedule}";
        SettingsEditor.PermissionNoticeAccepted = settings.PermissionNoticeAccepted;
        SettingsEditor.StoreDocumentText = settings.StoreDocumentText;
        SettingsEditor.StartWithWindows = settings.StartWithWindows;
        SettingsEditor.OcrEnabled = settings.OcrEnabled;
        SettingsEditor.HashCheckEnabled = settings.HashCheckEnabled;
    }

    private void ApplyWatchedFolders(IEnumerable<WatchedFolder> folders)
    {
        WatchedFolders.Clear();
        foreach (var folder in folders)
        {
            WatchedFolders.Add(new WatchedFolder
            {
                Path = folder.Path,
                IsEnabled = folder.IsEnabled
            });
        }
    }

    private static IReadOnlyList<string> GetExistingScanFolders(AppSettings settings)
    {
        var folders = settings.WatchedFolders
            .Where(folder => folder.IsEnabled)
            .Select(folder => folder.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var downloadsFallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        if (Directory.Exists(downloadsFallback) &&
            !folders.Contains(downloadsFallback, StringComparer.OrdinalIgnoreCase))
        {
            folders.Add(downloadsFallback);
        }

        return folders;
    }

    private void UpdateScanSummary(IReadOnlyList<FileAnalysisResult> analyses)
    {
        ScanFileCount = analyses.Count;
        ScanTotalSizeBytes = analyses.Sum(a => a.FileSizeBytes);
        ScanOldFilesCount = analyses.Count(a => a.CreatedLocal <= DateTime.Now.AddDays(-30));
        ScanLargeFilesCount = analyses.Count(a => a.FileSizeBytes >= 100L * 1024L * 1024L);
        ScanArchiveCount = analyses.Count(a => a.SuggestedCategory == FileCategory.Archieven);
        ScanInstallerCount = analyses.Count(a => a.SuggestedCategory == FileCategory.Installatiebestanden);
        ScanUncategorizedCount = analyses.Count(a => a.SuggestedCategory == FileCategory.Overig);
        ScanLikelySafeMoveCount = analyses.Count(a => a.Confidence >= 85);

        var duplicatesByNameAndSize = analyses
            .GroupBy(a => new { Name = a.OriginalFileName.ToLowerInvariant(), a.FileSizeBytes })
            .Where(g => g.Count() > 1)
            .Sum(g => g.Count());
        ScanPossibleDuplicatesCount = duplicatesByNameAndSize;
        RaisePropertyChanged(nameof(ScanTotalSizeReadable));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}

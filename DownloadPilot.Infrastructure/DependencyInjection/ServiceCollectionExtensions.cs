using DownloadPilot.Core.Abstractions;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DownloadPilot.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDownloadPilotInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<SqliteConnectionFactory>();

        services.AddSingleton<IFileStabilityService, FileStabilityService>();
        services.AddSingleton<IClassificationService, ClassificationService>();
        services.AddSingleton<IPdfTextExtractionService, PdfTextExtractionService>();
        services.AddSingleton<IOcrService, OcrService>();
        services.AddSingleton<IDocumentRecognitionService, DocumentRecognitionService>();
        services.AddSingleton<IFileAnalysisService, FileAnalysisService>();
        services.AddSingleton<IRuleEngine, RuleEngine>();
        services.AddSingleton<IFileHashService, FileHashService>();
        services.AddSingleton<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddSingleton<IArchiveExtractionService, ArchiveExtractionService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileOperationService, FileOperationService>();
        services.AddSingleton<IUndoService, UndoService>();
        services.AddSingleton<IStartupRegistrationService, StartupRegistrationService>();
        services.AddSingleton<IMailSpamFilterService, MailSpamFilterService>();
        services.AddSingleton<IUpdateService, GitHubUpdateService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IFolderWatchService, FolderWatchService>();

        return services;
    }
}

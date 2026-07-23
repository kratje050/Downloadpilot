using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;

namespace DownloadPilot.Infrastructure.Services;

public sealed class FileOperationService(
    IFileHashService fileHashService,
    IHistoryService historyService,
    ILogger<FileOperationService> logger) : IFileOperationService
{
    public async Task<HistoryEntry> MoveAndRenameAsync(FileOperationRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.TargetFolder);

        var safeFileName = FileNameSanitizer.Sanitize(request.TargetFileName);
        var uniquePath = FileNameSanitizer.BuildUniquePath(request.TargetFolder, safeFileName);

        var history = new HistoryEntry
        {
            TimestampLocal = DateTime.Now,
            OriginalPath = request.Analysis.OriginalPath,
            NewPath = uniquePath,
            OriginalName = request.Analysis.OriginalFileName,
            NewName = Path.GetFileName(uniquePath),
            RuleName = request.AppliedRuleName,
            ActionType = HistoryActionType.Verplaats,
            Status = HistoryStatus.Geslaagd,
            IsAutoApplied = request.IsAutoApplied,
            CanUndo = true,
            Sha256Hash = null
        };

        try
        {
            if (!File.Exists(request.Analysis.OriginalPath))
            {
                throw new FileNotFoundException("Bestand bestaat niet meer", request.Analysis.OriginalPath);
            }

            File.Move(request.Analysis.OriginalPath, uniquePath, overwrite: false);

            var successEntry = new HistoryEntry
            {
                TimestampLocal = history.TimestampLocal,
                OriginalPath = history.OriginalPath,
                NewPath = history.NewPath,
                OriginalName = history.OriginalName,
                NewName = history.NewName,
                RuleName = history.RuleName,
                ActionType = history.ActionType,
                Status = history.Status,
                IsAutoApplied = history.IsAutoApplied,
                CanUndo = history.CanUndo,
                ErrorMessage = history.ErrorMessage,
                Sha256Hash = await fileHashService.ComputeSha256Async(uniquePath, cancellationToken)
            };

            var id = await historyService.AddEntryAsync(successEntry, cancellationToken);
            return new HistoryEntry
            {
                Id = id,
                TimestampLocal = successEntry.TimestampLocal,
                OriginalPath = successEntry.OriginalPath,
                NewPath = successEntry.NewPath,
                OriginalName = successEntry.OriginalName,
                NewName = successEntry.NewName,
                RuleName = successEntry.RuleName,
                ActionType = successEntry.ActionType,
                Status = successEntry.Status,
                IsAutoApplied = successEntry.IsAutoApplied,
                CanUndo = successEntry.CanUndo,
                ErrorMessage = successEntry.ErrorMessage,
                Sha256Hash = successEntry.Sha256Hash
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Verplaatsen mislukt voor {Path}", request.Analysis.OriginalPath);
            var failedEntry = new HistoryEntry
            {
                TimestampLocal = history.TimestampLocal,
                OriginalPath = history.OriginalPath,
                NewPath = history.NewPath,
                OriginalName = history.OriginalName,
                NewName = history.NewName,
                RuleName = history.RuleName,
                ActionType = history.ActionType,
                Status = HistoryStatus.Mislukt,
                CanUndo = false,
                IsAutoApplied = history.IsAutoApplied,
                Sha256Hash = history.Sha256Hash,
                ErrorMessage = ex.Message
            };
            await historyService.AddEntryAsync(failedEntry, cancellationToken);
            return failedEntry;
        }
    }
}

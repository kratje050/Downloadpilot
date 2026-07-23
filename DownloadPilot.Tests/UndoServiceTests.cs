using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace DownloadPilot.Tests;

public sealed class UndoServiceTests
{
    [Fact]
    public async Task UndoLastAsync_ShouldRestoreMovedFile()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var sourceFolder = Path.Combine(root, "src");
        var targetFolder = Path.Combine(root, "dst");
        Directory.CreateDirectory(sourceFolder);
        Directory.CreateDirectory(targetFolder);

        var dbPath = Path.Combine(root, "test.db");
        var sourcePath = Path.Combine(sourceFolder, "document.pdf");
        await File.WriteAllTextAsync(sourcePath, "hello");

        var connectionFactory = new SqliteConnectionFactory(dbPath);
        IHistoryService history = new HistoryService(connectionFactory);
        await history.InitializeAsync(CancellationToken.None);

        var fileOperation = new FileOperationService(
            new FileHashService(),
            history,
            NullLogger<FileOperationService>.Instance);

        var analysis = new FileAnalysisResult
        {
            OriginalPath = sourcePath,
            OriginalFileName = "document.pdf",
            SourceFolder = sourceFolder,
            Extension = ".pdf",
            FileSizeBytes = 10,
            CreatedLocal = DateTime.Now,
            SuggestedCategory = FileCategory.Documenten,
            SuggestedDestinationFolder = targetFolder,
            SuggestedFileName = "x.pdf",
            Reason = "test",
            Confidence = 90
        };

        var request = new FileOperationRequest
        {
            Analysis = analysis,
            TargetFolder = targetFolder,
            TargetFileName = "x.pdf"
        };

        var moveResult = await fileOperation.MoveAndRenameAsync(request, CancellationToken.None);
        Assert.Equal(HistoryStatus.Geslaagd, moveResult.Status);

        var undo = new UndoService(history);
        var undone = await undo.UndoLastAsync(CancellationToken.None);

        Assert.True(undone);
        Assert.True(File.Exists(sourcePath));

        SqliteConnection.ClearAllPools();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task UndoAsync_ShouldRestoreSelectedOlderEntryOnly()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourceFolder = Path.Combine(root, "src");
            var targetFolder = Path.Combine(root, "dst");
            Directory.CreateDirectory(sourceFolder);
            Directory.CreateDirectory(targetFolder);

            var history = new HistoryService(new SqliteConnectionFactory(Path.Combine(root, "test.db")));
            await history.InitializeAsync(CancellationToken.None);
            var fileOperation = new FileOperationService(
                new FileHashService(),
                history,
                NullLogger<FileOperationService>.Instance);

            var firstSource = Path.Combine(sourceFolder, "first.txt");
            var secondSource = Path.Combine(sourceFolder, "second.txt");
            await File.WriteAllTextAsync(firstSource, "first");
            await File.WriteAllTextAsync(secondSource, "second");

            async Task<HistoryEntry> MoveAsync(string sourcePath, string targetName)
            {
                var analysis = new FileAnalysisResult
                {
                    OriginalPath = sourcePath,
                    OriginalFileName = Path.GetFileName(sourcePath),
                    SourceFolder = sourceFolder,
                    Extension = ".txt",
                    FileSizeBytes = new FileInfo(sourcePath).Length,
                    CreatedLocal = DateTime.Now,
                    SuggestedCategory = FileCategory.Documenten,
                    SuggestedDestinationFolder = targetFolder,
                    SuggestedFileName = targetName,
                    Reason = "test",
                    Confidence = 90
                };

                return await fileOperation.MoveAndRenameAsync(new FileOperationRequest
                {
                    Analysis = analysis,
                    TargetFolder = targetFolder,
                    TargetFileName = targetName
                }, CancellationToken.None);
            }

            var firstMove = await MoveAsync(firstSource, "first-moved.txt");
            var secondMove = await MoveAsync(secondSource, "second-moved.txt");

            var undo = new UndoService(history);
            var result = await undo.UndoAsync(firstMove.Id, CancellationToken.None);

            Assert.True(result);
            Assert.True(File.Exists(firstSource));
            Assert.False(File.Exists(firstMove.NewPath));
            Assert.False(File.Exists(secondSource));
            Assert.True(File.Exists(secondMove.NewPath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}

using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Services;

namespace DownloadPilot.Tests;

public sealed class RuleEngineTests
{
    [Fact]
    public void TryApplyRules_ShouldUseHighestPriorityMatch()
    {
        var engine = new RuleEngine();

        var analysis = new FileAnalysisResult
        {
            OriginalPath = "C:\\temp\\factuur.pdf",
            OriginalFileName = "factuur.pdf",
            SourceFolder = "C:\\temp",
            Extension = ".pdf",
            FileSizeBytes = 100,
            CreatedLocal = DateTime.Now,
            SuggestedCategory = FileCategory.Documenten,
            SuggestedDestinationFolder = "C:\\doel",
            SuggestedFileName = "a.pdf",
            Reason = "test",
            Confidence = 90
        };

        var low = new RuleDefinition
        {
            Id = 1,
            Name = "Laag",
            ExtensionEquals = ".pdf",
            Priority = 1,
            Category = FileCategory.Documenten,
            DestinationFolder = "C:\\laag"
        };

        var high = new RuleDefinition
        {
            Id = 2,
            Name = "Hoog",
            ExtensionEquals = ".pdf",
            Priority = 99,
            Category = FileCategory.Facturen,
            DestinationFolder = "C:\\hoog"
        };

        var result = engine.TryApplyRules(analysis, [low, high]);

        Assert.NotNull(result);
        Assert.Equal("Hoog", result!.AppliedRuleName);
        Assert.Equal("C:\\hoog", result.TargetFolder);
    }

    [Fact]
    public void TryApplyRules_ShouldKeepSmartDestinationWhenRuleHasNoFixedFolder()
    {
        var engine = new RuleEngine();

        var analysis = new FileAnalysisResult
        {
            OriginalPath = "C:\\temp\\factuur.pdf",
            OriginalFileName = "factuur.pdf",
            SourceFolder = "C:\\temp",
            Extension = ".pdf",
            FileSizeBytes = 100,
            CreatedLocal = DateTime.Now,
            SuggestedCategory = FileCategory.Facturen,
            SuggestedDestinationFolder = "C:\\doel\\Facturen\\Coolblue",
            SuggestedFileName = "factuur.pdf",
            Reason = "test",
            Confidence = 90
        };

        var smartRule = new RuleDefinition
        {
            Id = 1,
            Name = "Slimme facturen",
            ExtensionEquals = ".pdf",
            Priority = 99,
            Category = FileCategory.Facturen,
            DestinationFolder = null
        };

        var result = engine.TryApplyRules(analysis, [smartRule]);

        Assert.NotNull(result);
        Assert.Equal("C:\\doel\\Facturen\\Coolblue", result!.TargetFolder);
    }
}

using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;

namespace DownloadPilot.Infrastructure.Services;

public sealed class RuleEngine : IRuleEngine
{
    public FileOperationRequest? TryApplyRules(FileAnalysisResult analysis, IReadOnlyList<RuleDefinition> rules)
    {
        foreach (var rule in rules.OrderByDescending(r => r.Priority))
        {
            if (!MatchesRule(analysis, rule))
            {
                continue;
            }

            var targetFolder = rule.DestinationFolder ?? analysis.SuggestedDestinationFolder;
            var targetFileName = rule.RenameTemplate is null
                ? analysis.SuggestedFileName
                : ApplyTemplate(rule.RenameTemplate, analysis);

            return new FileOperationRequest
            {
                Analysis = analysis,
                TargetFolder = targetFolder,
                TargetFileName = targetFileName,
                AppliedRuleName = rule.Name,
                IsAutoApplied = rule.AutoApply
            };
        }

        return null;
    }

    private static bool MatchesRule(FileAnalysisResult analysis, RuleDefinition rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.ExtensionEquals) &&
            !analysis.Extension.Equals(rule.ExtensionEquals, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.FileNameContains) &&
            !analysis.OriginalFileName.Contains(rule.FileNameContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.SourceFolderContains) &&
            !analysis.SourceFolder.Contains(rule.SourceFolderContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string ApplyTemplate(string template, FileAnalysisResult analysis)
    {
        return template
            .Replace("{datum}", analysis.CreatedLocal.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{categorie}", analysis.SuggestedCategory.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{origineel}", Path.GetFileNameWithoutExtension(analysis.OriginalFileName), StringComparison.OrdinalIgnoreCase)
            + analysis.Extension;
    }
}

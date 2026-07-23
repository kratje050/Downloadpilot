using DownloadPilot.Core.Models;

namespace DownloadPilot.Core.Abstractions;

public interface IRuleEngine
{
    FileOperationRequest? TryApplyRules(FileAnalysisResult analysis, IReadOnlyList<RuleDefinition> rules);
}

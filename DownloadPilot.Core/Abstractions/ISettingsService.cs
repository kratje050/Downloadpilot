using DownloadPilot.Core.Models;

namespace DownloadPilot.Core.Abstractions;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);

    Task<IReadOnlyList<RuleDefinition>> LoadRulesAsync(CancellationToken cancellationToken);

    Task<int> UpsertRuleAsync(RuleDefinition rule, CancellationToken cancellationToken);

    Task DeleteRuleAsync(int ruleId, CancellationToken cancellationToken);
}

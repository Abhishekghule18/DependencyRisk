using DependencyRisk.Core.Entities;

namespace DependencyRisk.Core.Interfaces;

public interface IAiSummarizer
{
    Task<string> Summarize(string packageName, RiskScore score, RepoMetrics metrics);
}

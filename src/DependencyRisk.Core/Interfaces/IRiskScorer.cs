using DependencyRisk.Core.Entities;

namespace DependencyRisk.Core.Interfaces;

public interface IRiskScorer
{
    RiskScore Calculate(RepoMetrics metrics);
}

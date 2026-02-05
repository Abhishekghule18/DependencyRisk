namespace DependencyRisk.Core.Entities;

public class Dependency
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string Ecosystem { get; set; } = string.Empty; // 'nuget', 'npm', 'pypi'
    public string? GitHubRepoUrl { get; set; }

    public Project Project { get; set; } = null!;
    public List<RiskScore> RiskScores { get; set; } = new();
}

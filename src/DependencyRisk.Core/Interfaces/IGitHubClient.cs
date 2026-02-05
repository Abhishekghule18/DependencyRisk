namespace DependencyRisk.Core.Interfaces;

public record RepoMetrics(
    int Stars,
    int Forks,
    int OpenIssues,
    bool IsOrgOwned,
    int ContributorCount,
    DateTime? LastCommitDate,
    DateTime? LastReleaseDate,
    string? License,
    bool Archived,
    double IssueCloseRatio,
    double AvgIssueResponseHours
);

public interface IGitHubClient
{
    Task<RepoMetrics?> GetMetrics(string owner, string repo);
    Task<string?> ResolveGitHubUrl(string packageName, string ecosystem);
}

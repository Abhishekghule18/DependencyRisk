namespace DependencyRisk.Core.DTOs;

public record DependencyRiskDto(
    int DependencyId,
    string PackageName,
    string? Version,
    string Ecosystem,
    string? GitHubRepoUrl,
    int MaintainerScore,
    int ActivityScore,
    int IssueHealthScore,
    int ReleaseScore,
    int CommunityScore,
    int LicenseScore,
    decimal OverallScore,
    string RiskLevel,
    string? AiSummary
);

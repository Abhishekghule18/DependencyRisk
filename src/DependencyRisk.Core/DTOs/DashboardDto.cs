namespace DependencyRisk.Core.DTOs;

public record DashboardSummaryDto(
    int TotalProjects,
    int TotalDependencies,
    int TotalScans,
    int CriticalCount,
    int HighCount,
    int MediumCount,
    int LowCount
);

public record ProjectSummaryDto(
    int ProjectId,
    string ProjectName,
    string FileType,
    DateTime? LastScannedAt,
    decimal? LatestScore,
    int TotalDeps,
    int HighRiskCount
);

public record TrendPointDto(DateTime ScannedAt, decimal OverallScore, int HighRiskCount);

public record WorstDependencyDto(
    string PackageName,
    string Ecosystem,
    string? GitHubRepoUrl,
    decimal OverallScore,
    string RiskLevel,
    string ProjectName,
    string? AiSummary
);

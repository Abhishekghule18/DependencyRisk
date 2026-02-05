namespace DependencyRisk.Core.DTOs;

public record ScanResultDto(
    int ScanId,
    int ProjectId,
    string ProjectName,
    DateTime ScannedAt,
    int TotalDeps,
    int HighRiskCount,
    int MediumRiskCount,
    int LowRiskCount,
    decimal OverallScore,
    List<DependencyRiskDto> Dependencies
);

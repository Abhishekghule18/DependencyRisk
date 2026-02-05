namespace DependencyRisk.Core.Entities;

public class RiskScore
{
    public int Id { get; set; }
    public int ScanId { get; set; }
    public int DependencyId { get; set; }

    public int MaintainerScore { get; set; }
    public int ActivityScore { get; set; }
    public int IssueHealthScore { get; set; }
    public int ReleaseScore { get; set; }
    public int CommunityScore { get; set; }
    public int LicenseScore { get; set; }

    public decimal OverallScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty; // 'Low', 'Medium', 'High', 'Critical'

    public string? AiSummary { get; set; }
    public string? RawMetrics { get; set; }

    public Scan Scan { get; set; } = null!;
    public Dependency Dependency { get; set; } = null!;
}

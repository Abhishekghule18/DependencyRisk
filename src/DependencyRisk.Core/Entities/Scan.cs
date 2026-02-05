namespace DependencyRisk.Core.Entities;

public class Scan
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public int TotalDeps { get; set; }
    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int LowRiskCount { get; set; }
    public decimal OverallScore { get; set; }

    public Project Project { get; set; } = null!;
    public List<RiskScore> RiskScores { get; set; } = new();
}

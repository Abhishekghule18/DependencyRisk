using DependencyRisk.Core.Entities;
using DependencyRisk.Core.Interfaces;

namespace DependencyRisk.Infrastructure.Scoring;

public class WeightedRiskScorer : IRiskScorer
{
    private static readonly Dictionary<string, int> Weights = new()
    {
        ["maintainer"] = 25,
        ["activity"] = 25,
        ["issueHealth"] = 15,
        ["release"] = 15,
        ["community"] = 10,
        ["license"] = 10
    };

    public RiskScore Calculate(RepoMetrics metrics)
    {
        var scores = new Dictionary<string, int>
        {
            ["maintainer"] = ScoreMaintainers(metrics),
            ["activity"] = ScoreActivity(metrics),
            ["issueHealth"] = ScoreIssueHealth(metrics),
            ["release"] = ScoreReleases(metrics),
            ["community"] = ScoreCommunity(metrics),
            ["license"] = ScoreLicense(metrics)
        };

        var overall = (decimal)scores.Sum(kvp => kvp.Value * Weights[kvp.Key]) / 100m;

        return new RiskScore
        {
            MaintainerScore = scores["maintainer"],
            ActivityScore = scores["activity"],
            IssueHealthScore = scores["issueHealth"],
            ReleaseScore = scores["release"],
            CommunityScore = scores["community"],
            LicenseScore = scores["license"],
            OverallScore = Math.Min(100m, overall),
            RiskLevel = overall switch
            {
                >= 80 => "Low",
                >= 60 => "Medium",
                >= 40 => "High",
                _ => "Critical"
            }
        };
    }

    private static int ScoreMaintainers(RepoMetrics m)
    {
        var score = m.ContributorCount switch
        {
            1 => 20,
            <= 3 => 50,
            <= 10 => 80,
            _ => 100
        };
        if (m.IsOrgOwned) score = Math.Min(100, score + 10);
        return score;
    }

    private static int ScoreActivity(RepoMetrics m)
    {
        if (m.Archived) return 0;
        if (m.LastCommitDate == null) return 5;

        var days = (DateTime.UtcNow - m.LastCommitDate.Value).TotalDays;
        return days switch
        {
            <= 30 => 100,
            <= 90 => 90,
            <= 180 => 60,
            <= 365 => 30,
            _ => 10
        };
    }

    private static int ScoreIssueHealth(RepoMetrics m)
    {
        var ratioScore = (int)(m.IssueCloseRatio * 100);
        var responseScore = m.AvgIssueResponseHours switch
        {
            <= 24 => 100,
            <= 72 => 80,
            <= 168 => 60,
            <= 720 => 40,
            _ => 20
        };
        return (ratioScore + responseScore) / 2;
    }

    private static int ScoreReleases(RepoMetrics m)
    {
        if (m.LastReleaseDate == null) return 20;
        var days = (DateTime.UtcNow - m.LastReleaseDate.Value).TotalDays;
        return days switch
        {
            <= 30 => 100,
            <= 90 => 85,
            <= 180 => 65,
            <= 365 => 40,
            _ => 15
        };
    }

    private static int ScoreCommunity(RepoMetrics m)
    {
        var starScore = m.Stars switch
        {
            >= 10000 => 100,
            >= 1000 => 80,
            >= 100 => 60,
            >= 10 => 40,
            _ => 20
        };
        var forkScore = m.Forks switch
        {
            >= 1000 => 100,
            >= 100 => 80,
            >= 10 => 60,
            _ => 30
        };
        return (starScore + forkScore) / 2;
    }

    private static int ScoreLicense(RepoMetrics m)
    {
        if (string.IsNullOrEmpty(m.License)) return 20;
        return m.License.ToUpper() switch
        {
            "MIT" or "APACHE-2.0" or "BSD-2-CLAUSE" or "BSD-3-CLAUSE" or "ISC" => 100,
            "GPL-2.0" or "GPL-3.0" or "LGPL-2.0" or "LGPL-2.1" or "LGPL-3.0" => 70,
            "AGPL-3.0" => 50,
            "NOASSERTION" => 30,
            _ => 60
        };
    }
}

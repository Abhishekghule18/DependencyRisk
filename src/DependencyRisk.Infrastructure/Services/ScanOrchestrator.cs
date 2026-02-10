using System.Text.Json;
using DependencyRisk.Core.DTOs;
using DependencyRisk.Core.Entities;
using DependencyRisk.Core.Interfaces;
using DependencyRisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DependencyRisk.Infrastructure.Services;

public class ScanOrchestrator
{
    private readonly AppDbContext _db;
    private readonly IGitHubClient _github;
    private readonly IRiskScorer _scorer;
    private readonly IAiSummarizer _ai;
    private readonly ILogger<ScanOrchestrator> _logger;

    public ScanOrchestrator(
        AppDbContext db,
        IGitHubClient github,
        IRiskScorer scorer,
        IAiSummarizer ai,
        ILogger<ScanOrchestrator> logger)
    {
        _db = db;
        _github = github;
        _scorer = scorer;
        _ai = ai;
        _logger = logger;
    }

    public async Task<ScanResultDto> ExecuteScan(int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Dependencies)
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException($"Project {projectId} not found");

        // Resolve GitHub URLs for any deps missing them
        foreach (var dep in project.Dependencies.Where(d => d.GitHubRepoUrl == null))
        {
            dep.GitHubRepoUrl = await _github.ResolveGitHubUrl(dep.PackageName, dep.Ecosystem);
        }
        await _db.SaveChangesAsync();

        // Fetch metrics in parallel (max 5 concurrent GitHub calls)
        var semaphore = new SemaphoreSlim(5);
        var depsWithUrl = project.Dependencies.Where(d => d.GitHubRepoUrl != null).ToList();

        var tasks = depsWithUrl.Select(async dep =>
        {
            await semaphore.WaitAsync();
            try
            {
                var (owner, repo) = ParseGitHubUrl(dep.GitHubRepoUrl!);
                var metrics = await _github.GetMetrics(owner, repo);
                if (metrics == null) return (dep, (RiskScore?)null, (RepoMetrics?)null);

                var score = _scorer.Calculate(metrics);
                score.AiSummary = await _ai.Summarize(dep.PackageName, score, metrics);
                score.RawMetrics = JsonSerializer.Serialize(metrics);
                return (dep, (RiskScore?)score, (RepoMetrics?)metrics);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process {Package}", dep.PackageName);
                return (dep, (RiskScore?)null, (RepoMetrics?)null);
            }
            finally { semaphore.Release(); }
        });

        var results = await Task.WhenAll(tasks);
        var validResults = results.Where(r => r.Item2 != null).ToList();

        // Create scan record
        var scan = new Scan
        {
            ProjectId = projectId,
            ScannedAt = DateTime.UtcNow,
            TotalDeps = project.Dependencies.Count,
            HighRiskCount = validResults.Count(r => r.Item2!.RiskLevel is "High" or "Critical"),
            MediumRiskCount = validResults.Count(r => r.Item2!.RiskLevel == "Medium"),
            LowRiskCount = validResults.Count(r => r.Item2!.RiskLevel == "Low"),
            OverallScore = validResults.Count > 0
                ? Math.Round(validResults.Average(r => r.Item2!.OverallScore), 2)
                : 0
        };

        _db.Scans.Add(scan);
        await _db.SaveChangesAsync();

        // Save risk scores
        foreach (var (dep, score, _) in validResults)
        {
            score!.ScanId = scan.Id;
            score.DependencyId = dep.Id;
            _db.RiskScores.Add(score);
        }

        project.LastScannedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToDto(scan, project.Name, validResults);
    }

    private static (string owner, string repo) ParseGitHubUrl(string url)
    {
        var uri = new Uri(url.TrimEnd('/'));
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        if (parts.Length < 2)
            throw new ArgumentException($"Cannot parse GitHub URL: {url}");
        return (parts[0], parts[1]);
    }

    private static ScanResultDto MapToDto(
        Scan scan,
        string projectName,
        List<(Dependency dep, RiskScore? score, RepoMetrics? metrics)> results)
    {
        var deps = results
            .Where(r => r.score != null)
            .Select(r => new DependencyRiskDto(
                r.dep.Id,
                r.dep.PackageName,
                r.dep.Version,
                r.dep.Ecosystem,
                r.dep.GitHubRepoUrl,
                r.score!.MaintainerScore,
                r.score.ActivityScore,
                r.score.IssueHealthScore,
                r.score.ReleaseScore,
                r.score.CommunityScore,
                r.score.LicenseScore,
                r.score.OverallScore,
                r.score.RiskLevel,
                r.score.AiSummary
            )).ToList();

        return new ScanResultDto(
            scan.Id,
            scan.ProjectId,
            projectName,
            scan.ScannedAt,
            scan.TotalDeps,
            scan.HighRiskCount,
            scan.MediumRiskCount,
            scan.LowRiskCount,
            scan.OverallScore,
            deps
        );
    }
}

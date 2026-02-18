using DependencyRisk.Core.DTOs;
using DependencyRisk.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DependencyRisk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    /// <summary>Overall stats across all projects</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var totalProjects = await _db.Projects.CountAsync();
        var totalDeps = await _db.Dependencies.CountAsync();
        var totalScans = await _db.Scans.CountAsync();

        // Count risk levels from latest scans per project
        var latestScanIds = await _db.Scans
            .GroupBy(s => s.ProjectId)
            .Select(g => g.OrderByDescending(s => s.ScannedAt).First().Id)
            .ToListAsync();

        var riskCounts = await _db.RiskScores
            .Where(r => latestScanIds.Contains(r.ScanId))
            .GroupBy(r => r.RiskLevel)
            .Select(g => new { RiskLevel = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new DashboardSummaryDto(
            totalProjects,
            totalDeps,
            totalScans,
            riskCounts.FirstOrDefault(r => r.RiskLevel == "Critical")?.Count ?? 0,
            riskCounts.FirstOrDefault(r => r.RiskLevel == "High")?.Count ?? 0,
            riskCounts.FirstOrDefault(r => r.RiskLevel == "Medium")?.Count ?? 0,
            riskCounts.FirstOrDefault(r => r.RiskLevel == "Low")?.Count ?? 0
        ));
    }

    /// <summary>Score trends over time for a project</summary>
    [HttpGet("trends/{projectId:int}")]
    public async Task<IActionResult> Trends(int projectId)
    {
        var trends = await _db.Scans
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.ScannedAt)
            .Select(s => new TrendPointDto(s.ScannedAt, s.OverallScore, s.HighRiskCount))
            .ToListAsync();

        return Ok(trends);
    }

    /// <summary>Top 10 riskiest dependencies across all projects</summary>
    [HttpGet("worst")]
    public async Task<IActionResult> Worst()
    {
        var latestScanIds = await _db.Scans
            .GroupBy(s => s.ProjectId)
            .Select(g => g.OrderByDescending(s => s.ScannedAt).First().Id)
            .ToListAsync();

        var worst = await _db.RiskScores
            .Include(r => r.Dependency)
                .ThenInclude(d => d.Project)
            .Where(r => latestScanIds.Contains(r.ScanId))
            .OrderBy(r => r.OverallScore)
            .Take(10)
            .Select(r => new WorstDependencyDto(
                r.Dependency.PackageName,
                r.Dependency.Ecosystem,
                r.Dependency.GitHubRepoUrl,
                r.OverallScore,
                r.RiskLevel,
                r.Dependency.Project.Name,
                r.AiSummary
            ))
            .ToListAsync();

        return Ok(worst);
    }
}

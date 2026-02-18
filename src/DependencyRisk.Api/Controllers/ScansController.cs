using DependencyRisk.Core.DTOs;
using DependencyRisk.Infrastructure.Data;
using DependencyRisk.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DependencyRisk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScansController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ScanOrchestrator _orchestrator;
    private readonly ILogger<ScansController> _logger;

    public ScansController(AppDbContext db, ScanOrchestrator orchestrator, ILogger<ScansController> logger)
    {
        _db = db;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>Trigger a new scan for a project</summary>
    [HttpPost("{projectId:int}")]
    public async Task<IActionResult> TriggerScan(int projectId)
    {
        var exists = await _db.Projects.AnyAsync(p => p.Id == projectId);
        if (!exists) return NotFound($"Project {projectId} not found.");

        _logger.LogInformation("Starting scan for project {ProjectId}", projectId);
        var result = await _orchestrator.ExecuteScan(projectId);
        return Ok(result);
    }

    /// <summary>Get scan results with all dependency scores</summary>
    [HttpGet("{scanId:int}")]
    public async Task<IActionResult> GetScan(int scanId)
    {
        var scan = await _db.Scans
            .Include(s => s.Project)
            .Include(s => s.RiskScores)
                .ThenInclude(r => r.Dependency)
            .FirstOrDefaultAsync(s => s.Id == scanId);

        if (scan == null) return NotFound();

        return Ok(new ScanResultDto(
            scan.Id,
            scan.ProjectId,
            scan.Project.Name,
            scan.ScannedAt,
            scan.TotalDeps,
            scan.HighRiskCount,
            scan.MediumRiskCount,
            scan.LowRiskCount,
            scan.OverallScore,
            scan.RiskScores.Select(r => new DependencyRiskDto(
                r.DependencyId,
                r.Dependency.PackageName,
                r.Dependency.Version,
                r.Dependency.Ecosystem,
                r.Dependency.GitHubRepoUrl,
                r.MaintainerScore,
                r.ActivityScore,
                r.IssueHealthScore,
                r.ReleaseScore,
                r.CommunityScore,
                r.LicenseScore,
                r.OverallScore,
                r.RiskLevel,
                r.AiSummary
            )).ToList()
        ));
    }

    /// <summary>Get heatmap-formatted data for a scan</summary>
    [HttpGet("{scanId:int}/heatmap")]
    public async Task<IActionResult> GetHeatmap(int scanId)
    {
        var scores = await _db.RiskScores
            .Include(r => r.Dependency)
            .Where(r => r.ScanId == scanId)
            .Select(r => new
            {
                r.Dependency.PackageName,
                r.Dependency.Ecosystem,
                r.OverallScore,
                r.RiskLevel,
                r.MaintainerScore,
                r.ActivityScore,
                r.IssueHealthScore,
                r.ReleaseScore,
                r.CommunityScore,
                r.LicenseScore
            })
            .OrderBy(r => r.OverallScore)
            .ToListAsync();

        return Ok(scores);
    }

    /// <summary>List all scans for a project</summary>
    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetScansForProject(int projectId)
    {
        var scans = await _db.Scans
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.ScannedAt)
            .Select(s => new
            {
                s.Id,
                s.ScannedAt,
                s.TotalDeps,
                s.HighRiskCount,
                s.MediumRiskCount,
                s.LowRiskCount,
                s.OverallScore
            })
            .ToListAsync();

        return Ok(scans);
    }
}

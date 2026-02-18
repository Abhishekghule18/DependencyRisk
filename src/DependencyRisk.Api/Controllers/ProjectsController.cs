using DependencyRisk.Core.DTOs;
using DependencyRisk.Core.Entities;
using DependencyRisk.Core.Interfaces;
using DependencyRisk.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DependencyRisk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IFileParser> _parsers;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(AppDbContext db, IEnumerable<IFileParser> parsers, ILogger<ProjectsController> logger)
    {
        _db = db;
        _parsers = parsers;
        _logger = logger;
    }

    /// <summary>Upload a dependency file and create a project</summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var ext = Path.GetExtension(file.FileName).ToLower();
        var parser = _parsers.FirstOrDefault(p => p.SupportedExtension == ext);
        if (parser == null)
            return BadRequest($"Unsupported file type '{ext}'. Supported: .csproj, .json, .txt");

        List<ParsedDependency> parsed;
        using (var stream = file.OpenReadStream())
            parsed = parser.Parse(stream);

        if (parsed.Count == 0)
            return BadRequest("No dependencies found in the uploaded file.");

        var fileType = ext switch
        {
            ".csproj" => "csproj",
            ".json" => "package.json",
            ".txt" => "requirements.txt",
            _ => ext.TrimStart('.')
        };

        var project = new Project
        {
            Name = Path.GetFileNameWithoutExtension(file.FileName),
            FileType = fileType,
            CreatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var deps = parsed
            .DistinctBy(p => p.Name)
            .Select(p => new Dependency
            {
                ProjectId = project.Id,
                PackageName = p.Name,
                Version = p.Version,
                Ecosystem = p.Ecosystem
            })
            .ToList();

        _db.Dependencies.AddRange(deps);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created project {Id} with {Count} dependencies", project.Id, deps.Count);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, new
        {
            project.Id,
            project.Name,
            project.FileType,
            project.CreatedAt,
            DependencyCount = deps.Count
        });
    }

    /// <summary>List all projects</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _db.Projects
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Name,
                p.FileType,
                p.LastScannedAt,
                p.Scans.OrderByDescending(s => s.ScannedAt).Select(s => (decimal?)s.OverallScore).FirstOrDefault(),
                p.Dependencies.Count,
                p.Scans.OrderByDescending(s => s.ScannedAt).Select(s => s.HighRiskCount).FirstOrDefault()
            ))
            .ToListAsync();

        return Ok(projects);
    }

    /// <summary>Get project with latest scan summary</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await _db.Projects
            .Include(p => p.Dependencies)
            .Include(p => p.Scans)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return NotFound();

        var latestScan = project.Scans.OrderByDescending(s => s.ScannedAt).FirstOrDefault();

        return Ok(new
        {
            project.Id,
            project.Name,
            project.FileType,
            project.CreatedAt,
            project.LastScannedAt,
            DependencyCount = project.Dependencies.Count,
            LatestScan = latestScan == null ? null : new
            {
                latestScan.Id,
                latestScan.ScannedAt,
                latestScan.OverallScore,
                latestScan.HighRiskCount,
                latestScan.MediumRiskCount,
                latestScan.LowRiskCount,
                latestScan.TotalDeps
            }
        });
    }

    /// <summary>Delete a project</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return NotFound();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

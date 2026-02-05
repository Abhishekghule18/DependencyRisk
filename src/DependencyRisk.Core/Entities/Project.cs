namespace DependencyRisk.Core.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // 'csproj', 'package.json', 'requirements.txt'
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastScannedAt { get; set; }

    public List<Dependency> Dependencies { get; set; } = new();
    public List<Scan> Scans { get; set; } = new();
}

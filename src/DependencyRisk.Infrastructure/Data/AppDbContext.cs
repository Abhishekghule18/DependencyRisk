using DependencyRisk.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DependencyRisk.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<RiskScore> RiskScores => Set<RiskScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.FileType).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Dependency>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.PackageName).HasMaxLength(300).IsRequired();
            e.Property(d => d.Ecosystem).HasMaxLength(50).IsRequired();
            e.Property(d => d.Version).HasMaxLength(100);
            e.Property(d => d.GitHubRepoUrl).HasMaxLength(500);
            e.HasIndex(d => new { d.ProjectId, d.PackageName }).IsUnique();
            e.HasOne(d => d.Project)
                .WithMany(p => p.Dependencies)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Scan>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.OverallScore).HasColumnType("decimal(5,2)");
            e.HasOne(s => s.Project)
                .WithMany(p => p.Scans)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RiskScore>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.OverallScore).HasColumnType("decimal(5,2)");
            e.Property(r => r.RiskLevel).HasMaxLength(20).IsRequired();
            e.HasIndex(r => new { r.ScanId, r.DependencyId }).IsUnique();
            e.HasIndex(r => new { r.DependencyId, r.ScanId }).IncludeProperties(r => new { r.OverallScore, r.RiskLevel });
            e.HasOne(r => r.Scan)
                .WithMany(s => s.RiskScores)
                .HasForeignKey(r => r.ScanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Dependency)
                .WithMany(d => d.RiskScores)
                .HasForeignKey(r => r.DependencyId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}

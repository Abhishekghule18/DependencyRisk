namespace DependencyRisk.Core.Interfaces;

public record ParsedDependency(string Name, string? Version, string Ecosystem);

public interface IFileParser
{
    string SupportedExtension { get; }
    List<ParsedDependency> Parse(Stream fileStream);
}

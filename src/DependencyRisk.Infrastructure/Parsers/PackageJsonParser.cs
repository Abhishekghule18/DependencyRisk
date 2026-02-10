using System.Text.Json;
using DependencyRisk.Core.Interfaces;

namespace DependencyRisk.Infrastructure.Parsers;

public class PackageJsonParser : IFileParser
{
    public string SupportedExtension => ".json";

    public List<ParsedDependency> Parse(Stream fileStream)
    {
        var doc = JsonDocument.Parse(fileStream);
        var root = doc.RootElement;
        var deps = new List<ParsedDependency>();

        foreach (var section in new[] { "dependencies", "devDependencies" })
        {
            if (root.TryGetProperty(section, out var depSection))
            {
                foreach (var dep in depSection.EnumerateObject())
                {
                    deps.Add(new ParsedDependency(dep.Name, dep.Value.GetString(), "npm"));
                }
            }
        }

        return deps;
    }
}

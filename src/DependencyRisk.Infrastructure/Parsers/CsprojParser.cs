using System.Xml.Linq;
using DependencyRisk.Core.Interfaces;

namespace DependencyRisk.Infrastructure.Parsers;

public class CsprojParser : IFileParser
{
    public string SupportedExtension => ".csproj";

    public List<ParsedDependency> Parse(Stream fileStream)
    {
        var doc = XDocument.Load(fileStream);
        return doc.Descendants("PackageReference")
            .Select(pr => new ParsedDependency(
                pr.Attribute("Include")?.Value ?? "",
                pr.Attribute("Version")?.Value ?? pr.Element("Version")?.Value,
                "nuget"))
            .Where(d => !string.IsNullOrEmpty(d.Name))
            .ToList();
    }
}

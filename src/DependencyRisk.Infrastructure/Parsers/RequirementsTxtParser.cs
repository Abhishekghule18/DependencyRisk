using DependencyRisk.Core.Interfaces;

namespace DependencyRisk.Infrastructure.Parsers;

public class RequirementsTxtParser : IFileParser
{
    public string SupportedExtension => ".txt";

    public List<ParsedDependency> Parse(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        var lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return lines
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith('#') && !l.StartsWith('-'))
            .Select(ParseLine)
            .Where(d => !string.IsNullOrEmpty(d.Name))
            .ToList();
    }

    private static ParsedDependency ParseLine(string line)
    {
        // formats: package, package==1.0, package>=1.0, package~=1.0
        var separators = new[] { "==", ">=", "<=", "~=", "!=", ">" , "<" };
        foreach (var sep in separators)
        {
            var idx = line.IndexOf(sep, StringComparison.Ordinal);
            if (idx >= 0)
                return new ParsedDependency(line[..idx].Trim(), line[(idx + sep.Length)..].Trim(), "pypi");
        }
        return new ParsedDependency(line.Trim(), null, "pypi");
    }
}

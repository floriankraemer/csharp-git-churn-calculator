using System.Globalization;
using System.Xml.Linq;

namespace GitChurnCalculator.Services;

public sealed class CoberturaXmlParser : ICoverageParser
{
    public Dictionary<string, double> MapToTrackedFiles(
        Dictionary<string, double> coverageByPath,
        IReadOnlyList<string> trackedGitRelativePaths) =>
        CoveragePathMatcher.MapToGitFiles(coverageByPath, trackedGitRelativePaths);

    public Dictionary<string, double> Parse(string coverageFilePath)
    {
        var doc = XDocument.Load(coverageFilePath);
        var root = doc.Root ?? throw new InvalidOperationException("Cobertura XML has no root element.");

        var sourcePrefixes = root
            .Descendants("source")
            .Select(e => CoveragePathMatcher.NormalizePath(e.Value))
            .Where(s => s.Length > 0)
            .ToList();

        var coverage = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var classEl in root.Descendants("class"))
        {
            var filename = classEl.Attribute("filename")?.Value;
            if (string.IsNullOrWhiteSpace(filename))
                continue;

            var lineRateStr = classEl.Attribute("line-rate")?.Value;
            if (lineRateStr is null ||
                !double.TryParse(lineRateStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lineRate))
                continue;

            var coveragePercent = lineRate * 100.0;
            var normalized = CoveragePathMatcher.NormalizePath(filename);
            var relativePath = MakeRelative(normalized, sourcePrefixes);

            // Multiple <class> elements can map to the same file; keep the max coverage.
            if (!coverage.TryGetValue(relativePath, out var existing) || coveragePercent > existing)
                coverage[relativePath] = coveragePercent;
        }

        return coverage;
    }

    private static string MakeRelative(string normalizedPath, List<string> sourcePrefixes)
    {
        foreach (var prefix in sourcePrefixes)
        {
            var prefixWithSlash = prefix.EndsWith('/') ? prefix : prefix + "/";
            if (!normalizedPath.StartsWith(prefixWithSlash, StringComparison.OrdinalIgnoreCase))
                continue;
            return normalizedPath[prefixWithSlash.Length..];
        }
        return normalizedPath;
    }
}

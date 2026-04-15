using System.Globalization;
using System.Xml.Linq;

namespace GitChurnCalculator.Services;

public sealed class CoberturaXmlParser : ICoberturaParser
{
    public Dictionary<string, double> MapToTrackedFiles(
        Dictionary<string, double> coverageByPath,
        IReadOnlyList<string> trackedGitRelativePaths) =>
        MapToGitFiles(coverageByPath, trackedGitRelativePaths);

    public Dictionary<string, double> Parse(string coberturaFilePath)
    {
        var doc = XDocument.Load(coberturaFilePath);
        var root = doc.Root ?? throw new InvalidOperationException("Cobertura XML has no root element.");

        var sourcePrefixes = root
            .Descendants("source")
            .Select(e => NormalizePath(e.Value))
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
            var normalized = NormalizePath(filename);
            var relativePath = MakeRelative(normalized, sourcePrefixes);

            // Multiple <class> elements can map to the same file; keep the max coverage.
            if (coverage.TryGetValue(relativePath, out var existing))
            {
                if (coveragePercent > existing)
                    coverage[relativePath] = coveragePercent;
            }
            else
            {
                coverage[relativePath] = coveragePercent;
            }
        }

        return coverage;
    }

    /// <summary>
    /// Attempts to match a Cobertura file path to a git-tracked file path.
    /// Uses the source prefixes from the Cobertura XML to strip absolute path prefixes,
    /// then falls back to suffix matching against the provided git file list.
    /// </summary>
    public static Dictionary<string, double> MapToGitFiles(
        Dictionary<string, double> coverageByPath,
        IReadOnlyList<string> gitFiles)
    {
        var gitFileLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var gf in gitFiles)
            gitFileLookup[NormalizePath(gf)] = gf;

        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rawCovPath, percent) in coverageByPath)
        {
            var covPath = NormalizePath(rawCovPath);

            // 1. Exact match
            if (gitFileLookup.TryGetValue(covPath, out var exactMatch))
            {
                result[exactMatch] = percent;
                continue;
            }

            // 2. Suffix match: find a git file that ends with the coverage path
            var matched = false;
            foreach (var (normalizedGit, originalGit) in gitFileLookup)
            {
                if (normalizedGit.EndsWith("/" + covPath, StringComparison.OrdinalIgnoreCase) ||
                    normalizedGit.Equals(covPath, StringComparison.OrdinalIgnoreCase))
                {
                    result[originalGit] = percent;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                // 3. Try matching just the filename portion
                var fileName = covPath.Split('/').Last();
                foreach (var (normalizedGit, originalGit) in gitFileLookup)
                {
                    if (normalizedGit.Split('/').Last().Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        result[originalGit] = percent;
                        break;
                    }
                }
            }
        }

        return result;
    }

    public static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }

    private static string MakeRelative(string normalizedPath, List<string> sourcePrefixes)
    {
        foreach (var prefix in sourcePrefixes)
        {
            var prefixWithSlash = prefix.EndsWith('/') ? prefix : prefix + "/";
            if (normalizedPath.StartsWith(prefixWithSlash, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath[prefixWithSlash.Length..];
            }
        }
        return normalizedPath;
    }
}

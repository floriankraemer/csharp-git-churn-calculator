namespace GitChurnCalculator.Services;

/// <summary>
/// Shared utilities for matching coverage file paths to git-tracked file paths.
/// </summary>
public static class CoveragePathMatcher
{
    /// <summary>
    /// Attempts to match coverage file paths to git-tracked file paths using
    /// exact match, suffix match, and filename-only match (in that priority order).
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
            if (TryGetSuffixGitMatch(gitFileLookup, covPath, out var suffixOriginal))
            {
                result[suffixOriginal] = percent;
                continue;
            }

            // 3. Try matching just the filename portion
            var fileName = covPath.Split('/').Last();
            if (TryGetGitMatchByFileName(gitFileLookup, fileName, out var nameOriginal))
                result[nameOriginal] = percent;
        }

        return result;
    }

    public static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }

    private static bool TryGetSuffixGitMatch(
        Dictionary<string, string> gitFileLookup,
        string covPath,
        out string originalGit)
    {
        foreach (var (normalizedGit, orig) in gitFileLookup)
        {
            if (normalizedGit.EndsWith("/" + covPath, StringComparison.OrdinalIgnoreCase) ||
                covPath.EndsWith("/" + normalizedGit, StringComparison.OrdinalIgnoreCase) ||
                normalizedGit.Equals(covPath, StringComparison.OrdinalIgnoreCase))
            {
                originalGit = orig;
                return true;
            }
        }

        originalGit = null!;
        return false;
    }

    private static bool TryGetGitMatchByFileName(
        Dictionary<string, string> gitFileLookup,
        string fileName,
        out string originalGit)
    {
        foreach (var (normalizedGit, orig) in gitFileLookup)
        {
            if (normalizedGit.Split('/').Last().Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                originalGit = orig;
                return true;
            }
        }

        originalGit = null!;
        return false;
    }
}

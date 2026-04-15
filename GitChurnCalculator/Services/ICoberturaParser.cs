namespace GitChurnCalculator.Services;

public interface ICoberturaParser
{
    /// <summary>
    /// Parses a Cobertura XML file and returns a mapping of normalized file paths to line coverage percent (0-100).
    /// </summary>
    Dictionary<string, double> Parse(string coberturaFilePath);

    /// <summary>
    /// Maps Cobertura file paths to paths as reported by git (tracked paths).
    /// </summary>
    Dictionary<string, double> MapToTrackedFiles(
        Dictionary<string, double> coverageByPath,
        IReadOnlyList<string> trackedGitRelativePaths);
}

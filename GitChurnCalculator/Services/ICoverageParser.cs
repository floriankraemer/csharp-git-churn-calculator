namespace GitChurnCalculator.Services;

public interface ICoverageParser
{
    /// <summary>
    /// Parses a coverage XML file and returns a mapping of file paths to line coverage percent (0-100).
    /// </summary>
    Dictionary<string, double> Parse(string coverageFilePath);

    /// <summary>
    /// Maps coverage file paths to paths as reported by git (tracked paths).
    /// </summary>
    Dictionary<string, double> MapToTrackedFiles(
        Dictionary<string, double> coverageByPath,
        IReadOnlyList<string> trackedGitRelativePaths);
}

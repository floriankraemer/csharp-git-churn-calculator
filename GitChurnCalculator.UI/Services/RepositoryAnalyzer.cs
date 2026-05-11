using GitChurnCalculator.Models;
using GitChurnCalculator.Services;
using GitChurnCalculator.UI.Models;

namespace GitChurnCalculator.UI.Services;

public sealed class RepositoryAnalyzer
{
    private readonly IChurnCalculator _calculator;

    public RepositoryAnalyzer(IChurnCalculator calculator) => _calculator = calculator;

    public Task<IReadOnlyList<FileChurnResult>> AnalyzeAsync(AppSettings settings, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.LastRepositoryPath))
            throw new InvalidOperationException("Select a repository first.");

        if (!Directory.Exists(settings.LastRepositoryPath))
            throw new InvalidOperationException($"Repository does not exist: {settings.LastRepositoryPath}");

        var options = new ChurnAnalysisOptions
        {
            RepositoryPath = settings.LastRepositoryPath,
            CoverageFilePath = string.IsNullOrWhiteSpace(settings.CoverageFilePath) ? null : settings.CoverageFilePath,
            IncludePattern = string.IsNullOrWhiteSpace(settings.IncludePattern) ? null : settings.IncludePattern,
            ExcludePattern = string.IsNullOrWhiteSpace(settings.ExcludePattern) ? null : settings.ExcludePattern,
            AsOf = settings.AsOf,
        };

        return _calculator.AnalyzeAsync(options, ct);
    }
}

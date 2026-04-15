using GitChurnCalculator.Models;

namespace GitChurnCalculator.Services;

public sealed class ChurnCalculator
{
    private readonly IGitDataProvider _gitDataProvider;
    private readonly ICoberturaParser _coberturaParser;

    public ChurnCalculator(IGitDataProvider gitDataProvider, ICoberturaParser coberturaParser)
    {
        _gitDataProvider = gitDataProvider;
        _coberturaParser = coberturaParser;
    }

    public async Task<IReadOnlyList<FileChurnResult>> AnalyzeAsync(
        ChurnAnalysisOptions options,
        CancellationToken ct = default)
    {
        var repoPath = options.RepositoryPath;
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);
        var yearAgo = now.AddDays(-365);

        var trackedFiles = await _gitDataProvider.GetTrackedFilesAsync(repoPath, ct);

        // Run independent git queries in parallel
        var commitCountsTask = _gitDataProvider.GetCommitCountsAsync(repoPath, ct);
        var firstDatesTask = _gitDataProvider.GetFirstCommitDatesAsync(repoPath, ct);
        var lastDatesTask = _gitDataProvider.GetLastCommitDatesAsync(repoPath, ct);
        var commits7Task = _gitDataProvider.GetCommitCountsSinceAsync(repoPath, sevenDaysAgo, ct);
        var commits30Task = _gitDataProvider.GetCommitCountsSinceAsync(repoPath, thirtyDaysAgo, ct);
        var commits365Task = _gitDataProvider.GetCommitCountsSinceAsync(repoPath, yearAgo, ct);
        var authorsAllTask = _gitDataProvider.GetUniqueAuthorCountsAsync(repoPath, ct);
        var authors7Task = _gitDataProvider.GetUniqueAuthorCountsSinceAsync(repoPath, sevenDaysAgo, ct);
        var authors30Task = _gitDataProvider.GetUniqueAuthorCountsSinceAsync(repoPath, thirtyDaysAgo, ct);
        var authors365Task = _gitDataProvider.GetUniqueAuthorCountsSinceAsync(repoPath, yearAgo, ct);

        await Task.WhenAll(
            commitCountsTask, firstDatesTask, lastDatesTask,
            commits7Task, commits30Task, commits365Task,
            authorsAllTask, authors7Task, authors30Task, authors365Task);

        var commitCounts = commitCountsTask.Result;
        var firstDates = firstDatesTask.Result;
        var lastDates = lastDatesTask.Result;
        var commits7 = commits7Task.Result;
        var commits30 = commits30Task.Result;
        var commits365 = commits365Task.Result;
        var authorsAll = authorsAllTask.Result;
        var authors7 = authors7Task.Result;
        var authors30 = authors30Task.Result;
        var authors365 = authors365Task.Result;

        // Parse coverage if provided
        Dictionary<string, double>? coverageMap = null;
        if (!string.IsNullOrEmpty(options.CoberturaFilePath))
        {
            var rawCoverage = _coberturaParser.Parse(options.CoberturaFilePath);
            coverageMap = CoberturaXmlParser.MapToGitFiles(rawCoverage, trackedFiles);
        }

        var results = new List<FileChurnResult>(trackedFiles.Count);

        foreach (var file in trackedFiles)
        {
            var totalCommits = commitCounts.GetValueOrDefault(file, 0);
            if (totalCommits == 0)
                continue;

            firstDates.TryGetValue(file, out var firstDate);
            lastDates.TryGetValue(file, out var lastDate);

            var ageDays = firstDate != default
                ? Math.Max(1, (int)(now - firstDate).TotalDays)
                : 1;

            var ageWeeks = ageDays / 7.0;
            var ageMonths = ageDays / 30.44;
            var ageYears = ageDays / 365.25;

            var changesPerWeek = totalCommits / ageWeeks;
            var changesPerMonth = totalCommits / ageMonths;
            var changesPerYear = totalCommits / ageYears;

            var totalUniqueAuthors = authorsAll.GetValueOrDefault(file, 0);

            double? coveragePercent = coverageMap?.GetValueOrDefault(file, 0.0);
            if (coverageMap is null)
                coveragePercent = null;

            var churnRiskScore = CalculateChurnRiskScore(
                changesPerWeek, totalUniqueAuthors, coveragePercent);

            results.Add(new FileChurnResult
            {
                FilePath = file,
                TotalCommits = totalCommits,
                FirstCommitDate = firstDate != default ? firstDate : null,
                LastCommitDate = lastDate != default ? lastDate : null,
                AgeDays = ageDays,
                ChangesPerWeek = Math.Round(changesPerWeek, 2),
                ChangesPerMonth = Math.Round(changesPerMonth, 2),
                ChangesPerYear = Math.Round(changesPerYear, 2),
                CommitsLast7Days = commits7.GetValueOrDefault(file, 0),
                CommitsLast30Days = commits30.GetValueOrDefault(file, 0),
                CommitsLast365Days = commits365.GetValueOrDefault(file, 0),
                TotalUniqueAuthors = totalUniqueAuthors,
                UniqueAuthorsLast7Days = authors7.GetValueOrDefault(file, 0),
                UniqueAuthorsLast30Days = authors30.GetValueOrDefault(file, 0),
                UniqueAuthorsLast365Days = authors365.GetValueOrDefault(file, 0),
                CoveragePercent = coveragePercent.HasValue ? Math.Round(coveragePercent.Value, 2) : null,
                ChurnRiskScore = churnRiskScore,
            });
        }

        results.Sort((a, b) => b.ChurnRiskScore.CompareTo(a.ChurnRiskScore));
        return results;
    }

    /// <summary>
    /// ChurnRiskScore = ChangesPerWeek * TotalUniqueAuthors * (1 - CoveragePercent / 100)
    /// When no coverage data is available, the risk multiplier is 1.0.
    /// </summary>
    public static double CalculateChurnRiskScore(
        double changesPerWeek,
        int totalUniqueAuthors,
        double? coveragePercent)
    {
        var riskMultiplier = coveragePercent.HasValue
            ? 1.0 - (coveragePercent.Value / 100.0)
            : 1.0;

        var score = changesPerWeek * totalUniqueAuthors * riskMultiplier;
        return Math.Round(score, 4);
    }
}

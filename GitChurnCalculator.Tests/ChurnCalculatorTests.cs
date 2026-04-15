using GitChurnCalculator.Models;
using GitChurnCalculator.Services;
using NSubstitute;
using Xunit;

namespace GitChurnCalculator.Tests;

public class ChurnCalculatorTests
{
    [Fact]
    public void CalculateChurnRiskScore_WithoutCoverage_ReturnsChangesTimesAuthors()
    {
        var score = ChurnCalculator.CalculateChurnRiskScore(
            changesPerWeek: 5.0,
            totalUniqueAuthors: 3,
            coveragePercent: null);

        Assert.Equal(15.0, score);
    }

    [Fact]
    public void CalculateChurnRiskScore_WithZeroCoverage_SameAsNoCoverage()
    {
        var score = ChurnCalculator.CalculateChurnRiskScore(
            changesPerWeek: 5.0,
            totalUniqueAuthors: 3,
            coveragePercent: 0.0);

        Assert.Equal(15.0, score);
    }

    [Fact]
    public void CalculateChurnRiskScore_With50PercentCoverage_HalvesScore()
    {
        var score = ChurnCalculator.CalculateChurnRiskScore(
            changesPerWeek: 10.0,
            totalUniqueAuthors: 4,
            coveragePercent: 50.0);

        Assert.Equal(20.0, score);
    }

    [Fact]
    public void CalculateChurnRiskScore_With100PercentCoverage_ReturnsZero()
    {
        var score = ChurnCalculator.CalculateChurnRiskScore(
            changesPerWeek: 12.0,
            totalUniqueAuthors: 8,
            coveragePercent: 100.0);

        Assert.Equal(0.0, score);
    }

    [Fact]
    public void CalculateChurnRiskScore_WithZeroAuthors_ReturnsZero()
    {
        var score = ChurnCalculator.CalculateChurnRiskScore(
            changesPerWeek: 10.0,
            totalUniqueAuthors: 0,
            coveragePercent: null);

        Assert.Equal(0.0, score);
    }

    [Fact]
    public void CalculateChurnRiskScore_WithZeroChanges_ReturnsZero()
    {
        var score = ChurnCalculator.CalculateChurnRiskScore(
            changesPerWeek: 0.0,
            totalUniqueAuthors: 5,
            coveragePercent: null);

        Assert.Equal(0.0, score);
    }

    [Fact]
    public void CalculateChurnRiskScore_GrokExample_CorrectResult()
    {
        // From the Grok conversation: ChangesPerWeek=12.45, Authors=8, Coverage=35%
        // Expected: 12.45 * 8 * (1 - 35/100) = 12.45 * 8 * 0.65 = 64.74
        var score = ChurnCalculator.CalculateChurnRiskScore(
            changesPerWeek: 12.45,
            totalUniqueAuthors: 8,
            coveragePercent: 35.0);

        Assert.Equal(64.74, score);
    }

    [Fact]
    public async Task AnalyzeAsync_SortsResultsByChurnRiskScoreDescending()
    {
        var gitProvider = Substitute.For<IGitDataProvider>();
        var coberturaParser = Substitute.For<ICoberturaParser>();
        var repoPath = "/fake/repo";

        gitProvider.GetTrackedFilesAsync(repoPath, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "low.cs", "high.cs", "mid.cs" });

        var commitCounts = new Dictionary<string, int>
        {
            ["low.cs"] = 2,
            ["high.cs"] = 100,
            ["mid.cs"] = 20,
        };
        gitProvider.GetCommitCountsAsync(repoPath, Arg.Any<CancellationToken>())
            .Returns(commitCounts);

        var now = DateTime.UtcNow;
        var dates = new Dictionary<string, DateTime>
        {
            ["low.cs"] = now.AddDays(-30),
            ["high.cs"] = now.AddDays(-30),
            ["mid.cs"] = now.AddDays(-30),
        };
        gitProvider.GetFirstCommitDatesAsync(repoPath, Arg.Any<CancellationToken>()).Returns(dates);
        gitProvider.GetLastCommitDatesAsync(repoPath, Arg.Any<CancellationToken>()).Returns(dates);

        var authors = new Dictionary<string, int>
        {
            ["low.cs"] = 1,
            ["high.cs"] = 10,
            ["mid.cs"] = 3,
        };
        gitProvider.GetUniqueAuthorCountsAsync(repoPath, Arg.Any<CancellationToken>()).Returns(authors);

        var empty = new Dictionary<string, int>();
        gitProvider.GetCommitCountsSinceAsync(repoPath, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(empty);
        gitProvider.GetUniqueAuthorCountsSinceAsync(repoPath, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(empty);

        var calculator = new ChurnCalculator(gitProvider, coberturaParser);
        var results = await calculator.AnalyzeAsync(new ChurnAnalysisOptions { RepositoryPath = repoPath });

        Assert.Equal(3, results.Count);
        Assert.Equal("high.cs", results[0].FilePath);
        Assert.Equal("mid.cs", results[1].FilePath);
        Assert.Equal("low.cs", results[2].FilePath);

        Assert.True(results[0].ChurnRiskScore >= results[1].ChurnRiskScore);
        Assert.True(results[1].ChurnRiskScore >= results[2].ChurnRiskScore);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCoverage_AppliesRiskMultiplier()
    {
        var gitProvider = Substitute.For<IGitDataProvider>();
        var coberturaParser = Substitute.For<ICoberturaParser>();
        var repoPath = "/fake/repo";

        gitProvider.GetTrackedFilesAsync(repoPath, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "covered.cs", "uncovered.cs" });

        var commitCounts = new Dictionary<string, int>
        {
            ["covered.cs"] = 50,
            ["uncovered.cs"] = 50,
        };
        gitProvider.GetCommitCountsAsync(repoPath, Arg.Any<CancellationToken>()).Returns(commitCounts);

        var now = DateTime.UtcNow;
        var dates = new Dictionary<string, DateTime>
        {
            ["covered.cs"] = now.AddDays(-70),
            ["uncovered.cs"] = now.AddDays(-70),
        };
        gitProvider.GetFirstCommitDatesAsync(repoPath, Arg.Any<CancellationToken>()).Returns(dates);
        gitProvider.GetLastCommitDatesAsync(repoPath, Arg.Any<CancellationToken>()).Returns(dates);

        var authors = new Dictionary<string, int>
        {
            ["covered.cs"] = 5,
            ["uncovered.cs"] = 5,
        };
        gitProvider.GetUniqueAuthorCountsAsync(repoPath, Arg.Any<CancellationToken>()).Returns(authors);

        var empty = new Dictionary<string, int>();
        gitProvider.GetCommitCountsSinceAsync(repoPath, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(empty);
        gitProvider.GetUniqueAuthorCountsSinceAsync(repoPath, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(empty);

        coberturaParser.Parse("/fake/coverage.xml").Returns(new Dictionary<string, double>
        {
            ["covered.cs"] = 90.0,
            ["uncovered.cs"] = 10.0,
        });

        var calculator = new ChurnCalculator(gitProvider, coberturaParser);
        var results = await calculator.AnalyzeAsync(new ChurnAnalysisOptions
        {
            RepositoryPath = repoPath,
            CoberturaFilePath = "/fake/coverage.xml",
        });

        Assert.Equal(2, results.Count);
        // uncovered.cs should rank higher (90% risk multiplier vs 10%)
        Assert.Equal("uncovered.cs", results[0].FilePath);
        Assert.Equal("covered.cs", results[1].FilePath);
        Assert.True(results[0].ChurnRiskScore > results[1].ChurnRiskScore);
    }

    [Fact]
    public async Task AnalyzeAsync_WithoutCoverage_CoveragePercentIsNull()
    {
        var gitProvider = Substitute.For<IGitDataProvider>();
        var coberturaParser = Substitute.For<ICoberturaParser>();
        var repoPath = "/fake/repo";

        gitProvider.GetTrackedFilesAsync(repoPath, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "file.cs" });

        gitProvider.GetCommitCountsAsync(repoPath, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int> { ["file.cs"] = 10 });

        var now = DateTime.UtcNow;
        var dates = new Dictionary<string, DateTime> { ["file.cs"] = now.AddDays(-14) };
        gitProvider.GetFirstCommitDatesAsync(repoPath, Arg.Any<CancellationToken>()).Returns(dates);
        gitProvider.GetLastCommitDatesAsync(repoPath, Arg.Any<CancellationToken>()).Returns(dates);
        gitProvider.GetUniqueAuthorCountsAsync(repoPath, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int> { ["file.cs"] = 2 });

        var empty = new Dictionary<string, int>();
        gitProvider.GetCommitCountsSinceAsync(repoPath, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(empty);
        gitProvider.GetUniqueAuthorCountsSinceAsync(repoPath, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(empty);

        var calculator = new ChurnCalculator(gitProvider, coberturaParser);
        var results = await calculator.AnalyzeAsync(new ChurnAnalysisOptions { RepositoryPath = repoPath });

        Assert.Single(results);
        Assert.Null(results[0].CoveragePercent);
    }
}

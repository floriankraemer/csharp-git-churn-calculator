using GitChurnCalculator.Console.Reporting;
using GitChurnCalculator.Models;
using Xunit;

namespace GitChurnCalculator.Console.Tests;

/// <summary>Exercises internal CI helpers so Stryker cannot survive trivial string / conditional mutants.</summary>
public class ChurnCiReportingInternalsTests
{
    [Fact]
    public void EncodeWorkflowCommandMessage_PercentSign_IsPercentEncoded()
    {
        Assert.Equal("100%25", ChurnCiEncoding.EncodeWorkflowCommandMessage("100%"));
    }

    [Fact]
    public void EncodeWorkflowCommandMessage_CarriageReturn_IsEncoded()
    {
        Assert.Equal("a%0Db", ChurnCiEncoding.EncodeWorkflowCommandMessage("a\rb"));
    }

    [Fact]
    public void EncodeWorkflowCommandMessage_Newline_IsEncoded()
    {
        Assert.Equal("a%0Ab", ChurnCiEncoding.EncodeWorkflowCommandMessage("a\nb"));
    }

    [Fact]
    public void EncodeWorkflowCommandMessage_CombinesEscapesInOrder()
    {
        Assert.Equal("x%25y%0D%0Az", ChurnCiEncoding.EncodeWorkflowCommandMessage("x%y\r\nz"));
    }

    [Fact]
    public void BuildMessage_WithCoverage_IncludesFormattedPercent()
    {
        var row = new FileChurnResult
        {
            FilePath = "f.cs",
            TotalCommits = 1,
            LinesAdded = 0,
            LinesRemoved = 0,
            FirstCommitDate = null,
            LastCommitDate = null,
            AgeDays = 0,
            ChangesPerWeek = 0,
            ChangesPerMonth = 0,
            ChangesPerYear = 0,
            CommitsLast7Days = 0,
            CommitsLast30Days = 0,
            CommitsLast365Days = 0,
            TotalUniqueAuthors = 1,
            UniqueAuthorsLast7Days = 0,
            UniqueAuthorsLast30Days = 0,
            UniqueAuthorsLast365Days = 0,
            CoveragePercent = 90,
            ChurnRiskScore = 1.0,
        };

        var msg = ChurnCiSeverity.BuildMessage(row);
        Assert.Contains("coverage=90.0%", msg, StringComparison.Ordinal);
        Assert.DoesNotContain("coverage=n/a", msg, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMessage_WithoutCoverage_UsesNaPlaceholder()
    {
        var row = new FileChurnResult
        {
            FilePath = "f.cs",
            TotalCommits = 1,
            LinesAdded = 0,
            LinesRemoved = 0,
            FirstCommitDate = null,
            LastCommitDate = null,
            AgeDays = 0,
            ChangesPerWeek = 0,
            ChangesPerMonth = 0,
            ChangesPerYear = 0,
            CommitsLast7Days = 0,
            CommitsLast30Days = 0,
            CommitsLast365Days = 0,
            TotalUniqueAuthors = 1,
            UniqueAuthorsLast7Days = 0,
            UniqueAuthorsLast30Days = 0,
            UniqueAuthorsLast365Days = 0,
            CoveragePercent = null,
            ChurnRiskScore = 1.0,
        };

        var msg = ChurnCiSeverity.BuildMessage(row);
        Assert.Contains("coverage=n/a", msg, StringComparison.Ordinal);
    }
}

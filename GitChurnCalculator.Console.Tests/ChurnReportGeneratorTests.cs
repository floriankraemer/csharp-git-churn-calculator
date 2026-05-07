using System.Text.Json;
using GitChurnCalculator.Console.Reporting;
using GitChurnCalculator.Models;
using Xunit;

namespace GitChurnCalculator.Console.Tests;

public class ChurnReportGeneratorTests
{
    private static FileChurnResult SampleFile(string path, double churnRisk)
    {
        return new FileChurnResult
        {
            FilePath = path,
            TotalCommits = 5,
            FirstCommitDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastCommitDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            AgeDays = 14,
            ChangesPerWeek = 2.5,
            ChangesPerMonth = 10.71,
            ChangesPerYear = 130.35,
            CommitsLast7Days = 1,
            CommitsLast30Days = 4,
            CommitsLast365Days = 5,
            TotalUniqueAuthors = 1,
            UniqueAuthorsLast7Days = 1,
            UniqueAuthorsLast30Days = 1,
            UniqueAuthorsLast365Days = 1,
            CoveragePercent = null,
            ChurnRiskScore = churnRisk,
        };
    }

    [Fact]
    public void Factory_SupportsSarifGithubGitlab()
    {
        Assert.True(ChurnReportGeneratorFactory.TryGet("sarif", out _));
        Assert.True(ChurnReportGeneratorFactory.TryGet("github", out _));
        Assert.True(ChurnReportGeneratorFactory.TryGet("gitlab", out _));
    }

    [Fact]
    public void SarifChurn_ContainsSchemaVersionAndRule()
    {
        var gen = new SarifChurnReportGenerator();
        var json = gen.Generate(new[] { SampleFile("src/A.cs", 0.5) }, "repo");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("$schema", out _));
        Assert.Equal("2.1.0", root.GetProperty("version").GetString());

        var run = root.GetProperty("runs")[0];
        Assert.Equal("GitChurnCalculator", run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());
        Assert.Contains(
            run.GetProperty("tool").GetProperty("driver").GetProperty("rules").EnumerateArray(),
            r => r.GetProperty("id").GetString() == "churn/file-risk"
        );

        var result = run.GetProperty("results")[0];
        Assert.Equal("churn/file-risk", result.GetProperty("ruleId").GetString());
        Assert.Equal("fail", result.GetProperty("kind").GetString());
        Assert.Equal("note", result.GetProperty("level").GetString());
        Assert.Contains("src/A.cs", result.GetProperty("locations")[0].GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uri").GetString());
        Assert.True(result.TryGetProperty("properties", out var props));
        Assert.True(props.TryGetProperty("churnRiskScore", out _));
    }

    [Fact]
    public void SarifChurn_HighScore_UsesErrorLevel()
    {
        var gen = new SarifChurnReportGenerator();
        var json = gen.Generate(new[] { SampleFile("x.cs", 15.0) }, "r");
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0];
        Assert.Equal("error", result.GetProperty("level").GetString());
        Assert.Contains(result.GetProperty("level").GetString(), new[] { "note", "warning", "error", "none" });
    }

    [Fact]
    public void GithubActionsChurn_ContainsWorkflowCommand()
    {
        var gen = new GithubActionsChurnReportGenerator();
        var text = gen.Generate(new[] { SampleFile("src/B.cs", 5.0) }, "r");

        Assert.StartsWith("::warning file=src/B.cs,line=1,", text.Trim());
        Assert.Contains("::", text);
    }

    [Fact]
    public void GitlabCodeQualityChurn_IsJsonArrayWithFingerprint()
    {
        var gen = new GitlabCodeQualityChurnReportGenerator();
        var json = gen.Generate(new[] { SampleFile("p/q.cs", 2.0) }, "r");

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        var first = doc.RootElement[0];
        Assert.Equal("issue", first.GetProperty("type").GetString());
        Assert.True(first.GetProperty("fingerprint").GetString()!.Length >= 32);
        Assert.Equal(1, first.GetProperty("location").GetProperty("lines").GetProperty("begin").GetInt32());
    }

    [Fact]
    public void TimeSeriesFactory_DoesNotIncludeCiFormats()
    {
        Assert.False(TimeSeriesReportGeneratorFactory.TryGet("sarif", out _));
        Assert.False(TimeSeriesReportGeneratorFactory.TryGet("github", out _));
        Assert.False(TimeSeriesReportGeneratorFactory.TryGet("gitlab", out _));
    }

    [Fact]
    public void TimeSeriesFactory_SupportsGraphFormat()
    {
        Assert.True(TimeSeriesReportGeneratorFactory.TryGet("graph", out var generator));
        Assert.IsType<HtmlTimeSeriesGraphReportGenerator>(generator);
    }
}

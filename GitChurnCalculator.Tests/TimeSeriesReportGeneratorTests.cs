using System.Text.Json;
using GitChurnCalculator.Console.Reporting;
using GitChurnCalculator.Models;
using Xunit;

namespace GitChurnCalculator.Tests;

public class TimeSeriesReportGeneratorTests
{
    private static IReadOnlyList<TimeSeriesPoint> BuildTwoPoints()
    {
        var file1 = new FileChurnResult
        {
            FilePath = "src/Foo.cs",
            TotalCommits = 10,
            FirstCommitDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastCommitDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            AgeDays = 14,
            ChangesPerWeek = 5.0,
            ChangesPerMonth = 21.43,
            ChangesPerYear = 260.71,
            CommitsLast7Days = 2,
            CommitsLast30Days = 8,
            CommitsLast365Days = 10,
            TotalUniqueAuthors = 2,
            UniqueAuthorsLast7Days = 1,
            UniqueAuthorsLast30Days = 2,
            UniqueAuthorsLast365Days = 2,
            CoveragePercent = 80.0,
            ChurnRiskScore = 2.0,
        };

        var file2 = new FileChurnResult
        {
            FilePath = "src/Bar.cs",
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
            ChurnRiskScore = 2.5,
        };

        return new List<TimeSeriesPoint>
        {
            new() { AsOf = new DateTime(2024, 1, 7, 0, 0, 0, DateTimeKind.Utc), Files = new[] { file1 } },
            new() { AsOf = new DateTime(2024, 1, 14, 0, 0, 0, DateTimeKind.Utc), Files = new[] { file2 } },
        };
    }

    // ── CSV ──────────────────────────────────────────────────────────────────

    [Fact]
    public void CsvTimeSeries_TwoPoints_IncludesAsOfColumn()
    {
        var generator = new CsvTimeSeriesReportGenerator();
        var output = generator.Generate(BuildTwoPoints(), "repo");

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Header contains AsOf as first column
        Assert.StartsWith("AsOf,File,", lines[0]);

        // One data row per (asOf, file) pair — 2 time points × 1 file each = 2 rows + 1 header
        Assert.Equal(3, lines.Length);

        // First data row starts with the first asOf date
        Assert.StartsWith("2024-01-07,", lines[1]);

        // Second data row uses the second asOf date
        Assert.StartsWith("2024-01-14,", lines[2]);
    }

    [Fact]
    public void CsvTimeSeries_EmptyPoints_OnlyHeader()
    {
        var generator = new CsvTimeSeriesReportGenerator();
        var output = generator.Generate(Array.Empty<TimeSeriesPoint>(), "repo");

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.StartsWith("AsOf,", lines[0]);
    }

    // ── JSON ─────────────────────────────────────────────────────────────────

    [Fact]
    public void JsonTimeSeries_TwoPoints_SerializesAsOfAndFiles()
    {
        var generator = new JsonTimeSeriesReportGenerator();
        var output = generator.Generate(BuildTwoPoints(), "repo");

        using var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());

        var firstPoint = root[0];
        Assert.True(firstPoint.TryGetProperty("asOf", out var asOfProp));
        Assert.Contains("2024-01-07", asOfProp.GetString());

        Assert.True(firstPoint.TryGetProperty("files", out var filesProp));
        Assert.Equal(JsonValueKind.Array, filesProp.ValueKind);
        Assert.Equal(1, filesProp.GetArrayLength());

        var firstFile = filesProp[0];
        Assert.True(firstFile.TryGetProperty("filePath", out var pathProp));
        Assert.Equal("src/Foo.cs", pathProp.GetString());
    }

    [Fact]
    public void JsonTimeSeries_EmptyPoints_ReturnsEmptyArray()
    {
        var generator = new JsonTimeSeriesReportGenerator();
        var output = generator.Generate(Array.Empty<TimeSeriesPoint>(), "repo");

        using var doc = JsonDocument.Parse(output);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    // ── HTML ─────────────────────────────────────────────────────────────────

    [Fact]
    public void HtmlTimeSeries_TwoPoints_ContainsBothSections()
    {
        var generator = new HtmlTimeSeriesReportGenerator();
        var output = generator.Generate(BuildTwoPoints(), "/my/repo");

        // Both asOf dates should appear as section labels
        Assert.Contains("2024-01-07", output);
        Assert.Contains("2024-01-14", output);

        // Both file paths should be present
        Assert.Contains("src/Foo.cs", output);
        Assert.Contains("src/Bar.cs", output);

        // Two <details> sections
        var detailsCount = CountOccurrences(output, "<details");
        Assert.Equal(2, detailsCount);
    }

    [Fact]
    public void HtmlTimeSeries_SubtitleIsHtmlEncoded()
    {
        var generator = new HtmlTimeSeriesReportGenerator();
        var output = generator.Generate(BuildTwoPoints(), "<repo & path>");

        Assert.Contains("&lt;repo &amp; path&gt;", output);
        Assert.DoesNotContain("<repo & path>", output);
    }

    [Fact]
    public void HtmlTimeSeries_EmptyPoints_RendersValidPage()
    {
        var generator = new HtmlTimeSeriesReportGenerator();
        var output = generator.Generate(Array.Empty<TimeSeriesPoint>(), "repo");

        Assert.StartsWith("<!DOCTYPE html>", output.TrimStart());
        Assert.Contains("0 time points", output);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}

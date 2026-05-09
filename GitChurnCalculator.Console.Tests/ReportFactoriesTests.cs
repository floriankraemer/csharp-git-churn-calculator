using GitChurnCalculator.Console.Reporting;
using Xunit;

namespace GitChurnCalculator.Console.Tests;

public class ReportFactoriesTests
{
    [Theory]
    [InlineData("csv")]
    [InlineData("CSV")]
    [InlineData(" Html ")]
    [InlineData("JSON")]
    public void ChurnReportGeneratorFactory_TryGet_SupportedFormats(string format)
    {
        Assert.True(ChurnReportGeneratorFactory.TryGet(format, out var gen));
        Assert.NotNull(gen);
    }

    [Fact]
    public void ChurnReportGeneratorFactory_TryGet_Unknown_ReturnsFalse()
    {
        Assert.False(ChurnReportGeneratorFactory.TryGet("xml", out var gen));
        Assert.Null(gen);
    }

    [Fact]
    public void ChurnReportGeneratorFactory_SupportedFormatsList_ListsCsvFirst()
    {
        Assert.Contains("csv", ChurnReportGeneratorFactory.SupportedFormatsList, StringComparison.Ordinal);
        Assert.Contains("html", ChurnReportGeneratorFactory.SupportedFormatsList, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("JSON")]
    [InlineData(" Html ")]
    [InlineData("GRAPH")]
    public void TimeSeriesReportGeneratorFactory_TryGet_SupportedFormats(string format)
    {
        Assert.True(TimeSeriesReportGeneratorFactory.TryGet(format, out var gen));
        Assert.NotNull(gen);
    }

    [Fact]
    public void TimeSeriesReportGeneratorFactory_TryGet_Unknown_ReturnsFalse()
    {
        Assert.False(TimeSeriesReportGeneratorFactory.TryGet("sarif", out var gen));
        Assert.Null(gen);
    }

    [Fact]
    public void TimeSeriesReportGeneratorFactory_SupportedFormatsList_IsNonEmptyCsvPrefixed()
    {
        var list = TimeSeriesReportGeneratorFactory.SupportedFormatsList;
        Assert.False(string.IsNullOrWhiteSpace(list));
        Assert.StartsWith("csv", list, StringComparison.OrdinalIgnoreCase);
    }
}

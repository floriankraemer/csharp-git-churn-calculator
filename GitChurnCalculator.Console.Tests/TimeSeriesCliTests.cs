using GitChurnCalculator.Console.Cli;
using Xunit;

namespace GitChurnCalculator.Console.Tests;

public class TimeSeriesCliTests
{
    [Fact]
    public void BucketEndCalculator_FromEqualsTo_ReturnsThatDate()
    {
        var d = new DateTime(2024, 6, 10);
        var ends = TimeSeriesBucketEndCalculator.BuildEnds(d, d, "week");
        Assert.Single(ends);
        Assert.Equal(d, ends[0]);
    }

    [Fact]
    public void BucketEndCalculator_Month_AlwaysIncludesToAsLast()
    {
        var from = new DateTime(2024, 1, 5);
        var to = new DateTime(2024, 3, 18);
        var ends = TimeSeriesBucketEndCalculator.BuildEnds(from, to, "month");
        Assert.Equal(to, ends[^1]);
        Assert.Contains(new DateTime(2024, 2, 5), ends);
    }

    [Fact]
    public void TimeSeriesArguments_MissingFrom_ReturnsFalse()
    {
        var ok = TimeSeriesArguments.TryValidate("week", null, null, out var err, out var v);
        Assert.False(ok);
        Assert.Contains("--from", err!);
        Assert.Null(v);
    }

    [Fact]
    public void TimeSeriesArguments_InvalidFrom_ReturnsFalse()
    {
        var ok = TimeSeriesArguments.TryValidate("week", "not-a-date", null, out var err, out _);
        Assert.False(ok);
        Assert.Contains("yyyy-MM-dd", err!);
    }

    [Fact]
    public void TimeSeriesArguments_FromAfterTo_ReturnsFalse()
    {
        var ok = TimeSeriesArguments.TryValidate("week", "2024-02-01", "2024-01-01", out var err, out _);
        Assert.False(ok);
        Assert.Contains("before", err!);
    }

    [Fact]
    public void TimeSeriesArguments_InvalidSeries_ReturnsFalse()
    {
        var ok = TimeSeriesArguments.TryValidate("day", "2024-01-01", "2024-01-31", out var err, out _);
        Assert.False(ok);
        Assert.Contains("week", err!);
    }

    [Fact]
    public void TimeSeriesArguments_ValidWeek_ReturnsParsed()
    {
        var ok = TimeSeriesArguments.TryValidate("Week", "2024-01-01", "2024-01-31", out _, out var v);
        Assert.True(ok);
        Assert.NotNull(v);
        Assert.Equal("week", v!.GranularityLower);
        Assert.Equal(new DateTime(2024, 1, 1), v.From);
        Assert.Equal(new DateTime(2024, 1, 31), v.To);
    }

    [Fact]
    public void BucketEndCalculator_Week_IncludesEndWhenNotAligned()
    {
        var from = new DateTime(2024, 1, 1);
        var to = new DateTime(2024, 1, 10);
        var ends = TimeSeriesBucketEndCalculator.BuildEnds(from, to, "week");
        Assert.NotEmpty(ends);
        Assert.Equal(to, ends[^1]);
    }

    [Fact]
    public void BucketEndCalculator_Month_AddsMonthlyPoints()
    {
        var from = new DateTime(2024, 1, 1);
        var to = new DateTime(2024, 3, 15);
        var ends = TimeSeriesBucketEndCalculator.BuildEnds(from, to, "month");
        Assert.Contains(new DateTime(2024, 2, 1), ends);
        Assert.Equal(to, ends[^1]);
    }
}

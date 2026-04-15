using System.Text.Json;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

public sealed class JsonTimeSeriesReportGenerator : ITimeSeriesReportGenerator
{
    public string Generate(IReadOnlyList<TimeSeriesPoint> points, string subtitle)
    {
        _ = subtitle;
        return JsonSerializer.Serialize(points, ChurnReportsJsonContext.Default.IReadOnlyListTimeSeriesPoint);
    }
}

using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

public interface ITimeSeriesReportGenerator
{
    string Generate(IReadOnlyList<TimeSeriesPoint> points, string subtitle);
}

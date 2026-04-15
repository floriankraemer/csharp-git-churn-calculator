using System.Text.Json;
using System.Text.Json.Serialization;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

public sealed class JsonTimeSeriesReportGenerator : ITimeSeriesReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public string Generate(IReadOnlyList<TimeSeriesPoint> points, string subtitle)
    {
        _ = subtitle;
        return JsonSerializer.Serialize(points, JsonOptions);
    }
}

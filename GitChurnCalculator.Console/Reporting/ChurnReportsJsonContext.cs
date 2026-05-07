using System.Text.Json.Serialization;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(FileChurnResult))]
[JsonSerializable(typeof(IReadOnlyList<FileChurnResult>))]
[JsonSerializable(typeof(TimeSeriesPoint))]
[JsonSerializable(typeof(IReadOnlyList<TimeSeriesPoint>))]
[JsonSerializable(typeof(TimeSeriesGraphPoint))]
[JsonSerializable(typeof(TimeSeriesGraphSeries))]
[JsonSerializable(typeof(IReadOnlyList<TimeSeriesGraphSeries>))]
internal partial class ChurnReportsJsonContext : JsonSerializerContext;

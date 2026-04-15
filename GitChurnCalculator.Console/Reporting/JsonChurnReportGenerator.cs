using System.Text.Json;
using System.Text.Json.Serialization;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

public sealed class JsonChurnReportGenerator : IChurnReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public string Generate(IReadOnlyList<FileChurnResult> results, string subtitle)
    {
        _ = subtitle;
        return JsonSerializer.Serialize(results, JsonOptions);
    }
}

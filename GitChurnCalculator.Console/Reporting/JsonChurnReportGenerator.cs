using System.Text.Json;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

public sealed class JsonChurnReportGenerator : IChurnReportGenerator
{
    public string Generate(IReadOnlyList<FileChurnResult> results, string subtitle)
    {
        _ = subtitle;
        return JsonSerializer.Serialize(results, ChurnReportsJsonContext.Default.IReadOnlyListFileChurnResult);
    }
}

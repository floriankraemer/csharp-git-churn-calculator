namespace GitChurnCalculator.Console.Reporting;

public static class TimeSeriesReportGeneratorFactory
{
    private static readonly IReadOnlyDictionary<string, ITimeSeriesReportGenerator> Generators =
        new Dictionary<string, ITimeSeriesReportGenerator>(StringComparer.OrdinalIgnoreCase)
        {
            ["csv"] = new CsvTimeSeriesReportGenerator(),
            ["json"] = new JsonTimeSeriesReportGenerator(),
            ["html"] = new HtmlTimeSeriesReportGenerator(),
        };

    public static string SupportedFormatsList =>
        string.Join(", ", Generators.Keys);

    public static bool TryGet(string format, out ITimeSeriesReportGenerator? generator) =>
        Generators.TryGetValue(format, out generator);
}

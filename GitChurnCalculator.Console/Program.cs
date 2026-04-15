using System.CommandLine;
using System.Globalization;
using GitChurnCalculator.Console.Reporting;
using GitChurnCalculator.Models;
using GitChurnCalculator.Services;

var repoArgument = new Argument<DirectoryInfo>(
    "repo-path",
    "Path to the git repository to analyze");

var formatOption = new Option<string>(
    "--format",
    getDefaultValue: () => "csv",
    description: $"Output format: {ChurnReportGeneratorFactory.SupportedFormatsList}");

var coverageOption = new Option<FileInfo?>(
    "--coverage",
    description: "Path to a Cobertura XML coverage file (optional)");

var outputOption = new Option<FileInfo?>(
    "--output",
    description: "Output file path (defaults to stdout)");

var seriesOption = new Option<string?>(
    "--series",
    description: "Produce a time series by stepping in 'week' or 'month' chunks. Requires --from.");

var fromOption = new Option<string?>(
    "--from",
    description: "Start date for time series (yyyy-MM-dd). Required when --series is used.");

var toOption = new Option<string?>(
    "--to",
    description: "End date for time series (yyyy-MM-dd). Defaults to today when --series is used.");

var rootCommand = new RootCommand("Git Churn Risk Calculator - analyzes file churn, author spread, and optional test coverage")
{
    repoArgument,
    formatOption,
    coverageOption,
    outputOption,
    seriesOption,
    fromOption,
    toOption,
};

rootCommand.SetHandler(async (
    DirectoryInfo repo,
    string format,
    FileInfo? coverage,
    FileInfo? output,
    string? series,
    string? from,
    string? to) =>
{
    if (!repo.Exists)
    {
        Console.Error.WriteLine($"Error: Repository path '{repo.FullName}' does not exist.");
        Environment.ExitCode = 1;
        return;
    }

    if (coverage is not null && !coverage.Exists)
    {
        Console.Error.WriteLine($"Error: Coverage file '{coverage.FullName}' does not exist.");
        Environment.ExitCode = 1;
        return;
    }

    var calculator = new ChurnCalculator(
        new GitProcessDataProvider(),
        new CoberturaXmlParser());

    Console.Error.WriteLine($"Analyzing repository: {repo.FullName}");
    if (coverage is not null)
        Console.Error.WriteLine($"Using coverage file: {coverage.FullName}");

    // ── Time series mode ────────────────────────────────────────────────────
    if (series is not null)
    {
        if (!TimeSeriesReportGeneratorFactory.TryGet(format, out var tsGenerator) || tsGenerator is null)
        {
            Console.Error.WriteLine($"Error: Unsupported format '{format}'. Use {TimeSeriesReportGeneratorFactory.SupportedFormatsList}.");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrEmpty(from))
        {
            Console.Error.WriteLine("Error: --from <date> is required when --series is used.");
            Environment.ExitCode = 1;
            return;
        }

        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
        {
            Console.Error.WriteLine($"Error: --from value '{from}' is not a valid date. Use yyyy-MM-dd.");
            Environment.ExitCode = 1;
            return;
        }

        DateTime toDate;
        if (string.IsNullOrEmpty(to))
        {
            toDate = DateTime.UtcNow.Date;
        }
        else if (!DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out toDate))
        {
            Console.Error.WriteLine($"Error: --to value '{to}' is not a valid date. Use yyyy-MM-dd.");
            Environment.ExitCode = 1;
            return;
        }

        if (fromDate > toDate)
        {
            Console.Error.WriteLine("Error: --from date must be on or before --to date.");
            Environment.ExitCode = 1;
            return;
        }

        var seriesLower = series.ToLowerInvariant();
        if (seriesLower is not ("week" or "month"))
        {
            Console.Error.WriteLine($"Error: --series must be 'week' or 'month', got '{series}'.");
            Environment.ExitCode = 1;
            return;
        }

        var bucketEnds = BuildBucketEnds(fromDate, toDate, seriesLower);
        Console.Error.WriteLine($"Time series mode: {seriesLower} chunks from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} ({bucketEnds.Count} points).");

        var points = new List<TimeSeriesPoint>(bucketEnds.Count);
        foreach (var asOf in bucketEnds)
        {
            Console.Error.WriteLine($"  Analyzing as of {asOf:yyyy-MM-dd}...");
            var options = new ChurnAnalysisOptions
            {
                RepositoryPath = repo.FullName,
                CoberturaFilePath = coverage?.FullName,
                AsOf = asOf,
            };
            var results = await calculator.AnalyzeAsync(options);
            points.Add(new TimeSeriesPoint { AsOf = asOf, Files = results });
        }

        Console.Error.WriteLine($"Found data across {points.Count} time points.");

        var outputText = tsGenerator.Generate(points, repo.FullName);
        await WriteOutputAsync(output, outputText);
        return;
    }

    // ── Single-snapshot mode (existing behaviour) ───────────────────────────
    if (!ChurnReportGeneratorFactory.TryGet(format, out var generator) || generator is null)
    {
        Console.Error.WriteLine($"Error: Unsupported format '{format}'. Use {ChurnReportGeneratorFactory.SupportedFormatsList}.");
        Environment.ExitCode = 1;
        return;
    }

    var snapshotOptions = new ChurnAnalysisOptions
    {
        RepositoryPath = repo.FullName,
        CoberturaFilePath = coverage?.FullName,
    };

    var snapshotResults = await calculator.AnalyzeAsync(snapshotOptions);

    Console.Error.WriteLine($"Found {snapshotResults.Count} files with commit history.");

    var snapshotOutput = generator.Generate(snapshotResults, repo.FullName);
    await WriteOutputAsync(output, snapshotOutput);

}, repoArgument, formatOption, coverageOption, outputOption, seriesOption, fromOption, toOption);

return await rootCommand.InvokeAsync(args);

// ── Helpers ─────────────────────────────────────────────────────────────────

static List<DateTime> BuildBucketEnds(DateTime from, DateTime to, string granularity)
{
    var ends = new List<DateTime>();
    var cursor = granularity == "week"
        ? from.AddDays(7)
        : AddOneMonth(from);

    while (cursor <= to)
    {
        ends.Add(cursor);
        cursor = granularity == "week"
            ? cursor.AddDays(7)
            : AddOneMonth(cursor);
    }

    // Always include the end date as the final point if it wasn't already added
    if (ends.Count == 0 || ends[^1] != to)
        ends.Add(to);

    return ends;
}

static DateTime AddOneMonth(DateTime date)
{
    return date.AddMonths(1);
}

static async Task WriteOutputAsync(FileInfo? output, string text)
{
    if (output is not null)
    {
        await File.WriteAllTextAsync(output.FullName, text);
        Console.Error.WriteLine($"Output written to: {output.FullName}");
    }
    else
    {
        Console.Write(text);
    }
}

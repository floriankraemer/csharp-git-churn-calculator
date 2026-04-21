using GitChurnCalculator.Console.Reporting;
using GitChurnCalculator.Models;
using GitChurnCalculator.Services;

namespace GitChurnCalculator.Console.Cli;

public sealed class ChurnAnalysisApp
{
    private readonly IChurnCalculator _calculator;

    public ChurnAnalysisApp()
        : this(new ChurnCalculator(new GitProcessDataProvider(), new AutoDetectCoverageParser()))
    {
    }

    public ChurnAnalysisApp(IChurnCalculator calculator) => _calculator = calculator;

    public async Task HandleAsync(
        DirectoryInfo repo,
        string format,
        FileInfo? coverage,
        FileInfo? output,
        string? series,
        string? from,
        string? to)
    {
        if (!repo.Exists)
        {
            Fail($"Error: Repository path '{repo.FullName}' does not exist.");
            return;
        }

        if (coverage is not null && !coverage.Exists)
        {
            Fail($"Error: Coverage file '{coverage.FullName}' does not exist.");
            return;
        }

        LogAnalysisStart(repo, coverage);

        if (series is null)
        {
            await RunSnapshotAsync(repo, format, coverage, output);
            return;
        }

        await RunTimeSeriesAsync(repo, format, coverage, output, series, from, to);
    }

    private async Task RunSnapshotAsync(
        DirectoryInfo repo,
        string format,
        FileInfo? coverage,
        FileInfo? output)
    {
        if (!ChurnReportGeneratorFactory.TryGet(format, out var generator) || generator is null)
        {
            Fail($"Error: Unsupported format '{format}'. Use {ChurnReportGeneratorFactory.SupportedFormatsList}.");
            return;
        }

        var options = new ChurnAnalysisOptions
        {
            RepositoryPath = repo.FullName,
            CoverageFilePath = coverage?.FullName,
        };

        var results = await _calculator.AnalyzeAsync(options);
        global::System.Console.Error.WriteLine($"Found {results.Count} files with commit history.");

        var text = generator.Generate(results, repo.FullName);
        await ChurnOutputWriter.WriteAsync(output, text);
    }

    private async Task RunTimeSeriesAsync(
        DirectoryInfo repo,
        string format,
        FileInfo? coverage,
        FileInfo? output,
        string series,
        string? from,
        string? to)
    {
        if (!TimeSeriesReportGeneratorFactory.TryGet(format, out var tsGenerator) || tsGenerator is null)
        {
            Fail($"Error: Unsupported format '{format}'. Use {TimeSeriesReportGeneratorFactory.SupportedFormatsList}.");
            return;
        }

        if (!TimeSeriesArguments.TryValidate(series, from, to, out var validationError, out var parsed))
        {
            Fail(validationError!);
            return;
        }

        var bucketEnds = TimeSeriesBucketEndCalculator.BuildEnds(parsed!.From, parsed.To, parsed.GranularityLower);
        global::System.Console.Error.WriteLine(
            $"Time series mode: {parsed.GranularityLower} chunks from {parsed.From:yyyy-MM-dd} to {parsed.To:yyyy-MM-dd} ({bucketEnds.Count} points).");

        var points = await CollectTimeSeriesPointsAsync(repo, coverage, bucketEnds);
        global::System.Console.Error.WriteLine($"Found data across {points.Count} time points.");

        var outputText = tsGenerator.Generate(points, repo.FullName);
        await ChurnOutputWriter.WriteAsync(output, outputText);
    }

    private async Task<List<TimeSeriesPoint>> CollectTimeSeriesPointsAsync(
        DirectoryInfo repo,
        FileInfo? coverage,
        IReadOnlyList<DateTime> bucketEnds)
    {
        var points = new List<TimeSeriesPoint>(bucketEnds.Count);
        foreach (var asOf in bucketEnds)
        {
            global::System.Console.Error.WriteLine($"  Analyzing as of {asOf:yyyy-MM-dd}...");
            var options = new ChurnAnalysisOptions
            {
                RepositoryPath = repo.FullName,
                CoverageFilePath = coverage?.FullName,
                AsOf = asOf,
            };
            var results = await _calculator.AnalyzeAsync(options);
            points.Add(new TimeSeriesPoint { AsOf = asOf, Files = results });
        }

        return points;
    }

    private static void LogAnalysisStart(DirectoryInfo repo, FileInfo? coverage)
    {
        global::System.Console.Error.WriteLine($"Analyzing repository: {repo.FullName}");
        if (coverage is not null)
            global::System.Console.Error.WriteLine($"Using coverage file: {coverage.FullName}");
    }

    private static void Fail(string message)
    {
        global::System.Console.Error.WriteLine(message);
        Environment.ExitCode = 1;
    }
}

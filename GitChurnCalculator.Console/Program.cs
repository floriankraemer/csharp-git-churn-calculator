using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitChurnCalculator.Models;
using GitChurnCalculator.Services;

var repoArgument = new Argument<DirectoryInfo>(
    "repo-path",
    "Path to the git repository to analyze");

var formatOption = new Option<string>(
    "--format",
    getDefaultValue: () => "csv",
    description: "Output format: csv or json");

var coverageOption = new Option<FileInfo?>(
    "--coverage",
    description: "Path to a Cobertura XML coverage file (optional)");

var outputOption = new Option<FileInfo?>(
    "--output",
    description: "Output file path (defaults to stdout)");

var rootCommand = new RootCommand("Git Churn Risk Calculator - analyzes file churn, author spread, and optional test coverage")
{
    repoArgument,
    formatOption,
    coverageOption,
    outputOption,
};

rootCommand.SetHandler(async (DirectoryInfo repo, string format, FileInfo? coverage, FileInfo? output) =>
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

    if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Error: Unsupported format '{format}'. Use 'csv' or 'json'.");
        Environment.ExitCode = 1;
        return;
    }

    var options = new ChurnAnalysisOptions
    {
        RepositoryPath = repo.FullName,
        CoberturaFilePath = coverage?.FullName,
    };

    var calculator = new ChurnCalculator(
        new GitProcessDataProvider(),
        new CoberturaXmlParser());

    Console.Error.WriteLine($"Analyzing repository: {repo.FullName}");
    if (coverage is not null)
        Console.Error.WriteLine($"Using coverage file: {coverage.FullName}");

    var results = await calculator.AnalyzeAsync(options);

    Console.Error.WriteLine($"Found {results.Count} files with commit history.");

    var outputText = string.Equals(format, "json", StringComparison.OrdinalIgnoreCase)
        ? FormatJson(results)
        : FormatCsv(results);

    if (output is not null)
    {
        await File.WriteAllTextAsync(output.FullName, outputText);
        Console.Error.WriteLine($"Output written to: {output.FullName}");
    }
    else
    {
        Console.Write(outputText);
    }

}, repoArgument, formatOption, coverageOption, outputOption);

return await rootCommand.InvokeAsync(args);

static string FormatCsv(IReadOnlyList<FileChurnResult> results)
{
    var sb = new StringBuilder();
    sb.AppendLine("File,TotalCommits,FirstCommitDate,LastCommitDate,AgeDays,ChangesPerWeek,ChangesPerMonth,ChangesPerYear,CommitsLast7Days,CommitsLast30Days,CommitsLast365Days,TotalUniqueAuthors,UniqueAuthorsLast7Days,UniqueAuthorsLast30Days,UniqueAuthorsLast365Days,CoveragePercent,ChurnRiskScore");

    foreach (var r in results)
    {
        sb.Append('"').Append(r.FilePath.Replace("\"", "\"\"")).Append('"');
        sb.Append(',').Append(r.TotalCommits);
        sb.Append(',').Append(r.FirstCommitDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
        sb.Append(',').Append(r.LastCommitDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
        sb.Append(',').Append(r.AgeDays);
        sb.Append(',').Append(r.ChangesPerWeek.ToString("F2", CultureInfo.InvariantCulture));
        sb.Append(',').Append(r.ChangesPerMonth.ToString("F2", CultureInfo.InvariantCulture));
        sb.Append(',').Append(r.ChangesPerYear.ToString("F2", CultureInfo.InvariantCulture));
        sb.Append(',').Append(r.CommitsLast7Days);
        sb.Append(',').Append(r.CommitsLast30Days);
        sb.Append(',').Append(r.CommitsLast365Days);
        sb.Append(',').Append(r.TotalUniqueAuthors);
        sb.Append(',').Append(r.UniqueAuthorsLast7Days);
        sb.Append(',').Append(r.UniqueAuthorsLast30Days);
        sb.Append(',').Append(r.UniqueAuthorsLast365Days);
        sb.Append(',').Append(r.CoveragePercent?.ToString("F2", CultureInfo.InvariantCulture) ?? "");
        sb.Append(',').Append(r.ChurnRiskScore.ToString("F4", CultureInfo.InvariantCulture));
        sb.AppendLine();
    }

    return sb.ToString();
}

static string FormatJson(IReadOnlyList<FileChurnResult> results)
{
    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };
    return JsonSerializer.Serialize(results, jsonOptions);
}

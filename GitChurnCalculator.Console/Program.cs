using System.CommandLine;
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

    if (!ChurnReportGeneratorFactory.TryGet(format, out var generator) || generator is null)
    {
        Console.Error.WriteLine($"Error: Unsupported format '{format}'. Use {ChurnReportGeneratorFactory.SupportedFormatsList}.");
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

    var outputText = generator.Generate(results, repo.FullName);

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

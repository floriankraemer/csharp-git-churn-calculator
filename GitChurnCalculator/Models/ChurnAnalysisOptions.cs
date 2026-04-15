namespace GitChurnCalculator.Models;

public sealed class ChurnAnalysisOptions
{
    public required string RepositoryPath { get; init; }
    public string? CoberturaFilePath { get; init; }
}

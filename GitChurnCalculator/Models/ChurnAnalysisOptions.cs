namespace GitChurnCalculator.Models;

public sealed class ChurnAnalysisOptions
{
    public required string RepositoryPath { get; init; }
    public string? CoberturaFilePath { get; init; }

    /// <summary>
    /// When set, the analysis treats this date as "now": all git queries are bounded
    /// with --until and rolling windows are calculated relative to this date.
    /// When null, DateTime.UtcNow is used (existing behaviour).
    /// </summary>
    public DateTime? AsOf { get; init; }
}

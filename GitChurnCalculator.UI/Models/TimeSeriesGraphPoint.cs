namespace GitChurnCalculator.UI.Models;

public sealed class TimeSeriesGraphPoint
{
    public required DateTime Date { get; init; }
    public required double ChurnRiskScore { get; init; }
    public required double ChangesPerWeek { get; init; }
    public required int TotalCommits { get; init; }
    public required int LinesAdded { get; init; }
    public required int LinesRemoved { get; init; }
    public required double? CoveragePercent { get; init; }
}

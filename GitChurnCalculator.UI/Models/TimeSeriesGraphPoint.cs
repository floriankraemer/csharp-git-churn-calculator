namespace GitChurnCalculator.UI.Models;

public sealed class TimeSeriesGraphPoint
{
    public required DateTime Date { get; init; }
    public required double ChurnRiskScore { get; init; }
}

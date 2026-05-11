namespace GitChurnCalculator.UI.Models;

public sealed class TimeSeriesGraphSeries
{
    public required string FilePath { get; init; }
    public required IReadOnlyList<TimeSeriesGraphPoint> Points { get; init; }
    public double MaxChurnRiskScore => Points.Count == 0 ? 0 : Points.Max(point => point.ChurnRiskScore);
}

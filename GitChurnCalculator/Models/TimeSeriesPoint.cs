namespace GitChurnCalculator.Models;

public sealed class TimeSeriesPoint
{
    public required DateTime AsOf { get; init; }
    public required IReadOnlyList<FileChurnResult> Files { get; init; }
}

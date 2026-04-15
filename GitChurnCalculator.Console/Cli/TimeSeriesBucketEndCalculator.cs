namespace GitChurnCalculator.Console.Cli;

/// <summary>
/// Builds inclusive series end dates (bucket boundaries) between two calendar dates.
/// </summary>
public static class TimeSeriesBucketEndCalculator
{
    public static IReadOnlyList<DateTime> BuildEnds(DateTime from, DateTime to, string granularity)
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

        if (ends.Count == 0 || ends[^1] != to)
            ends.Add(to);

        return ends;
    }

    private static DateTime AddOneMonth(DateTime date) => date.AddMonths(1);
}

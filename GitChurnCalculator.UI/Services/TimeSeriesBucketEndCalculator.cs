namespace GitChurnCalculator.UI.Services;

public static class TimeSeriesBucketEndCalculator
{
    public static IReadOnlyList<DateTime> BuildMonthEnds(DateTime from, DateTime to)
    {
        var ends = new List<DateTime> { from };
        var cursor = from.AddMonths(1);

        while (cursor <= to)
        {
            ends.Add(cursor);
            cursor = cursor.AddMonths(1);
        }

        if (ends[^1] != to)
            ends.Add(to);

        return ends;
    }
}

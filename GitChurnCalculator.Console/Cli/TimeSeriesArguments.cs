using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GitChurnCalculator.Console.Cli;

public static class TimeSeriesArguments
{
    public sealed record Validated(DateTime From, DateTime To, string GranularityLower);

    public static bool TryValidate(
        string series,
        string? from,
        string? to,
        [NotNullWhen(false)] out string? errorMessage,
        [NotNullWhen(true)] out Validated? validated)
    {
        validated = null;
        errorMessage = null;

        if (string.IsNullOrEmpty(from))
        {
            errorMessage = "Error: --from <date> is required when --series is used.";
            return false;
        }

        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
        {
            errorMessage = $"Error: --from value '{from}' is not a valid date. Use yyyy-MM-dd.";
            return false;
        }

        var toDate = DateTime.UtcNow.Date;
        if (!string.IsNullOrEmpty(to) &&
            !DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out toDate))
        {
            errorMessage = $"Error: --to value '{to}' is not a valid date. Use yyyy-MM-dd.";
            return false;
        }

        if (fromDate > toDate)
        {
            errorMessage = "Error: --from date must be on or before --to date.";
            return false;
        }

        var seriesLower = series.ToLowerInvariant();
        if (seriesLower is not ("week" or "month"))
        {
            errorMessage = $"Error: --series must be 'week' or 'month', got '{series}'.";
            return false;
        }

        validated = new Validated(fromDate, toDate, seriesLower);
        return true;
    }
}

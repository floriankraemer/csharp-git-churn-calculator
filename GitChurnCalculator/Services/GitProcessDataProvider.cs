using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace GitChurnCalculator.Services;

public sealed class GitProcessDataProvider : IGitDataProvider
{
    public async Task<IReadOnlyList<string>> GetTrackedFilesAsync(string repoPath, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, "ls-files", ct);
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetCommitCountsAsync(string repoPath, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, "log --pretty=format: --name-only", ct);
        return ParseFileCountsFromNameOnlyLog(output);
    }

    public async Task<Dictionary<string, int>> GetCommitCountsUntilAsync(string repoPath, DateTime until, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, $"log{UntilClause(until)} --pretty=format: --name-only", ct);
        return ParseFileCountsFromNameOnlyLog(output);
    }

    public async Task<Dictionary<string, int>> GetCommitCountsSinceAsync(string repoPath, DateTime since, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, $"log{SinceClause(since)} --pretty=format: --name-only", ct);
        return ParseFileCountsFromNameOnlyLog(output);
    }

    public async Task<Dictionary<string, int>> GetCommitCountsSinceUntilAsync(string repoPath, DateTime since, DateTime until, CancellationToken ct = default)
    {
        var output = await RunGitAsync(
            repoPath,
            $"log{SinceClause(since)}{UntilClause(until)} --pretty=format: --name-only",
            ct);
        return ParseFileCountsFromNameOnlyLog(output);
    }

    public async Task<Dictionary<string, DateTime>> GetFirstCommitDatesAsync(string repoPath, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, "log --reverse --format=%ai --name-only", ct);
        return ParseFirstDatePerFile(output);
    }

    public async Task<Dictionary<string, DateTime>> GetFirstCommitDatesUntilAsync(string repoPath, DateTime until, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, $"log{UntilClause(until)} --reverse --format=%ai --name-only", ct);
        return ParseFirstDatePerFile(output);
    }

    public async Task<Dictionary<string, DateTime>> GetLastCommitDatesAsync(string repoPath, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, "log --format=%ai --name-only", ct);
        return ParseFirstDatePerFile(output);
    }

    public async Task<Dictionary<string, DateTime>> GetLastCommitDatesUntilAsync(string repoPath, DateTime until, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, $"log{UntilClause(until)} --format=%ai --name-only", ct);
        return ParseFirstDatePerFile(output);
    }

    public async Task<Dictionary<string, int>> GetUniqueAuthorCountsAsync(string repoPath, CancellationToken ct = default)
    {
        var output = await RunGitAsync(repoPath, "log --pretty=format:\"COMMIT %ae\" --name-only", ct);
        return ParseUniqueAuthorCounts(output);
    }

    public async Task<Dictionary<string, int>> GetUniqueAuthorCountsUntilAsync(string repoPath, DateTime until, CancellationToken ct = default)
    {
        var output = await RunGitAsync(
            repoPath,
            $"log{UntilClause(until)} --pretty=format:\"COMMIT %ae\" --name-only",
            ct);
        return ParseUniqueAuthorCounts(output);
    }

    public async Task<Dictionary<string, int>> GetUniqueAuthorCountsSinceAsync(string repoPath, DateTime since, CancellationToken ct = default)
    {
        var output = await RunGitAsync(
            repoPath,
            $"log{SinceClause(since)} --pretty=format:\"COMMIT %ae\" --name-only",
            ct);
        return ParseUniqueAuthorCounts(output);
    }

    public async Task<Dictionary<string, int>> GetUniqueAuthorCountsSinceUntilAsync(string repoPath, DateTime since, DateTime until, CancellationToken ct = default)
    {
        var output = await RunGitAsync(
            repoPath,
            $"log{SinceClause(since)}{UntilClause(until)} --pretty=format:\"COMMIT %ae\" --name-only",
            ct);
        return ParseUniqueAuthorCounts(output);
    }

    public static Dictionary<string, int> ParseFileCountsFromNameOnlyLog(string output)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            counts.TryGetValue(trimmed, out var current);
            counts[trimmed] = current + 1;
        }
        return counts;
    }

    /// <summary>
    /// Parses git log output with date lines followed by file name lines.
    /// Returns the first date encountered for each path in traversal order:
    /// with <c>--reverse</c> that is the oldest commit touching the file; without <c>--reverse</c>
    /// (newest-first log) it is the most recent commit touching the file.
    /// </summary>
    public static Dictionary<string, DateTime> ParseFirstDatePerFile(string output)
    {
        var dates = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        DateTime? currentDate = null;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            if (TryParseGitDate(trimmed, out var date))
            {
                currentDate = date;
                continue;
            }

            if (currentDate.HasValue && !dates.ContainsKey(trimmed))
            {
                dates[trimmed] = currentDate.Value;
            }
        }
        return dates;
    }

    public static Dictionary<string, int> ParseUniqueAuthorCounts(string output)
    {
        var fileAuthors = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        string? currentAuthor = null;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim().Trim('"');
            if (trimmed.Length == 0)
                continue;

            if (trimmed.StartsWith("COMMIT ", StringComparison.Ordinal))
            {
                currentAuthor = trimmed["COMMIT ".Length..];
                continue;
            }

            if (currentAuthor is not null && trimmed.Length > 0)
            {
                if (!fileAuthors.TryGetValue(trimmed, out var authors))
                {
                    authors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    fileAuthors[trimmed] = authors;
                }
                authors.Add(currentAuthor);
            }
        }

        return fileAuthors.ToDictionary(kv => kv.Key, kv => kv.Value.Count, StringComparer.Ordinal);
    }

    private static string FormatLogDate(DateTime value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string UntilClause(DateTime until) => $" --until=\"{FormatLogDate(until)}\"";

    private static string SinceClause(DateTime since) => $" --since=\"{FormatLogDate(since)}\"";

    private static bool TryParseGitDate(string value, out DateTime result)
    {
        // Git date format: 2024-01-15 10:30:45 +0100
        if (value.Length >= 10 && char.IsDigit(value[0]) && value[4] == '-')
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
        result = default;
        return false;
    }

    private static async Task<string> RunGitAsync(string repoPath, string arguments, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var error = await errorTask;
            throw new InvalidOperationException($"git {arguments} failed (exit code {process.ExitCode}): {error}");
        }

        return await outputTask;
    }
}

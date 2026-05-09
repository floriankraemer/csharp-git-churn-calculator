using System.Diagnostics;
using GitChurnCalculator.Services;
using Xunit;

namespace GitChurnCalculator.Tests;

public class GitProcessDataProviderIntegrationTests
{
    [Fact]
    public async Task TinyRepo_TrackedFilesCommitCountsAndLineTotals_AreConsistent()
    {
        Assert.True(TryRunGitVersion(), "This test requires `git` on PATH.");

        var dir = Path.Combine(Path.GetTempPath(), "gitchurn-git-int-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);

        try
        {
            RunGit(dir, "init");
            RunGit(dir, "config", "user.email", "int@test.local");
            RunGit(dir, "config", "user.name", "integration");
            await File.WriteAllTextAsync(Path.Combine(dir, "tracked.txt"), "line1\n");

            RunGit(dir, "add", "tracked.txt");
            RunGit(dir, "commit", "-m", "first");
            await File.AppendAllTextAsync(Path.Combine(dir, "tracked.txt"), "line2\n");
            RunGit(dir, "commit", "-am", "second");

            var sut = new GitProcessDataProvider();
            var tracked = await sut.GetTrackedFilesAsync(dir);
            Assert.Contains("tracked.txt", tracked);

            var commits = await sut.GetCommitCountsAsync(dir);
            Assert.Equal(2, commits["tracked.txt"]);

            var totals = await sut.GetLineChangeTotalsAsync(dir);
            Assert.True(totals["tracked.txt"].Added >= 1);
            Assert.True(totals["tracked.txt"].Removed >= 0);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    private static bool TryRunGitVersion()
    {
        try
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            p.Start();
            p.WaitForExit(milliseconds: 30_000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void RunGit(string workingDirectory, params string[] args)
    {
        using var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        foreach (var a in args)
            p.StartInfo.ArgumentList.Add(a);

        p.Start();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(milliseconds: 60_000);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr}");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best effort
        }
    }
}

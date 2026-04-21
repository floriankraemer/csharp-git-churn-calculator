using GitChurnCalculator.Console.Cli;
using GitChurnCalculator.Models;
using Xunit;

namespace GitChurnCalculator.Console.Tests;

public class ChurnAnalysisAppTests
{
    private static FileChurnResult OneRow()
    {
        var d = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        return new FileChurnResult
        {
            FilePath = "src/Example.cs",
            TotalCommits = 12,
            FirstCommitDate = d.AddDays(-20),
            LastCommitDate = d,
            AgeDays = 20,
            ChangesPerWeek = 4.2,
            ChangesPerMonth = 18.0,
            ChangesPerYear = 219.0,
            CommitsLast7Days = 2,
            CommitsLast30Days = 8,
            CommitsLast365Days = 12,
            TotalUniqueAuthors = 3,
            UniqueAuthorsLast7Days = 1,
            UniqueAuthorsLast30Days = 2,
            UniqueAuthorsLast365Days = 3,
            CoveragePercent = 80.0,
            ChurnRiskScore = 2.52,
        };
    }

    private static int SaveExitCode()
    {
        var c = Environment.ExitCode;
        Environment.ExitCode = 0;
        return c;
    }

    private static void RestoreExitCode(int previous) => Environment.ExitCode = previous;

    [Fact]
    public async Task Snapshot_ValidRepo_WritesCsvAndExitZero()
    {
        var prev = SaveExitCode();
        try
        {
            var fake = new FakeChurnCalculator();
            fake.Results.Add(OneRow());
            var app = new ChurnAnalysisApp(fake);
            using var repo = new TempRepoDir();
            var outFile = new FileInfo(Path.Combine(repo.Path, "out.csv"));

            await app.HandleAsync(new DirectoryInfo(repo.Path), "csv", null, outFile, null, null, null);

            Assert.Equal(0, Environment.ExitCode);
            Assert.True(outFile.Exists);
            var text = await File.ReadAllTextAsync(outFile.FullName);
            Assert.Contains("src/Example.cs", text);
        }
        finally
        {
            RestoreExitCode(prev);
        }
    }

    [Fact]
    public async Task Snapshot_MissingRepo_SetsExitCodeOne()
    {
        var prev = SaveExitCode();
        try
        {
            var app = new ChurnAnalysisApp(new FakeChurnCalculator());
            var missing = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "nonexistent-repo-" + Guid.NewGuid()));

            await app.HandleAsync(missing, "csv", null, null, null, null, null);

            Assert.Equal(1, Environment.ExitCode);
        }
        finally
        {
            RestoreExitCode(prev);
        }
    }

    [Fact]
    public async Task Snapshot_MissingCoverageFile_SetsExitCodeOne()
    {
        var prev = SaveExitCode();
        try
        {
            var app = new ChurnAnalysisApp(new FakeChurnCalculator());
            using var repo = new TempRepoDir();
            var cov = new FileInfo(Path.Combine(repo.Path, "missing.xml"));

            await app.HandleAsync(new DirectoryInfo(repo.Path), "csv", cov, null, null, null, null);

            Assert.Equal(1, Environment.ExitCode);
        }
        finally
        {
            RestoreExitCode(prev);
        }
    }

    [Fact]
    public async Task Snapshot_UnsupportedFormat_SetsExitCodeOne()
    {
        var prev = SaveExitCode();
        try
        {
            var fake = new FakeChurnCalculator();
            fake.Results.Add(OneRow());
            var app = new ChurnAnalysisApp(fake);
            using var repo = new TempRepoDir();

            await app.HandleAsync(new DirectoryInfo(repo.Path), "not-a-format", null, null, null, null, null);

            Assert.Equal(1, Environment.ExitCode);
        }
        finally
        {
            RestoreExitCode(prev);
        }
    }

    [Fact]
    public async Task TimeSeries_SarifFormat_SetsExitCodeOne()
    {
        var prev = SaveExitCode();
        try
        {
            var fake = new FakeChurnCalculator();
            fake.Results.Add(OneRow());
            var app = new ChurnAnalysisApp(fake);
            using var repo = new TempRepoDir();

            await app.HandleAsync(
                new DirectoryInfo(repo.Path),
                "sarif",
                null,
                null,
                "week",
                "2024-01-01",
                "2024-01-14");

            Assert.Equal(1, Environment.ExitCode);
        }
        finally
        {
            RestoreExitCode(prev);
        }
    }

    [Fact]
    public async Task TimeSeries_ValidWeek_WritesJson()
    {
        var prev = SaveExitCode();
        try
        {
            var fake = new FakeChurnCalculator();
            fake.Results.Add(OneRow());
            var app = new ChurnAnalysisApp(fake);
            using var repo = new TempRepoDir();
            var outFile = new FileInfo(Path.Combine(repo.Path, "ts.json"));

            await app.HandleAsync(
                new DirectoryInfo(repo.Path),
                "json",
                null,
                outFile,
                "week",
                "2024-01-01",
                "2024-01-14");

            Assert.Equal(0, Environment.ExitCode);
            Assert.True(outFile.Exists);
            Assert.StartsWith("[", (await File.ReadAllTextAsync(outFile.FullName)).TrimStart());
        }
        finally
        {
            RestoreExitCode(prev);
        }
    }

    private sealed class TempRepoDir : IDisposable
    {
        public string Path { get; }

        public TempRepoDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gitchurn-app-" + Guid.NewGuid());
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // best effort cleanup on CI
            }
        }
    }
}

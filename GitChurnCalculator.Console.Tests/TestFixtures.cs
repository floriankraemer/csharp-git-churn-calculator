using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Tests;

internal static class TestFixtures
{
    internal static FileChurnResult OneRow()
    {
        var d = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        return new FileChurnResult
        {
            FilePath = "src/Example.cs",
            TotalCommits = 12,
            LinesAdded = 0,
            LinesRemoved = 0,
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
}

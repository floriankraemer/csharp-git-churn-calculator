using GitChurnCalculator.Models;
using GitChurnCalculator.Services;
using Xunit;

namespace GitChurnCalculator.Tests;

public class GitDataProviderParsingTests
{
    [Fact]
    public void ParseFileCountsFromNameOnlyLog_CountsFilesCorrectly()
    {
        var output = """
                     
                     src/Foo.cs
                     src/Bar.cs
                     
                     src/Foo.cs
                     src/Baz.cs
                     
                     src/Foo.cs
                     """;

        var result = GitProcessDataProvider.ParseFileCountsFromNameOnlyLog(output);

        Assert.Equal(3, result["src/Foo.cs"]);
        Assert.Equal(1, result["src/Bar.cs"]);
        Assert.Equal(1, result["src/Baz.cs"]);
    }

    [Fact]
    public void ParseFileCountsFromNameOnlyLog_EmptyOutput_ReturnsEmpty()
    {
        var result = GitProcessDataProvider.ParseFileCountsFromNameOnlyLog("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseFirstDatePerFile_ExtractsFirstDateForEachFile()
    {
        var output = """
                     2023-01-10 10:00:00 +0000
                     
                     src/Foo.cs
                     src/Bar.cs
                     
                     2023-06-15 14:30:00 +0000
                     
                     src/Foo.cs
                     src/Baz.cs
                     """;

        var result = GitProcessDataProvider.ParseFirstDatePerFile(output);

        Assert.Equal(3, result.Count);
        Assert.Equal(new DateTime(2023, 1, 10), result["src/Foo.cs"].Date);
        Assert.Equal(new DateTime(2023, 1, 10), result["src/Bar.cs"].Date);
        Assert.Equal(new DateTime(2023, 6, 15), result["src/Baz.cs"].Date);
    }

    [Fact]
    public void ParseFirstDatePerFile_KeepsFirstEncounteredDate()
    {
        var output = """
                     2023-01-01 08:00:00 +0000
                     
                     file.cs
                     
                     2024-12-31 23:59:59 +0000
                     
                     file.cs
                     """;

        var result = GitProcessDataProvider.ParseFirstDatePerFile(output);

        Assert.Single(result);
        Assert.Equal(new DateTime(2023, 1, 1), result["file.cs"].Date);
    }

    [Fact]
    public void ParseUniqueAuthorCounts_CountsDistinctAuthors()
    {
        var output = """
                     "COMMIT alice@example.com"
                     
                     src/Foo.cs
                     src/Bar.cs
                     
                     "COMMIT bob@example.com"
                     
                     src/Foo.cs
                     
                     "COMMIT alice@example.com"
                     
                     src/Foo.cs
                     src/Baz.cs
                     """;

        var result = GitProcessDataProvider.ParseUniqueAuthorCounts(output);

        Assert.Equal(2, result["src/Foo.cs"]); // alice + bob
        Assert.Equal(1, result["src/Bar.cs"]); // alice only
        Assert.Equal(1, result["src/Baz.cs"]); // alice only
    }

    [Fact]
    public void ParseUniqueAuthorCounts_EmptyOutput_ReturnsEmpty()
    {
        var result = GitProcessDataProvider.ParseUniqueAuthorCounts("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseUniqueAuthorCounts_SameAuthorMultipleTimes_CountsOnce()
    {
        var output = """
                     "COMMIT dev@test.com"
                     
                     file.cs
                     
                     "COMMIT dev@test.com"
                     
                     file.cs
                     
                     "COMMIT dev@test.com"
                     
                     file.cs
                     """;

        var result = GitProcessDataProvider.ParseUniqueAuthorCounts(output);

        Assert.Single(result);
        Assert.Equal(1, result["file.cs"]);
    }

    [Fact]
    public void ParseLineChangeTotalsFromNumstatLog_SumsPerFileAcrossCommits()
    {
        var output = """
                     12	3	src/A.cs
                     5	10	src/B.cs

                     40	8	src/A.cs
                     """;

        var result = GitProcessDataProvider.ParseLineChangeTotalsFromNumstatLog(output);

        Assert.Equal(2, result.Count);
        Assert.Equal(new LineChangeTotals(52, 11), result["src/A.cs"]);
        Assert.Equal(new LineChangeTotals(5, 10), result["src/B.cs"]);
    }

    [Fact]
    public void ParseLineChangeTotalsFromNumstatLog_BinaryRow_ContributesZeros()
    {
        var output = "-\t-\timg.bin\n100\t50\tsrc/Text.cs";

        var result = GitProcessDataProvider.ParseLineChangeTotalsFromNumstatLog(output);

        Assert.Equal(new LineChangeTotals(0, 0), result["img.bin"]);
        Assert.Equal(new LineChangeTotals(100, 50), result["src/Text.cs"]);
    }

    [Fact]
    public void ParseLineChangeTotalsFromNumstatLog_EmptyOutput_ReturnsEmpty()
    {
        var result = GitProcessDataProvider.ParseLineChangeTotalsFromNumstatLog("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseLineChangeTotalsFromNumstatLog_NonThreeColumnLines_Ignores()
    {
        const string output = "malformed line\n1\t2\tsrc/Ok.cs";

        var result = GitProcessDataProvider.ParseLineChangeTotalsFromNumstatLog(output);

        Assert.Single(result);
        Assert.Equal(new LineChangeTotals(1, 2), result["src/Ok.cs"]);
    }
}

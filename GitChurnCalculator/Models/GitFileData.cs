namespace GitChurnCalculator.Models;

/// <summary>
/// Raw per-file data collected from git before score calculation.
/// </summary>
internal sealed class GitFileData
{
    public required string FilePath { get; init; }
    public int TotalCommits { get; set; }
    public DateTime? FirstCommitDate { get; set; }
    public DateTime? LastCommitDate { get; set; }
    public int CommitsLast7Days { get; set; }
    public int CommitsLast30Days { get; set; }
    public int CommitsLast365Days { get; set; }
    public int TotalUniqueAuthors { get; set; }
    public int UniqueAuthorsLast7Days { get; set; }
    public int UniqueAuthorsLast30Days { get; set; }
    public int UniqueAuthorsLast365Days { get; set; }
}

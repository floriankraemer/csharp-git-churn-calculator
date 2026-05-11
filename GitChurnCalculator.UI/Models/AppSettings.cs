namespace GitChurnCalculator.UI.Models;

public sealed class AppSettings
{
    public string? LastRepositoryPath { get; set; }
    public string? CoverageFilePath { get; set; }
    public string? IncludePattern { get; set; }
    public string? ExcludePattern { get; set; }
    public DateTime? AsOf { get; set; }
}

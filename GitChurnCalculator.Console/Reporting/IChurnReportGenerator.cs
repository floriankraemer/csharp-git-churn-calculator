using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

public interface IChurnReportGenerator
{
    string Generate(IReadOnlyList<FileChurnResult> results, string subtitle);
}

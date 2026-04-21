using GitChurnCalculator.Models;
using GitChurnCalculator.Services;

namespace GitChurnCalculator.Console.Tests;

internal sealed class FakeChurnCalculator : IChurnCalculator
{
    public List<FileChurnResult> Results { get; } = new();

    public Task<IReadOnlyList<FileChurnResult>> AnalyzeAsync(ChurnAnalysisOptions options, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FileChurnResult>>(Results);
}

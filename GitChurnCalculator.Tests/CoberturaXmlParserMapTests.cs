using GitChurnCalculator.Services;
using Xunit;

namespace GitChurnCalculator.Tests;

public class CoberturaXmlParserMapTests
{
    [Fact]
    public void MapToTrackedFiles_DelegatesToMatcher()
    {
        var parser = new CoberturaXmlParser();
        var coverage = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["X.cs"] = 42.0 };
        var tracked = new List<string> { "lib/X.cs" };

        var result = parser.MapToTrackedFiles(coverage, tracked);

        Assert.Single(result);
        Assert.Equal(42.0, result["lib/X.cs"]);
    }
}

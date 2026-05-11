using GitChurnCalculator.Models;
using GitChurnCalculator.UI.Models;

namespace GitChurnCalculator.UI.Tests;

public sealed class FileTreeNodeTests
{
    [Fact]
    public void Build_CreatesHierarchyFromAnalyzerResults()
    {
        var results = new[]
        {
            new FileChurnResult { FilePath = "src/App.cs", TotalCommits = 2 },
            new FileChurnResult { FilePath = "tests/AppTests.cs", TotalCommits = 1 },
        };

        var roots = FileTreeNode.Build(results);

        Assert.Equal(["src", "tests"], roots.Select(x => x.Name));
        var src = roots.Single(x => x.Name == "src");
        var app = Assert.Single(src.Children);
        Assert.Equal("App.cs", app.Name);
        Assert.Same(results[0], app.Result);
    }
}

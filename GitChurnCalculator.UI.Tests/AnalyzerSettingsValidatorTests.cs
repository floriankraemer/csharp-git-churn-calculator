using GitChurnCalculator.UI.Models;
using GitChurnCalculator.UI.Services;

namespace GitChurnCalculator.UI.Tests;

public sealed class AnalyzerSettingsValidatorTests
{
    [Fact]
    public void TryValidate_ReturnsFalseForInvalidIncludeRegex()
    {
        var settings = new AppSettings { IncludePattern = "[" };

        var isValid = AnalyzerSettingsValidator.TryValidate(settings, out var error);

        Assert.False(isValid);
        Assert.Contains("Include pattern", error);
    }

    [Fact]
    public void TryValidate_ReturnsFalseForMissingCoverageFile()
    {
        var settings = new AppSettings
        {
            CoverageFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "coverage.xml"),
        };

        var isValid = AnalyzerSettingsValidator.TryValidate(settings, out var error);

        Assert.False(isValid);
        Assert.Contains("Coverage file does not exist", error);
    }
}

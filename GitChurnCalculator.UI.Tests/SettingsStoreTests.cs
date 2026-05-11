using GitChurnCalculator.UI.Models;
using GitChurnCalculator.UI.Services;

namespace GitChurnCalculator.UI.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public async Task SaveAsync_AndLoadAsync_RoundTripSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), "git-churn-ui-tests", Guid.NewGuid().ToString("N"));
        var store = new SettingsStore(Path.Combine(directory, "settings.json"));
        var settings = new AppSettings
        {
            LastRepositoryPath = "/repo",
            CoverageFilePath = "/coverage.xml",
            IncludePattern = "src/.*",
            ExcludePattern = "bin/.*",
            AsOf = new DateTime(2024, 1, 2),
        };

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(settings.LastRepositoryPath, loaded.LastRepositoryPath);
        Assert.Equal(settings.CoverageFilePath, loaded.CoverageFilePath);
        Assert.Equal(settings.IncludePattern, loaded.IncludePattern);
        Assert.Equal(settings.ExcludePattern, loaded.ExcludePattern);
        Assert.Equal(settings.AsOf, loaded.AsOf);
    }
}

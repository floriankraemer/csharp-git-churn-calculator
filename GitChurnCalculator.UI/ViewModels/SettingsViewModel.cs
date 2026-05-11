using GitChurnCalculator.UI.Models;
using GitChurnCalculator.UI.Services;

namespace GitChurnCalculator.UI.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private string? _repositoryPath;
    private string? _coverageFilePath;
    private string? _includePattern;
    private string? _excludePattern;
    private DateTimeOffset? _asOfDate;
    private string? _validationMessage;

    public SettingsViewModel(AppSettings settings)
    {
        RepositoryPath = settings.LastRepositoryPath;
        CoverageFilePath = settings.CoverageFilePath;
        IncludePattern = settings.IncludePattern;
        ExcludePattern = settings.ExcludePattern;
        AsOfDate = settings.AsOf.HasValue ? new DateTimeOffset(settings.AsOf.Value) : null;
    }

    public string? RepositoryPath
    {
        get => _repositoryPath;
        set => SetProperty(ref _repositoryPath, value);
    }

    public string? CoverageFilePath
    {
        get => _coverageFilePath;
        set => SetProperty(ref _coverageFilePath, value);
    }

    public string? IncludePattern
    {
        get => _includePattern;
        set => SetProperty(ref _includePattern, value);
    }

    public string? ExcludePattern
    {
        get => _excludePattern;
        set => SetProperty(ref _excludePattern, value);
    }

    public DateTimeOffset? AsOfDate
    {
        get => _asOfDate;
        set => SetProperty(ref _asOfDate, value);
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public AppSettings ToSettings() => new()
    {
        LastRepositoryPath = Clean(RepositoryPath),
        CoverageFilePath = Clean(CoverageFilePath),
        IncludePattern = Clean(IncludePattern),
        ExcludePattern = Clean(ExcludePattern),
        AsOf = AsOfDate?.Date,
    };

    public bool Validate()
    {
        var settings = ToSettings();
        if (!string.IsNullOrWhiteSpace(settings.LastRepositoryPath) && !Directory.Exists(settings.LastRepositoryPath))
        {
            ValidationMessage = $"Repository does not exist: {settings.LastRepositoryPath}";
            return false;
        }

        var isValid = AnalyzerSettingsValidator.TryValidate(settings, out var error);
        ValidationMessage = error;
        return isValid;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

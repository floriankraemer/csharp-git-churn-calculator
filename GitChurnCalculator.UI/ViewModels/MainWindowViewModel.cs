using System.Collections.ObjectModel;
using System.Globalization;
using GitChurnCalculator.Models;
using GitChurnCalculator.UI.Models;
using GitChurnCalculator.UI.Services;

namespace GitChurnCalculator.UI.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly RepositoryAnalyzer _repositoryAnalyzer;
    private readonly SettingsStore _settingsStore;
    private AppSettings _settings = new();
    private ObservableCollection<FileTreeNode> _fileTree = [];
    private ObservableCollection<FileMetric> _selectedFileMetrics = [];
    private FileTreeNode? _selectedNode;
    private string? _repositoryPath;
    private string? _statusMessage = "Open a repository to analyze churn.";
    private string? _errorMessage;
    private bool _isAnalyzing;

    public MainWindowViewModel(RepositoryAnalyzer repositoryAnalyzer, SettingsStore settingsStore)
    {
        _repositoryAnalyzer = repositoryAnalyzer;
        _settingsStore = settingsStore;
    }

    public ObservableCollection<FileTreeNode> FileTree
    {
        get => _fileTree;
        private set => SetProperty(ref _fileTree, value);
    }

    public ObservableCollection<FileMetric> SelectedFileMetrics
    {
        get => _selectedFileMetrics;
        private set => SetProperty(ref _selectedFileMetrics, value);
    }

    public FileTreeNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (!SetProperty(ref _selectedNode, value))
                return;

            SelectedFileMetrics = value?.Result is null
                ? []
                : BuildMetrics(value.Result);
        }
    }

    public string? RepositoryPath
    {
        get => _repositoryPath;
        private set
        {
            if (SetProperty(ref _repositoryPath, value))
            {
                OnPropertyChanged(nameof(HasRepository));
                OnPropertyChanged(nameof(HasNoRepository));
            }
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set => SetProperty(ref _isAnalyzing, value);
    }

    public bool HasRepository => !string.IsNullOrWhiteSpace(RepositoryPath);
    public bool HasNoRepository => !HasRepository;

    public async Task InitializeAsync()
    {
        _settings = await _settingsStore.LoadAsync();
        RepositoryPath = Directory.Exists(_settings.LastRepositoryPath) ? _settings.LastRepositoryPath : null;

        if (HasRepository)
            await AnalyzeCurrentRepositoryAsync();
    }

    public SettingsViewModel CreateSettingsViewModel() => new(_settings);

    public async Task OpenRepositoryAsync(string repositoryPath)
    {
        _settings.LastRepositoryPath = repositoryPath;
        RepositoryPath = repositoryPath;
        await AnalyzeCurrentRepositoryAsync();
    }

    public async Task<bool> ApplySettingsAsync(SettingsViewModel settingsViewModel)
    {
        if (!settingsViewModel.Validate())
        {
            ErrorMessage = settingsViewModel.ValidationMessage;
            return false;
        }

        _settings = settingsViewModel.ToSettings();
        RepositoryPath = Directory.Exists(_settings.LastRepositoryPath) ? _settings.LastRepositoryPath : null;
        await _settingsStore.SaveAsync(_settings);

        if (HasRepository)
            await AnalyzeCurrentRepositoryAsync();
        else
            ClearAnalysis("Open a repository to analyze churn.");

        return true;
    }

    public async Task RefreshAsync()
    {
        if (HasRepository)
            await AnalyzeCurrentRepositoryAsync();
    }

    private async Task AnalyzeCurrentRepositoryAsync()
    {
        if (!AnalyzerSettingsValidator.TryValidate(_settings, out var validationError))
        {
            ErrorMessage = validationError;
            return;
        }

        try
        {
            IsAnalyzing = true;
            ErrorMessage = null;
            StatusMessage = "Analyzing repository...";
            SelectedNode = null;
            SelectedFileMetrics = [];

            var results = await _repositoryAnalyzer.AnalyzeAsync(_settings);
            FileTree = FileTreeNode.Build(results);
            StatusMessage = $"Found {results.Count} files with commit history.";
            await _settingsStore.SaveAsync(_settings);
        }
        catch (Exception ex)
        {
            ClearAnalysis("Analysis failed.");
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private void ClearAnalysis(string statusMessage)
    {
        FileTree = [];
        SelectedNode = null;
        SelectedFileMetrics = [];
        StatusMessage = statusMessage;
        ErrorMessage = null;
    }

    private static ObservableCollection<FileMetric> BuildMetrics(FileChurnResult result) =>
    [
        new("File path", result.FilePath),
        new("Churn risk score", result.ChurnRiskScore.ToString("0.####", CultureInfo.InvariantCulture)),
        new("Total commits", result.TotalCommits.ToString(CultureInfo.InvariantCulture)),
        new("First commit", FormatDate(result.FirstCommitDate)),
        new("Last commit", FormatDate(result.LastCommitDate)),
        new("Age days", result.AgeDays.ToString(CultureInfo.InvariantCulture)),
        new("Changes per week", result.ChangesPerWeek.ToString("0.##", CultureInfo.InvariantCulture)),
        new("Changes per month", result.ChangesPerMonth.ToString("0.##", CultureInfo.InvariantCulture)),
        new("Changes per year", result.ChangesPerYear.ToString("0.##", CultureInfo.InvariantCulture)),
        new("Commits last 7 days", result.CommitsLast7Days.ToString(CultureInfo.InvariantCulture)),
        new("Commits last 30 days", result.CommitsLast30Days.ToString(CultureInfo.InvariantCulture)),
        new("Commits last 365 days", result.CommitsLast365Days.ToString(CultureInfo.InvariantCulture)),
        new("Total unique authors", result.TotalUniqueAuthors.ToString(CultureInfo.InvariantCulture)),
        new("Unique authors last 7 days", result.UniqueAuthorsLast7Days.ToString(CultureInfo.InvariantCulture)),
        new("Unique authors last 30 days", result.UniqueAuthorsLast30Days.ToString(CultureInfo.InvariantCulture)),
        new("Unique authors last 365 days", result.UniqueAuthorsLast365Days.ToString(CultureInfo.InvariantCulture)),
        new("Coverage", result.CoveragePercent.HasValue
            ? $"{result.CoveragePercent.Value.ToString("0.##", CultureInfo.InvariantCulture)}%"
            : "Not configured"),
    ];

    private static string FormatDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "Unknown";
}

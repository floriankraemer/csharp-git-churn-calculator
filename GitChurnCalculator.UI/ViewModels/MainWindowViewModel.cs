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
    private IReadOnlyList<FileChurnResult> _analysisResults = [];
    private ObservableCollection<FileTreeNode> _fileTree = [];
    private ObservableCollection<FileChurnResult> _fileList = [];
    private ObservableCollection<FileMetric> _selectedFileMetrics = [];
    private FileTreeNode? _selectedNode;
    private FileChurnResult? _selectedFile;
    private string? _fileNameFilter;
    private string? _repositoryPath;
    private string? _statusMessage = "Open a repository to analyze churn.";
    private string? _errorMessage;
    private bool _isAnalyzing;
    private bool _isSynchronizingSelection;

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

    public ObservableCollection<FileChurnResult> FileList
    {
        get => _fileList;
        private set => SetProperty(ref _fileList, value);
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

            if (_isSynchronizingSelection)
                return;

            if (value?.Result is { } result)
                SelectFile(result);
            else
                ClearSelectedFile();
        }
    }

    public FileChurnResult? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (!SetProperty(ref _selectedFile, value))
                return;

            SelectedFileMetrics = value is null ? [] : BuildMetrics(value);
            if (!_isSynchronizingSelection && value is not null)
                SelectNodeForFile(value.FilePath);
        }
    }

    public string? FileNameFilter
    {
        get => _fileNameFilter;
        set
        {
            if (SetProperty(ref _fileNameFilter, value))
                ApplyFileFilter();
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
            ClearSelection();

            var results = await _repositoryAnalyzer.AnalyzeAsync(_settings);
            _analysisResults = results;
            FileTree = FileTreeNode.Build(results);
            ApplyFileFilter();
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
        _analysisResults = [];
        FileTree = [];
        FileList = [];
        ClearSelection();
        StatusMessage = statusMessage;
        ErrorMessage = null;
    }

    private void ApplyFileFilter()
    {
        var filteredResults = FilterResults(_analysisResults);
        FileList = new ObservableCollection<FileChurnResult>(
            filteredResults
                .OrderByDescending(x => x.ChurnRiskScore)
                .ThenBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase));
    }

    private IReadOnlyList<FileChurnResult> FilterResults(IReadOnlyList<FileChurnResult> results)
    {
        if (string.IsNullOrWhiteSpace(FileNameFilter))
            return results;

        return results
            .Where(x => Path.GetFileName(x.FilePath).Contains(FileNameFilter.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void SelectFile(FileChurnResult result)
    {
        SelectedFile = result;
    }

    private void SelectNodeForFile(string filePath)
    {
        var node = FindNodeByPath(FileTree, filePath);
        if (node is null)
            return;

        ExpandAncestors(node);

        try
        {
            _isSynchronizingSelection = true;
            SelectedNode = node;
        }
        finally
        {
            _isSynchronizingSelection = false;
        }
    }

    private static FileTreeNode? FindNodeByPath(IEnumerable<FileTreeNode> nodes, string filePath)
    {
        foreach (var node in nodes)
        {
            if (node.Result is not null && string.Equals(node.Result.FilePath, filePath, StringComparison.Ordinal))
                return node;

            var child = FindNodeByPath(node.Children, filePath);
            if (child is not null)
                return child;
        }

        return null;
    }

    private static void ExpandAncestors(FileTreeNode node)
    {
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            parent.IsExpanded = true;
        }
    }

    private void ClearSelection()
    {
        SelectedNode = null;
        ClearSelectedFile();
    }

    private void ClearSelectedFile()
    {
        SelectedFile = null;
        SelectedFileMetrics = [];
    }

    private static ObservableCollection<FileMetric> BuildMetrics(FileChurnResult result) =>
    [
        new("File path", result.FilePath),
        new("Churn risk score", result.ChurnRiskScore.ToString("0.####", CultureInfo.InvariantCulture)),
        new("Total commits", result.TotalCommits.ToString(CultureInfo.InvariantCulture)),
        new("Lines added", result.LinesAdded.ToString(CultureInfo.InvariantCulture)),
        new("Lines removed", result.LinesRemoved.ToString(CultureInfo.InvariantCulture)),
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

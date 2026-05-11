using System.Collections.ObjectModel;
using System.Globalization;
using GitChurnCalculator.Models;
using GitChurnCalculator.UI.Models;
using GitChurnCalculator.UI.Services;

namespace GitChurnCalculator.UI.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const int TopTimeSeriesFileLimit = 50;

    private readonly RepositoryAnalyzer _repositoryAnalyzer;
    private readonly SettingsStore _settingsStore;
    private AppSettings _settings = new();
    private IReadOnlyList<FileChurnResult> _analysisResults = [];
    private ObservableCollection<FileTreeNode> _fileTree = [];
    private ObservableCollection<FileChurnResult> _fileList = [];
    private ObservableCollection<FileMetric> _selectedFileMetrics = [];
    private ObservableCollection<TimeSeriesGraphSeries> _timeSeries = [];
    private ObservableCollection<FileMetric> _selectedTimeSeriesMetrics = [];
    private FileTreeNode? _selectedNode;
    private FileChurnResult? _selectedFile;
    private TimeSeriesGraphSeries? _selectedTimeSeries;
    private string? _fileNameFilter;
    private DateTimeOffset? _timeSeriesStartDate = DateTimeOffset.UtcNow.AddMonths(-6);
    private DateTimeOffset? _timeSeriesEndDate = DateTimeOffset.UtcNow;
    private string? _timeSeriesStatusMessage = "Choose a start and end date, then run the time series analysis.";
    private string? _repositoryPath;
    private string? _statusMessage = "Open a repository to analyze churn.";
    private string? _errorMessage;
    private bool _isAnalyzing;
    private bool _isAnalyzingTimeSeries;
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

    public ObservableCollection<TimeSeriesGraphSeries> TimeSeries
    {
        get => _timeSeries;
        private set => SetProperty(ref _timeSeries, value);
    }

    public ObservableCollection<FileMetric> SelectedTimeSeriesMetrics
    {
        get => _selectedTimeSeriesMetrics;
        private set => SetProperty(ref _selectedTimeSeriesMetrics, value);
    }

    public TimeSeriesGraphSeries? SelectedTimeSeries
    {
        get => _selectedTimeSeries;
        set
        {
            if (!SetProperty(ref _selectedTimeSeries, value))
                return;

            SelectedTimeSeriesMetrics = value is null ? [] : BuildTimeSeriesMetrics(value);
        }
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

    public DateTimeOffset? TimeSeriesStartDate
    {
        get => _timeSeriesStartDate;
        set => SetProperty(ref _timeSeriesStartDate, value);
    }

    public DateTimeOffset? TimeSeriesEndDate
    {
        get => _timeSeriesEndDate;
        set => SetProperty(ref _timeSeriesEndDate, value);
    }

    public string? TimeSeriesStatusMessage
    {
        get => _timeSeriesStatusMessage;
        private set => SetProperty(ref _timeSeriesStatusMessage, value);
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

    public bool IsAnalyzingTimeSeries
    {
        get => _isAnalyzingTimeSeries;
        private set => SetProperty(ref _isAnalyzingTimeSeries, value);
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

    public async Task RunTimeSeriesAsync()
    {
        if (!HasRepository)
            return;

        if (!TimeSeriesStartDate.HasValue || !TimeSeriesEndDate.HasValue)
        {
            TimeSeriesStatusMessage = "Start and end date are required.";
            return;
        }

        var from = TimeSeriesStartDate.Value.Date;
        var to = TimeSeriesEndDate.Value.Date;
        if (from > to)
        {
            TimeSeriesStatusMessage = "Start date must be on or before end date.";
            return;
        }

        try
        {
            IsAnalyzingTimeSeries = true;
            TimeSeriesStatusMessage = "Analyzing time series...";
            SelectedTimeSeries = null;
            var points = await _repositoryAnalyzer.AnalyzeTimeSeriesAsync(_settings, from, to);
            TimeSeries = BuildTimeSeries(points);
            TimeSeriesStatusMessage = $"Rendered {TimeSeries.Count} file series across {points.Count} time points.";
        }
        catch (Exception ex)
        {
            TimeSeries = [];
            SelectedTimeSeries = null;
            TimeSeriesStatusMessage = ex.Message;
        }
        finally
        {
            IsAnalyzingTimeSeries = false;
        }
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

    public void SelectTimeSeries(string filePath)
    {
        SelectedTimeSeries = TimeSeries.FirstOrDefault(series =>
            string.Equals(series.FilePath, filePath, StringComparison.Ordinal));
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

    private static ObservableCollection<FileMetric> BuildTimeSeriesMetrics(TimeSeriesGraphSeries series)
    {
        var point = series.DetailPoint;
        if (point is null)
            return [];

        var deltas = BuildAverageDeltas(series);
        return
        [
            new("File path", series.FilePath),
            new("Date", point.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new("Churn risk score", point.ChurnRiskScore.ToString("0.####", CultureInfo.InvariantCulture)),
            new("Max churn risk score", series.MaxChurnRiskScore.ToString("0.####", CultureInfo.InvariantCulture)),
            new("Changes per week", point.ChangesPerWeek.ToString("0.##", CultureInfo.InvariantCulture)),
            new("Coverage", point.CoveragePercent.HasValue
                ? $"{point.CoveragePercent.Value.ToString("0.##", CultureInfo.InvariantCulture)}%"
                : "Not configured"),
            new("Lines added (cum.)", point.LinesAdded.ToString(CultureInfo.InvariantCulture)),
            new("Lines removed (cum.)", point.LinesRemoved.ToString(CultureInfo.InvariantCulture)),
            new("Avg lines added/commit", FormatAverage(point.LinesAdded, point.TotalCommits)),
            new("Avg lines removed/commit", FormatAverage(point.LinesRemoved, point.TotalCommits)),
            new("Avg delta lines added/bucket", deltas.Added.ToString("0.##", CultureInfo.InvariantCulture)),
            new("Avg delta lines removed/bucket", deltas.Removed.ToString("0.##", CultureInfo.InvariantCulture)),
        ];
    }

    private static (double Added, double Removed) BuildAverageDeltas(TimeSeriesGraphSeries series)
    {
        var ordered = series.Points.OrderBy(point => point.Date).ToArray();
        if (ordered.Length < 2)
            return (0, 0);

        var added = 0L;
        var removed = 0L;
        for (var i = 1; i < ordered.Length; i++)
        {
            added += ordered[i].LinesAdded - ordered[i - 1].LinesAdded;
            removed += ordered[i].LinesRemoved - ordered[i - 1].LinesRemoved;
        }

        var buckets = ordered.Length - 1;
        return (
            Math.Round(added / (double)buckets, 2),
            Math.Round(removed / (double)buckets, 2));
    }

    private static string FormatAverage(int value, int count) =>
        count > 0
            ? Math.Round(value / (double)count, 2).ToString("0.##", CultureInfo.InvariantCulture)
            : "Not available";

    private static ObservableCollection<TimeSeriesGraphSeries> BuildTimeSeries(IReadOnlyList<TimeSeriesPoint> points)
    {
        var orderedPoints = points
            .OrderBy(point => point.AsOf)
            .ToArray();

        var topFiles = orderedPoints
            .SelectMany(point => point.Files.Select(file => new { point.AsOf, File = file }))
            .GroupBy(item => item.File.FilePath, StringComparer.Ordinal)
            .OrderByDescending(group => group.Max(item => item.File.ChurnRiskScore))
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(TopTimeSeriesFileLimit)
            .Select(group => group.Key)
            .ToArray();

        var series = topFiles.Select(filePath => new TimeSeriesGraphSeries
        {
            FilePath = filePath,
            Points = orderedPoints
                .Select(point =>
                {
                    var file = point.Files.FirstOrDefault(file =>
                        string.Equals(file.FilePath, filePath, StringComparison.Ordinal));

                    return new TimeSeriesGraphPoint
                    {
                        Date = point.AsOf,
                        ChurnRiskScore = file?.ChurnRiskScore ?? 0,
                        ChangesPerWeek = file?.ChangesPerWeek ?? 0,
                        TotalCommits = file?.TotalCommits ?? 0,
                        LinesAdded = file?.LinesAdded ?? 0,
                        LinesRemoved = file?.LinesRemoved ?? 0,
                        CoveragePercent = file?.CoveragePercent,
                    };
                })
                .ToArray(),
        });

        return new ObservableCollection<TimeSeriesGraphSeries>(series);
    }
}

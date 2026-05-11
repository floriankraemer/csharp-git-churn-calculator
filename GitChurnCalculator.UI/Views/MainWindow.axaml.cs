using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GitChurnCalculator.UI.Models;
using GitChurnCalculator.UI.ViewModels;
using ScottPlot.Avalonia;

namespace GitChurnCalculator.UI.Views;

public sealed partial class MainWindow : Window
{
    private bool _initialized;
    private MainWindowViewModel? _subscribedViewModel;

    public MainWindow()
    {
        InitializeComponent();
        Opened += MainWindow_Opened;
        DataContextChanged += MainWindow_DataContextChanged;
    }

    private void MainWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;

        _subscribedViewModel = DataContext as MainWindowViewModel;
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged += ViewModel_PropertyChanged;

        RenderTimeSeriesPlot(_subscribedViewModel?.TimeSeries ?? []);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel viewModel)
            return;

        if (e.PropertyName is nameof(MainWindowViewModel.TimeSeries) or nameof(MainWindowViewModel.SelectedTimeSeries))
        {
            RenderTimeSeriesPlot(viewModel.TimeSeries, viewModel.SelectedTimeSeries);
        }
    }

    private async void MainWindow_Opened(object? sender, EventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        if (DataContext is MainWindowViewModel viewModel)
            await viewModel.InitializeAsync();
    }

    private async void OpenRepository_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Repository",
            AllowMultiple = false,
        });

        var selectedFolder = folders.FirstOrDefault();
        if (selectedFolder is null)
            return;

        if (selectedFolder.TryGetLocalPath() is { } path)
            await viewModel.OpenRepositoryAsync(path);
    }

    private async void Refresh_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            await viewModel.RefreshAsync();
    }

    private async void RunTimeSeries_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            await viewModel.RunTimeSeriesAsync();
    }

    private async void Settings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var settingsViewModel = viewModel.CreateSettingsViewModel();
        var window = new SettingsWindow
        {
            DataContext = settingsViewModel,
        };

        var accepted = await window.ShowDialog<bool>(this);
        if (accepted)
            await viewModel.ApplySettingsAsync(settingsViewModel);
    }

    private void RenderTimeSeriesPlot(
        IEnumerable<TimeSeriesGraphSeries> series,
        TimeSeriesGraphSeries? selectedSeries = null)
    {
        var plot = this.FindControl<AvaPlot>("TimeSeriesPlot");
        if (plot is null)
            return;

        plot.Plot.Clear();

        var orderedSeries = series
            .OrderBy(item => IsSelected(item, selectedSeries))
            .ToArray();

        foreach (var item in orderedSeries)
        {
            var orderedPoints = item.Points
                .OrderBy(point => point.Date)
                .ToArray();

            if (orderedPoints.Length == 0)
                continue;

            var xs = orderedPoints
                .Select(point => point.Date.ToOADate())
                .ToArray();
            var ys = orderedPoints
                .Select(point => point.ChurnRiskScore)
                .ToArray();

            var scatter = plot.Plot.Add.Scatter(xs, ys);
            if (IsSelected(item, selectedSeries))
            {
                scatter.LineWidth = 3;
                scatter.MarkerSize = 7;
            }
            else
            {
                scatter.LineWidth = 1.5f;
                scatter.MarkerSize = 4;
            }
        }

        plot.Plot.Title("Git churn risk graph");
        plot.Plot.XLabel("Date");
        plot.Plot.YLabel("Churn risk score");
        plot.Plot.Axes.DateTimeTicksBottom();
        plot.Plot.Axes.AutoScale();
        plot.Refresh();
    }

    private static bool IsSelected(TimeSeriesGraphSeries item, TimeSeriesGraphSeries? selectedSeries) =>
        selectedSeries is not null && string.Equals(item.FilePath, selectedSeries.FilePath, StringComparison.Ordinal);
}

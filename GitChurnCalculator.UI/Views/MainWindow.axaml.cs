using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GitChurnCalculator.UI.ViewModels;

namespace GitChurnCalculator.UI.Views;

public sealed partial class MainWindow : Window
{
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();
        Opened += MainWindow_Opened;
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
}

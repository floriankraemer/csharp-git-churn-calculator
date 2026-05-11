using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GitChurnCalculator.UI.ViewModels;

namespace GitChurnCalculator.UI.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsWindow() => InitializeComponent();

    private async void BrowseRepository_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
            return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Repository",
            AllowMultiple = false,
        });

        var selectedFolder = folders.FirstOrDefault();
        if (selectedFolder?.TryGetLocalPath() is { } path)
            viewModel.RepositoryPath = path;
    }

    private async void BrowseCoverage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Coverage XML",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("XML coverage files")
                {
                    Patterns = ["*.xml"],
                    MimeTypes = ["application/xml", "text/xml"],
                },
                FilePickerFileTypes.All,
            ],
        });

        var selectedFile = files.FirstOrDefault();
        if (selectedFile?.TryGetLocalPath() is { } path)
            viewModel.CoverageFilePath = path;
    }

    private void ClearAsOf_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
            viewModel.AsOfDate = null;
    }

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && !viewModel.Validate())
            return;

        Close(true);
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);
}

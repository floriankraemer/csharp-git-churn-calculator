using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitChurnCalculator.Services;
using GitChurnCalculator.UI.Services;
using GitChurnCalculator.UI.ViewModels;
using GitChurnCalculator.UI.Views;

namespace GitChurnCalculator.UI;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsStore = SettingsStore.CreateDefault();
            var analyzer = new RepositoryAnalyzer(
                new ChurnCalculator(new GitProcessDataProvider(), new AutoDetectCoverageParser()));

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(analyzer, settingsStore),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

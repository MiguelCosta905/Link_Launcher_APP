using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShiftLauncher.App.ViewModels;
using ShiftLauncher.Core.Launch;
using ShiftLauncher.Core.ModLoaders;
using ShiftLauncher.Core.Storage;
using ShiftLauncher.Core.Auth;

namespace ShiftLauncher.App;

public partial class App : Application
{
    private SettingsService? _settingsService;
    private LauncherService? _launcherService;
    private MainWindowViewModel? _mainWindowViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var baseDirectory = FilePaths.GetAppDataDirectory();
            _settingsService = new SettingsService(baseDirectory);

            _launcherService = new LauncherService(
            new MinecraftDirectoryService(),
            _settingsService,
            new JavaService(),
            new ProcessMonitorService(),
            new ModLoaderInstallService(new JavaService()));


            _mainWindowViewModel = new MainWindowViewModel(
            _launcherService,
            new MicrosoftAuthService());

            desktop.MainWindow = new MainWindow(_mainWindowViewModel);
            desktop.ShutdownRequested += OnShutdownRequested;
            _ = LoadStartupDataAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task LoadStartupDataAsync()
    {
        if (_launcherService is null || _mainWindowViewModel is null)
            return;

        try
        {
            var settings = await _launcherService.LoadSettingsAsync();
            _mainWindowViewModel.ApplySettings(settings);

            var versions = await _launcherService.GetVersionsAsync(settings);
            _mainWindowViewModel.ApplyVersions(versions);
        }
        catch (Exception ex)
        {
            _mainWindowViewModel.HandleStartupError(ex);
        }
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (_launcherService is null || _mainWindowViewModel is null)
            return;

        await _launcherService.SaveSettingsAsync(_mainWindowViewModel.Settings);
    }
}

using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShiftLauncher.App.ViewModels;
using ShiftLauncher.Core.Launch;
using ShiftLauncher.Core.Storage;

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

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var baseDirectory = FilePaths.GetAppDataDirectory();
            _settingsService = new SettingsService(baseDirectory);
            _launcherService = new LauncherService(
                new MinecraftDirectoryService(),
                _settingsService);
           _mainWindowViewModel = new MainWindowViewModel(_launcherService);
            desktop.MainWindow = new MainWindow(_mainWindowViewModel);
            desktop.ShutdownRequested += OnShutdownRequested;

            try
            {
                var settings = await _launcherService.LoadSettingsAsync();
                _mainWindowViewModel.ApplySettings(settings);

                var versions = await _launcherService.GetVersionsAsync(settings);
                _mainWindowViewModel.ApplyVersions(versions);
            }
            catch (Exception ex)
            {
                _mainWindowViewModel.StatusText = $"Startup error: {ex.Message}";
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (_launcherService is null || _mainWindowViewModel is null)
            return;

        await _launcherService.SaveSettingsAsync(_mainWindowViewModel.Settings);
    }
}

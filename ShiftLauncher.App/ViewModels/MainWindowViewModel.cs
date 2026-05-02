using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ShiftLauncher.Core.Models;
using System.Threading.Tasks;
using System.Windows.Input;
using ShiftLauncher.Core.Launch;
using Avalonia.Threading;
using CmlLib.Core.Auth;
using ShiftLauncher.Core.Auth;
using System.Text;

namespace ShiftLauncher.App.ViewModels;
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly LauncherService _launcherService;
    private readonly MicrosoftAuthService _authService;
    private LauncherSettings _settings = new();
    private string _statusText = "Launcher ready.";
    private MinecraftVersionItem? _selectedVersion;
    private MSession? _microsoftSession;
    public ICommand PlayOnlineCommand { get; }
    private string _accountText = "Not logged in";
    private bool _isBusy;
    private double _fileProgressPercent;
    private double _byteProgressPercent;
    private bool _isProgressVisible;
    private AppLogEntry? _selectedLogEntry;

    public ObservableCollection<AppLogEntry> Logs { get; } = [];

    public AppLogEntry? SelectedLogEntry
    {
        get => _selectedLogEntry;
        set
        {
            if (_selectedLogEntry == value)
                return;

            _selectedLogEntry = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedLogDetails));
        }
    }

    public string SelectedLogDetails => SelectedLogEntry?.Details ?? string.Empty;


    public MainWindowViewModel(
    LauncherService launcherService,
    MicrosoftAuthService authService)
    {
        _launcherService = launcherService;
        _authService = authService;

        PlayOfflineCommand = new AsyncRelayCommand(PlayOfflineAsync, () => !IsBusy);
        LoginMicrosoftCommand = new AsyncRelayCommand(LoginMicrosoftAsync, () => !IsBusy);
        PlayOnlineCommand = new AsyncRelayCommand(PlayOnlineAsync, () => !IsBusy && _microsoftSession is not null);

    }

    private void AddLog(string level, string message, string details = "")
    {
        var entry = new AppLogEntry
        {
            Level = level,
            Message = message,
            Details = details
        };

        Logs.Insert(0, entry);
        SelectedLogEntry = entry;
    }

    private static string BuildExceptionDetails(Exception ex)
    {
        var builder = new StringBuilder();

        var current = ex;
        var depth = 0;

        while (current is not null)
        {
            builder.AppendLine($"Exception #{depth + 1}");
            builder.AppendLine(current.GetType().FullName);
            builder.AppendLine(current.Message);
            builder.AppendLine(current.StackTrace);
            builder.AppendLine();

            current = current.InnerException;
            depth++;
        }

        return builder.ToString();
    }


    public ICommand PlayOfflineCommand { get; }
    public ICommand LoginMicrosoftCommand { get; }

    public string AccountText
    {
        get => _accountText;
        private set
        {
            if (_accountText == value)
                return;

            _accountText = value;
            OnPropertyChanged();
        }
    }


    public double FileProgressPercent
    {
        get => _fileProgressPercent;
        private set
        {
            if (_fileProgressPercent == value)
                return;

            _fileProgressPercent = value;
            OnPropertyChanged();
        }
    }

    public double ByteProgressPercent
    {
        get => _byteProgressPercent;
        private set
        {
            if (_byteProgressPercent == value)
                return;

            _byteProgressPercent = value;
            OnPropertyChanged();
        }
    }

    public bool IsProgressVisible
    {
        get => _isProgressVisible;
        private set
        {
            if (_isProgressVisible == value)
                return;

            _isProgressVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();

            if (PlayOfflineCommand is AsyncRelayCommand offlineCommand)
            offlineCommand.RaiseCanExecuteChanged();

            if (LoginMicrosoftCommand is AsyncRelayCommand loginCommand)
                loginCommand.RaiseCanExecuteChanged();

            if (PlayOnlineCommand is AsyncRelayCommand onlineCommand)
            onlineCommand.RaiseCanExecuteChanged();
        }
    }

private async Task PlayOfflineAsync()
{
    IsBusy = true;
    StatusText = "Preparing Minecraft...";

    try
    {
        var request = _launcherService.CreateLaunchRequest(Settings);
        
        IsProgressVisible = true;
        FileProgressPercent = 0;
        ByteProgressPercent = 0;

        request.ProgressChanged = progress =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrWhiteSpace(progress.StatusText))
                    StatusText = progress.StatusText;

                if (progress.FileProgressPercent > 0)
                    FileProgressPercent = progress.FileProgressPercent;

                if (progress.ByteProgressPercent > 0)
                    ByteProgressPercent = progress.ByteProgressPercent;
            });
        };

        StatusText = $"Checking Java for Minecraft {request.VersionName}...";
        var result = await _launcherService.LaunchOfflineAsync(request);

        StatusText = result.Success
            ? $"Minecraft launched. Process ID: {result.ProcessId}"
            : $"Launch failed: {result.Message}";
    }
    finally
    {
        IsBusy = false;
        IsProgressVisible = false;
    }
}


    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MinecraftVersionItem> Versions { get; } = [];

    public LauncherSettings Settings
    {
        get => _settings;
        private set
        {
            _settings = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GameDirectory));
            OnPropertyChanged(nameof(MinecraftVersion));
            OnPropertyChanged(nameof(MaximumRamMb));
            OnPropertyChanged(nameof(PlayerName));
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText == value)
                return;

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string GameDirectory => Settings.GameDirectory;

    public string MinecraftVersion
    {
        get => Settings.LastProfile.MinecraftVersion;
        set
        {
            if (Settings.LastProfile.MinecraftVersion == value)
                return;

            Settings.LastProfile.MinecraftVersion = value;
            OnPropertyChanged();
        }
    }

    public MinecraftVersionItem? SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            if (_selectedVersion == value)
                return;

            _selectedVersion = value;

            if (value is not null)
                MinecraftVersion = value.Name;

            OnPropertyChanged();
        }
    }

    public int MaximumRamMb
    {
        get => Settings.LastProfile.MaximumRamMb;
        set
        {
            if (Settings.LastProfile.MaximumRamMb == value)
                return;

            Settings.LastProfile.MaximumRamMb = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MaximumRamMbDecimal));
        }
    }
    public decimal MaximumRamMbDecimal
    {
        get => MaximumRamMb;
        set => MaximumRamMb = (int)value;
    }

    public int MinimumRamMb => 1024;

    public int MaximumAllowedRamMb => 16384;


    public string PlayerName
    {
        get => Settings.LastProfile.PlayerName;
        set
        {
            if (Settings.LastProfile.PlayerName == value)
                return;

            Settings.LastProfile.PlayerName = value;
            OnPropertyChanged();
        }
    }

    public void ApplySettings(LauncherSettings settings)
    {
        Settings = settings;
        StatusText = "Settings loaded.";
    }

    public void ApplyVersions(IEnumerable<MinecraftVersionItem> versions)
    {
        Versions.Clear();

        foreach (var version in versions)
            Versions.Add(version);

        SelectedVersion = Versions.FirstOrDefault(x => x.Name == MinecraftVersion)
            ?? Versions.FirstOrDefault();

        StatusText = Versions.Count > 0
            ? $"Loaded {Versions.Count} versions."
            : "No versions found.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async Task LoginMicrosoftAsync()
{
    IsBusy = true;
    StatusText = "Opening Microsoft device login...";
    AddLog("Info", "Microsoft login started.");

    try
    {
        _microsoftSession = await _authService.LoginWithDeviceCodeAsync(message =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusText = message;
                AddLog("Info", "Microsoft device code received.", message);
            });
            return Task.CompletedTask;
        });

        AccountText = $"Logged in as {_microsoftSession.Username}";
        PlayerName = _microsoftSession.Username;
        StatusText = "Microsoft login completed.";

        AddLog(
            "Info",
            $"Logged in as {_microsoftSession.Username}",
            $"Username: {_microsoftSession.Username}{Environment.NewLine}UUID: {_microsoftSession.UUID}");
    }
    catch (Exception ex)
    {
        StatusText = $"Microsoft login failed: {ex.Message}";
        AddLog("Error", "Microsoft login failed.", BuildExceptionDetails(ex));
    }
    finally
    {
        IsBusy = false;
    }
    if (PlayOnlineCommand is AsyncRelayCommand onlineCommand)
    onlineCommand.RaiseCanExecuteChanged();
}

    private async Task PlayOnlineAsync()
    {
        if (_microsoftSession is null)
        {
            StatusText = "Login with Microsoft before playing online.";
            AddLog("Warning", "Online launch blocked.", "No Microsoft session is available.");
            return;
        }

        IsBusy = true;
        StatusText = "Preparing online Minecraft launch...";

        try
        {
            var request = _launcherService.CreateLaunchRequest(Settings);
            request.UseOfflineMode = false;
            request.Session = _microsoftSession;
            request.PlayerName = _microsoftSession.Username;

            IsProgressVisible = true;
            FileProgressPercent = 0;
            ByteProgressPercent = 0;

            request.ProgressChanged = progress =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!string.IsNullOrWhiteSpace(progress.StatusText))
                        StatusText = progress.StatusText;

                    if (progress.FileProgressPercent > 0)
                        FileProgressPercent = progress.FileProgressPercent;

                    if (progress.ByteProgressPercent > 0)
                        ByteProgressPercent = progress.ByteProgressPercent;
                });
            };

            StatusText = $"Checking Java for Minecraft {request.VersionName}...";
            AddLog("Info", "Online launch started.", $"Version: {request.VersionName}");

            var result = await _launcherService.LaunchOfflineAsync(request);

            StatusText = result.Success
                ? $"Minecraft launched online. Process ID: {result.ProcessId}"
                : $"Online launch failed: {result.Message}";

            AddLog(
                result.Success ? "Info" : "Error",
                result.Success ? "Online launch completed." : "Online launch failed.",
                result.Exception is null ? result.Message : BuildExceptionDetails(result.Exception));
        }
        catch (Exception ex)
        {
            StatusText = $"Online launch failed: {ex.Message}";
            AddLog("Error", "Online launch failed.", BuildExceptionDetails(ex));
        }
        finally
        {
            IsBusy = false;
            IsProgressVisible = false;
        }
    }

}

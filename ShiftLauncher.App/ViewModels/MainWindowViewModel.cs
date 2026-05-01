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

namespace ShiftLauncher.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly LauncherService _launcherService;
    private LauncherSettings _settings = new();
    private string _statusText = "Launcher ready.";
    private MinecraftVersionItem? _selectedVersion;
    private bool _isBusy;
    private double _fileProgressPercent;
    private double _byteProgressPercent;
    private bool _isProgressVisible;

    public MainWindowViewModel(LauncherService launcherService)
    {
        _launcherService = launcherService;
        PlayOfflineCommand = new AsyncRelayCommand(PlayOfflineAsync, () => !IsBusy);
    }
    public ICommand PlayOfflineCommand { get; }
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

            if (PlayOfflineCommand is AsyncRelayCommand command)
                command.RaiseCanExecuteChanged();
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

        StatusText = $"Launching Minecraft {request.VersionName}...";
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
}

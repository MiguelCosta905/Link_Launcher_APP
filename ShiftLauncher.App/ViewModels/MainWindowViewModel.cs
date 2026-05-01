using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ShiftLauncher.Core.Models;
using System.Threading.Tasks;
using System.Windows.Input;
using ShiftLauncher.Core.Launch;


namespace ShiftLauncher.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly LauncherService _launcherService;
    private LauncherSettings _settings = new();
    private string _statusText = "Launcher ready.";
    private MinecraftVersionItem? _selectedVersion;
    private bool _isBusy;

    public MainWindowViewModel(LauncherService launcherService)
    {
        _launcherService = launcherService;
        PlayOfflineCommand = new AsyncRelayCommand(PlayOfflineAsync, () => !IsBusy);
    }
    public ICommand PlayOfflineCommand { get; }

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

        StatusText = $"Launching Minecraft {request.VersionName}...";
        var result = await _launcherService.LaunchOfflineAsync(request);

        StatusText = result.Success
            ? $"Minecraft launched. Process ID: {result.ProcessId}"
            : $"Launch failed: {result.Message}";
    }
    finally
    {
        IsBusy = false;
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
        }
    }

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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CmlLib.Core.Auth;
using LinkLauncher.Core.Auth;
using LinkLauncher.Core.Launch;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.ModLoaders;
using LinkLauncher.Core.Utilities;


namespace LinkLauncher.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const int MaxConsoleTextLength = 24_000;
    private const int MaxLogEntries = 200;
    private static readonly TimeSpan ProgressUpdateInterval = TimeSpan.FromMilliseconds(150);
    private readonly ModLoaderVersionService _modLoaderVersionService = new();
    private readonly List<MinecraftVersionItem> _allVersions = new();
    private readonly LauncherService _launcherService;
    private readonly MicrosoftAuthService _authService;
    private LauncherSettings _settings = new();
    private string _statusText = "Launcher pronto.";
    private string _accountText = "Sem sessão Microsoft";
    private string _minecraftConsoleText = string.Empty;
    private string _selectedThemeMode = "Sistema";
    private MinecraftVersionItem? _selectedVersion;
    private MSession? _microsoftSession;
    private bool _isBusy;
    private double _fileProgressPercent;
    private double _byteProgressPercent;
    private bool _isProgressVisible;
    private AppLogEntry? _selectedLogEntry;
    private int _selectedRamIndex;
    private bool _isUpdatingRam;
    private DateTimeOffset _lastProgressUiUpdate = DateTimeOffset.MinValue;
    private IReadOnlySet<string> _supportedMinecraftVersions = new HashSet<string>();
    private bool _showReleases = true;
    private bool _showSnapshots;
    private bool _showOldBeta;
    private bool _showOldAlpha;
    

    public MainWindowViewModel(
        LauncherService launcherService,
        MicrosoftAuthService authService)
    {
        _launcherService = launcherService;
        _authService = authService;

        PlayOfflineCommand = new AsyncRelayCommand(PlayOfflineAsync, () => !IsBusy);
        LoginMicrosoftCommand = new AsyncRelayCommand(LoginMicrosoftAsync, () => !IsBusy);
        PlayOnlineCommand = new AsyncRelayCommand(PlayOnlineAsync, () => !IsBusy && _microsoftSession is not null);
        OpenGameFolderCommand = new AsyncRelayCommand(OpenGameFolderAsync);
        OpenInstallationFolderCommand = new AsyncRelayCommand(OpenInstallationFolderAsync);
        NewInstallationCommand = new AsyncRelayCommand(NewInstallationAsync, () => !IsBusy);
        DuplicateInstallationCommand = new AsyncRelayCommand(DuplicateInstallationAsync, () => !IsBusy);
        DeleteInstallationCommand = new AsyncRelayCommand(DeleteInstallationAsync, () => !IsBusy && Settings.Profiles.Count > 1);

        LoadRamOptions();
        ApplyTheme(SelectedThemeMode);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand PlayOfflineCommand { get; }
    public ICommand LoginMicrosoftCommand { get; }
    public ICommand PlayOnlineCommand { get; }
    public ICommand OpenGameFolderCommand { get; }
    public ICommand OpenInstallationFolderCommand { get; }
    public ICommand NewInstallationCommand { get; }
    public ICommand DuplicateInstallationCommand { get; }
    public ICommand DeleteInstallationCommand { get; }

    public ObservableCollection<MinecraftVersionItem> Versions { get; } = new();
    public ObservableCollection<AppLogEntry> Logs { get; } = new();
    public ObservableCollection<int> RamOptions { get; } = new();
    public ObservableCollection<string> ThemeModes { get; } = new() { "Sistema", "Claro", "Escuro" };
    public ObservableCollection<LoaderType> LoaderTypes { get; } = new(Enum.GetValues<LoaderType>());
    public ObservableCollection<MinecraftVersionItem> FilteredVersions { get; } = new();
    public ObservableCollection<string> LoaderVersions { get; } = new();
    public ObservableCollection<LauncherProfile> Installations { get; } = new();

    public bool ShowReleases
    {
        get => _showReleases;
        set
        {
            if (_showReleases == value) return;
            _showReleases = value;
            OnPropertyChanged();
            ApplyMinecraftVersionFilters();
        }
    }

    public bool ShowSnapshots
    {
        get => _showSnapshots;
        set
        {
            if (_showSnapshots == value) return;
            _showSnapshots = value;
            OnPropertyChanged();
            ApplyMinecraftVersionFilters();
        }
    }

    public bool ShowOldBeta
    {
        get => _showOldBeta;
        set
        {
            if (_showOldBeta == value) return;
            _showOldBeta = value;
            OnPropertyChanged();
            ApplyMinecraftVersionFilters();
        }
    }

    public bool ShowOldAlpha
    {
        get => _showOldAlpha;
        set
        {
            if (_showOldAlpha == value) return;
            _showOldAlpha = value;
            OnPropertyChanged();
            ApplyMinecraftVersionFilters();
        }
    }

    public bool IsVanillaSelected => SelectedLoaderType == LoaderType.Vanilla;
    public bool IsLoaderSelected => SelectedLoaderType != LoaderType.Vanilla;


    public LauncherSettings Settings
    {
        get => _settings;
        private set
        {
            _settings = value;
            OnPropertyChanged();
            RefreshInstallations();
            OnPropertyChanged(nameof(SelectedInstallation));
            OnPropertyChanged(nameof(InstallationName));
            OnPropertyChanged(nameof(InstallationDirectory));
            OnPropertyChanged(nameof(InstallationSummary));
            OnPropertyChanged(nameof(GameDirectory));
            OnPropertyChanged(nameof(MinecraftVersion));
            OnPropertyChanged(nameof(MaximumRamMb));
            OnPropertyChanged(nameof(PlayerName));
            OnPropertyChanged(nameof(SelectedLoaderType));
            OnPropertyChanged(nameof(LoaderVersion));
            OnPropertyChanged(nameof(ModLoaderSummary));
            OnPropertyChanged(nameof(IsVanillaSelected));
            OnPropertyChanged(nameof(IsLoaderSelected));
        }
    }

    public LauncherProfile SelectedProfile => Settings.GetSelectedProfile();

    public string InstallationName
    {
        get => SelectedProfile.Name;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "Instancia" : value.Trim();
            if (SelectedProfile.Name == normalized)
                return;

            SelectedProfile.Name = normalized;
            RefreshInstallations();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedInstallation));
            OnPropertyChanged(nameof(InstallationSummary));
        }
    }

    public string InstallationDirectory => Path.Combine(Settings.SettingsDirectory, "Instances", SelectedProfile.Id);

    public string InstallationSummary =>
        $"{InstallationName} - {MinecraftVersion} - {SelectedRamLabel} - {ModLoaderSummary}";

    public LauncherProfile? SelectedInstallation
    {
        get => SelectedProfile;
        set
        {
            if (value is null || Settings.SelectedProfileId == value.Id)
                return;

            Settings.SelectedProfileId = value.Id;
            OnSelectedProfileChanged();
            _ = RefreshLoaderDataAsync();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
                return;

            _statusText = value;
            OnPropertyChanged();
        }
    }

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

    public string MinecraftConsoleText
    {
        get => _minecraftConsoleText;
        private set
        {
            if (_minecraftConsoleText == value)
                return;

            _minecraftConsoleText = value;
            OnPropertyChanged();
        }
    }

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
    public string GameDirectory => Settings.SharedGameDirectory;

    public string MinecraftVersion
    {
        get => SelectedProfile.MinecraftVersion;
        set
        {
            if (SelectedProfile.MinecraftVersion == value)
                return;

            SelectedProfile.MinecraftVersion = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(InstallationSummary));
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
            {
                MinecraftVersion = value.Name;
                _ = RefreshLoaderVersionsAsync();
            }

            OnPropertyChanged();
        }
    }

    public int MaximumRamMb
    {
        get => SelectedProfile.MaximumRamMb;
        set
        {
            if (SelectedProfile.MaximumRamMb == value)
                return;

            SelectedProfile.MaximumRamMb = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRamLabel));
            OnPropertyChanged(nameof(InstallationSummary));

            if (!_isUpdatingRam)
                SelectClosestRamOption(value);
        }
    }

    public double SelectedRamIndexValue
    {
        get => _selectedRamIndex;
        set
        {
            if (RamOptions.Count == 0)
                return;

            var index = Math.Clamp((int)Math.Round(value), 0, RamOptions.Count - 1);
            if (_selectedRamIndex == index)
                return;

            _selectedRamIndex = index;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRamLabel));

            _isUpdatingRam = true;
            MaximumRamMb = RamOptions[index];
            _isUpdatingRam = false;
        }
    }

    public int MaximumRamIndex => Math.Max(0, RamOptions.Count - 1);
    public string SelectedRamLabel => $"{MaximumRamMb} MB";

    public string SelectedThemeMode
    {
        get => _selectedThemeMode;
        set
        {
            if (_selectedThemeMode == value)
                return;

            _selectedThemeMode = value;
            Settings.ThemeMode = value;
            ApplyTheme(value);
            OnPropertyChanged();
        }
    }

    public string PlayerName
    {
        get => SelectedProfile.PlayerName;
        set
        {
            if (SelectedProfile.PlayerName == value)
                return;

            SelectedProfile.PlayerName = value;
            OnPropertyChanged();
        }
    }

    public LoaderType SelectedLoaderType
    {
        get => SelectedProfile.ModLoader.LoaderType;
        set
        {
            if (SelectedProfile.ModLoader.LoaderType == value)
                return;

            SelectedProfile.ModLoader.LoaderType = value;

            if (value == LoaderType.Vanilla)
                SelectedProfile.ModLoader.LoaderVersion = null;

            OnPropertyChanged();
            OnPropertyChanged(nameof(LoaderVersion));
            OnPropertyChanged(nameof(ModLoaderSummary));
            OnPropertyChanged(nameof(InstallationSummary));
            OnPropertyChanged(nameof(IsVanillaSelected));
            OnPropertyChanged(nameof(IsLoaderSelected));
            _ = RefreshLoaderDataAsync();
        }
    }

    public string LoaderVersion
    {
        get => SelectedProfile.ModLoader.LoaderVersion ?? string.Empty;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (SelectedProfile.ModLoader.LoaderVersion == normalized)
                return;

            SelectedProfile.ModLoader.LoaderVersion = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ModLoaderSummary));
            OnPropertyChanged(nameof(InstallationSummary));
        }
    }

    public string ModLoaderSummary
    {
        get
        {
            if (SelectedLoaderType == LoaderType.Vanilla)
                return "Vanilla";

            return string.IsNullOrWhiteSpace(LoaderVersion)
                ? $"{SelectedLoaderType} sem versão definida"
                : $"{SelectedLoaderType} {LoaderVersion}";
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
            RaiseCommandStates();
        }
    }

    public void ApplySettings(LauncherSettings settings)
    {
        Settings = settings;
        LoadRamOptions();
        SelectedThemeMode = string.IsNullOrWhiteSpace(settings.ThemeMode) ? "Sistema" : settings.ThemeMode;
        SetStatus("Configuração carregada.");

        if (SelectedLoaderType != LoaderType.Vanilla)
            _ = RefreshLoaderDataAsync();
    }

    private void RefreshInstallations()
    {
        Installations.Clear();

        foreach (var profile in Settings.Profiles)
            Installations.Add(profile);
    }

    private void OnSelectedProfileChanged()
    {
        OnPropertyChanged(nameof(SelectedInstallation));
        OnPropertyChanged(nameof(InstallationName));
        OnPropertyChanged(nameof(InstallationDirectory));
        OnPropertyChanged(nameof(InstallationSummary));
        OnPropertyChanged(nameof(GameDirectory));
        OnPropertyChanged(nameof(MinecraftVersion));
        OnPropertyChanged(nameof(MaximumRamMb));
        OnPropertyChanged(nameof(PlayerName));
        OnPropertyChanged(nameof(SelectedLoaderType));
        OnPropertyChanged(nameof(LoaderVersion));
        OnPropertyChanged(nameof(ModLoaderSummary));
        OnPropertyChanged(nameof(IsVanillaSelected));
        OnPropertyChanged(nameof(IsLoaderSelected));
        SelectClosestRamOption(MaximumRamMb);
        ApplyMinecraftVersionFilters();
        RaiseCommandStates();
    }

    private Task NewInstallationAsync()
    {
        var profile = new LauncherProfile
        {
            Name = $"Instancia {Settings.Profiles.Count + 1}",
            PlayerName = PlayerName,
            MinecraftVersion = string.IsNullOrWhiteSpace(MinecraftVersion) ? "latest-release" : MinecraftVersion,
            MaximumRamMb = MaximumRamMb
        };

        Settings.Profiles.Add(profile);
        Settings.SelectedProfileId = profile.Id;
        RefreshInstallations();
        OnSelectedProfileChanged();
        SetStatus($"Instalacao criada: {profile.Name}.");
        AddLog("Info", "Instalacao criada.", profile.Name);
        return Task.CompletedTask;
    }

    private Task DuplicateInstallationAsync()
    {
        var profile = SelectedProfile.Clone($"{SelectedProfile.Name} copia");
        Settings.Profiles.Add(profile);
        Settings.SelectedProfileId = profile.Id;
        RefreshInstallations();
        OnSelectedProfileChanged();
        SetStatus($"Instalacao duplicada: {profile.Name}.");
        AddLog("Info", "Instalacao duplicada.", profile.Name);
        return Task.CompletedTask;
    }

    private Task DeleteInstallationAsync()
    {
        if (Settings.Profiles.Count <= 1)
        {
            SetStatus("Mantem pelo menos uma instalacao.");
            return Task.CompletedTask;
        }

        var profile = SelectedProfile;
        Settings.Profiles.Remove(profile);
        Settings.SelectedProfileId = Settings.Profiles[0].Id;
        RefreshInstallations();
        OnSelectedProfileChanged();
        SetStatus($"Instalacao removida: {profile.Name}.");
        AddLog("Info", "Instalacao removida.", profile.Name);
        return Task.CompletedTask;
    }

    public void ApplyVersions(IEnumerable<MinecraftVersionItem> versions)
    {
        _allVersions.Clear();
        _allVersions.AddRange(versions);

        ApplyMinecraftVersionFilters();

        SetStatus(FilteredVersions.Count > 0
            ? $"{FilteredVersions.Count} versões carregadas."
            : "Nenhuma versão encontrada.");
    }

private async Task RefreshLoaderDataAsync()
{
    if (SelectedLoaderType == LoaderType.Vanilla)
    {
        LoaderVersions.Clear();
        LoaderVersion = string.Empty;
        _supportedMinecraftVersions = new HashSet<string>();
        ApplyMinecraftVersionFilters();
        return;
    }

    try
    {
        SetStatus($"A carregar versões compatíveis com {SelectedLoaderType}...");
        _supportedMinecraftVersions =
            await _modLoaderVersionService.GetSupportedMinecraftVersionsAsync(SelectedLoaderType);

        ApplyMinecraftVersionFilters();
        await RefreshLoaderVersionsAsync();
    }
    catch (Exception ex)
    {
        HandleError($"Carregar versões {SelectedLoaderType}", ex);
    }
}

    private async Task RefreshLoaderVersionsAsync()
    {
        var currentLoaderVersion = LoaderVersion;
        LoaderVersions.Clear();

        if (SelectedLoaderType == LoaderType.Vanilla || string.IsNullOrWhiteSpace(MinecraftVersion))
        {
            LoaderVersion = string.Empty;
            return;
        }

        try
        {
            var versions = await _modLoaderVersionService.GetLoaderVersionsAsync(
                SelectedLoaderType,
                MinecraftVersion);

            foreach (var version in versions)
                LoaderVersions.Add(version);

            LoaderVersion = LoaderVersions.Contains(currentLoaderVersion)
                ? currentLoaderVersion
                : LoaderVersions.FirstOrDefault() ?? string.Empty;

            SetStatus(LoaderVersions.Count > 0
                ? $"{LoaderVersions.Count} versões de {SelectedLoaderType} encontradas."
                : $"Sem versões de {SelectedLoaderType} para Minecraft {MinecraftVersion}.");
        }
        catch (Exception ex)
        {
            HandleError($"Carregar versões {SelectedLoaderType}", ex);
        }
    }

    private void ApplyMinecraftVersionFilters()
    {
        FilteredVersions.Clear();

        IEnumerable<MinecraftVersionItem> query = _allVersions;

        if (SelectedLoaderType == LoaderType.Vanilla)
        {
            query = query.Where(version =>
                (ShowReleases && IsVersionType(version, "release") && IsReleaseMinecraftVersion(version.Name)) ||
                (ShowSnapshots && IsVersionType(version, "snapshot")) ||
                (ShowOldBeta && IsVersionType(version, "old_beta")) ||
                (ShowOldAlpha && IsVersionType(version, "old_alpha")));
        }
        else
        {
            query = query.Where(version =>
                IsVersionType(version, "release") &&
                IsReleaseMinecraftVersion(version.Name) &&
                _supportedMinecraftVersions.Contains(version.Name));
        }

        foreach (var version in query)
            FilteredVersions.Add(version);

        SelectedVersion = FilteredVersions.FirstOrDefault(x => x.Name == MinecraftVersion)
            ?? FilteredVersions.FirstOrDefault();
    }

    private static bool IsVersionType(MinecraftVersionItem version, string type)
    {
        return string.Equals(version.Type, type, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReleaseMinecraftVersion(string version)
    {
        return version.Length >= 3 &&
               version.StartsWith("1.", StringComparison.Ordinal) &&
               version[2..].All(c => c == '.' || char.IsDigit(c));
    }

    public void HandleStartupError(Exception ex)
    {
        HandleError("Arranque", ex);
    }

    private async Task LoginMicrosoftAsync()
    {
        IsBusy = true;
        SetStatus("A abrir login Microsoft por código...");
        AddLog("Info", "Login Microsoft iniciado.");

        try
        {
            _microsoftSession = await _authService.LoginWithDeviceCodeAsync(message =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SetStatus(message);
                    AddLog("Info", "Código Microsoft recebido.", message);
                });

                return Task.CompletedTask;
            });

            AccountText = $"Sessão: {_microsoftSession.Username}";
            PlayerName = _microsoftSession.Username ?? "Player";
            SetStatus("Login Microsoft concluído.");

            AddLog(
                "Info",
                $"Sessão iniciada como {_microsoftSession.Username}",
                $"Username: {_microsoftSession.Username}{Environment.NewLine}UUID: {_microsoftSession.UUID}");
        }
        catch (Exception ex)
        {
            HandleError("Login Microsoft", ex);
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    private async Task PlayOfflineAsync()
    {
        await LaunchAsync("Offline", null);
    }

    private async Task PlayOnlineAsync()
    {
        if (_microsoftSession is null)
        {
            SetStatus("Faz login com a Microsoft antes de jogar online.");
            AddLog("Aviso", "Arranque online bloqueado.", "Não existe sessão Microsoft ativa.");
            return;
        }

        await LaunchAsync("Online", _microsoftSession);
    }

    private async Task LaunchAsync(string launchMode, MSession? session)
    {
        IsBusy = true;
        IsProgressVisible = true;
        FileProgressPercent = 0;
        ByteProgressPercent = 0;
        MinecraftConsoleText = string.Empty;
        SetStatus($"A preparar arranque {launchMode.ToLowerInvariant()}...");

        try
        {
            if (SelectedLoaderType != LoaderType.Vanilla && string.IsNullOrWhiteSpace(LoaderVersion))
            {
                SetStatus($"Define a versão do {SelectedLoaderType} antes de iniciar.");
                AddLog("Aviso", "Loader sem versão.", $"Loader selecionado: {SelectedLoaderType}");
                return;
            }

            var request = _launcherService.CreateLaunchRequest(Settings);
            AttachProgress(request);
            AttachProcessMonitoring(request, launchMode);

            if (session is not null)
            {
                request.UseOfflineMode = false;
                request.Session = session;
                request.PlayerName = session.Username ?? "Player";
            }

            SetStatus($"A verificar Java para {request.VersionName}...");
            AddLog("Info", $"Arranque {launchMode} iniciado.", $"Versão: {request.VersionName}");

            var result = await _launcherService.LaunchOfflineAsync(request);

            if (!result.Success)
            {
                SetStatus($"{launchMode} falhou: {result.Message}");
                AddLog(
                    "Erro",
                    $"{launchMode} falhou.",
                    result.Exception is null ? result.Message : BuildExceptionDetails(result.Exception));
                return;
            }

            SetStatus($"Minecraft iniciado. Processo: {result.ProcessId}");
            AddLog("Info", $"{launchMode} iniciado.", $"Process ID: {result.ProcessId}");
        }
        catch (Exception ex)
        {
            HandleError($"Arranque {launchMode}", ex);
        }
        finally
        {
            IsBusy = false;
            IsProgressVisible = false;
        }
    }

    private Task OpenGameFolderAsync()
    {
        Directory.CreateDirectory(GameDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = GameDirectory,
            UseShellExecute = true
        });

        AddLog("Info", "Pasta do jogo aberta.", GameDirectory);
        return Task.CompletedTask;
    }

    private Task OpenInstallationFolderAsync()
    {
        Directory.CreateDirectory(InstallationDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = InstallationDirectory,
            UseShellExecute = true
        });

        AddLog("Info", "Pasta da instalacao aberta.", InstallationDirectory);
        return Task.CompletedTask;
    }

    private void AttachProgress(LaunchRequest request)
    {
        request.ProgressChanged = progress =>
        {
            var now = DateTimeOffset.UtcNow;
            var isComplete = progress.FileProgressPercent >= 100 || progress.ByteProgressPercent >= 100;
            if (!isComplete && now - _lastProgressUiUpdate < ProgressUpdateInterval)
                return;

            _lastProgressUiUpdate = now;

            Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrWhiteSpace(progress.StatusText))
                    SetStatus(progress.StatusText);

                if (progress.FileProgressPercent > 0)
                    FileProgressPercent = progress.FileProgressPercent;

                if (progress.ByteProgressPercent > 0)
                    ByteProgressPercent = progress.ByteProgressPercent;
            });
        };
    }

    private void AttachProcessMonitoring(LaunchRequest request, string launchMode)
    {
        request.ProcessLogReceived = message =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                AppendMinecraftConsole(message);
            });
        };

        request.ProcessExited = result =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                SetStatus(result.Message);

                AddLog(
                    result.Crashed ? "Erro" : "Info",
                    $"Processo Minecraft {launchMode} terminou.",
                    $"Exit code: {result.ExitCode}{Environment.NewLine}{result.Message}");
            });
        };
    }

    private void LoadRamOptions()
    {
        RamOptions.Clear();

        var availableMb = GetAvailableMemoryMb();
        for (var ram = 1024; ram <= availableMb; ram *= 2)
            RamOptions.Add(ram);

        if (RamOptions.Count == 0)
            RamOptions.Add(1024);

        OnPropertyChanged(nameof(MaximumRamIndex));
        SelectClosestRamOption(MaximumRamMb);
    }

    private void SelectClosestRamOption(int targetRamMb)
    {
        if (RamOptions.Count == 0)
            return;

        var closest = RamOptions
            .Select((ram, index) => new { ram, index })
            .OrderBy(x => Math.Abs(x.ram - targetRamMb))
            .First();

        _selectedRamIndex = closest.index;
        _isUpdatingRam = true;
        MaximumRamMb = closest.ram;
        _isUpdatingRam = false;

        OnPropertyChanged(nameof(SelectedRamIndexValue));
        OnPropertyChanged(nameof(SelectedRamLabel));
        OnPropertyChanged(nameof(InstallationSummary));
    }

    private static int GetAvailableMemoryMb()
    {
        var bytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var mb = bytes <= 0 ? 8192 : (int)(bytes / 1024 / 1024);
        return Math.Clamp(mb, 1024, 65536);
    }

    private static void ApplyTheme(string theme)
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeVariant = theme switch
        {
            "Claro" => ThemeVariant.Light,
            "Escuro" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void SetStatus(string message)
    {
        var previousStatus = StatusText;
        StatusText = message;

        if (previousStatus == message)
            return;

        AppendMinecraftConsole($"[Launcher] {message}");
    }

    private void AppendMinecraftConsole(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var text = MinecraftConsoleText + line + Environment.NewLine;
        if (text.Length > MaxConsoleTextLength)
            text = text[^MaxConsoleTextLength..];

        MinecraftConsoleText = text;
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

        while (Logs.Count > MaxLogEntries)
            Logs.RemoveAt(Logs.Count - 1);
    }

    private void HandleError(string operation, Exception ex)
    {
        var message = ErrorMessageService.ToUserMessage(ex);
        SetStatus($"{operation} falhou: {message}");
        AddLog("Erro", $"{operation} falhou.", BuildExceptionDetails(ex));
    }

    private static string BuildExceptionDetails(Exception ex)
    {
        var builder = new StringBuilder();

        if (ex is AggregateException aggregateException)
        {
            builder.AppendLine("AggregateException");
            builder.AppendLine(aggregateException.Message);
            builder.AppendLine();

            foreach (var inner in aggregateException.Flatten().InnerExceptions)
                AppendException(builder, inner);

            return builder.ToString();
        }

        AppendException(builder, ex);
        return builder.ToString();
    }

    private static void AppendException(StringBuilder builder, Exception ex)
    {
        var current = ex;
        var depth = 1;

        while (current is not null)
        {
            builder.AppendLine($"Exception #{depth}");
            builder.AppendLine($"Type: {current.GetType().FullName}");
            builder.AppendLine($"Message: {current.Message}");
            builder.AppendLine("StackTrace:");
            builder.AppendLine(current.StackTrace);
            builder.AppendLine();

            current = current.InnerException;
            depth++;
        }
    }

    private void RaiseCommandStates()
    {
        if (PlayOfflineCommand is AsyncRelayCommand offlineCommand)
            offlineCommand.RaiseCanExecuteChanged();

        if (LoginMicrosoftCommand is AsyncRelayCommand loginCommand)
            loginCommand.RaiseCanExecuteChanged();

        if (PlayOnlineCommand is AsyncRelayCommand onlineCommand)
            onlineCommand.RaiseCanExecuteChanged();

        if (OpenInstallationFolderCommand is AsyncRelayCommand openInstallationFolderCommand)
            openInstallationFolderCommand.RaiseCanExecuteChanged();

        if (NewInstallationCommand is AsyncRelayCommand newInstallationCommand)
            newInstallationCommand.RaiseCanExecuteChanged();

        if (DuplicateInstallationCommand is AsyncRelayCommand duplicateInstallationCommand)
            duplicateInstallationCommand.RaiseCanExecuteChanged();

        if (DeleteInstallationCommand is AsyncRelayCommand deleteInstallationCommand)
            deleteInstallationCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

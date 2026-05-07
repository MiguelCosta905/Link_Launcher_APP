using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using LinkLauncher.App.Localization;
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
    private string _statusText = UiText.Get("pt-PT", "status_ready");
    private string _accountText = UiText.Get("pt-PT", "account_no_session");
    private string _minecraftConsoleText = string.Empty;
    private string _selectedThemeKey = "System";
    private string _selectedLanguageCode = "pt-PT";
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
    private LauncherSection _currentSection = LauncherSection.Library;
    public HomeViewModel HomePage { get; }
    public SkinsViewModel SkinsPage { get; }
    public LibraryViewModel LibraryPage { get; }
    public CreateViewModel CreatePage { get; }
    private string? _pendingDeleteProfileId;
    public MainWindowViewModel(
        LauncherService launcherService,
        MicrosoftAuthService authService)
    {
        _launcherService = launcherService;
        _authService = authService;

        
        LoginMicrosoftCommand = new AsyncRelayCommand(LoginMicrosoftAsync, () => !IsBusy);
        OpenGameFolderCommand = new AsyncRelayCommand(OpenGameFolderAsync);
        NewInstallationCommand = new AsyncRelayCommand(NewInstallationAsync, () => !IsBusy);
        ShowHomeCommand = new RelayCommand(() => SetSection(LauncherSection.Home));
        ShowSkinsCommand = new RelayCommand(() => SetSection(LauncherSection.Skins));
        ShowLibraryCommand = new RelayCommand(() => SetSection(LauncherSection.Library));
        ShowCreateCommand = new RelayCommand(() => SetSection(LauncherSection.Create));
        HomePage = new HomeViewModel(this);
        SkinsPage = new SkinsViewModel(this);
        LibraryPage = new LibraryViewModel(this);
        CreatePage = new CreateViewModel(this);
        PlayOfflineCommand = new AsyncRelayCommand(PlayOfflineAsync, () => !IsBusy && HasSelectedInstallation);
        PlayOnlineCommand = new AsyncRelayCommand(PlayOnlineAsync, () => !IsBusy && HasSelectedInstallation && _microsoftSession is not null);
        OpenInstallationFolderCommand = new AsyncRelayCommand(OpenInstallationFolderAsync, () => HasSelectedInstallation);
        DuplicateInstallationCommand = new AsyncRelayCommand(DuplicateInstallationAsync, () => !IsBusy && HasSelectedInstallation);
        DeleteInstallationCommand = new AsyncRelayCommand(DeleteInstallationAsync, () => !IsBusy && HasSelectedInstallation);

        RebuildLanguageOptions();
        RebuildThemeOptions();
        LoadRamOptions();
        ApplyTheme(_selectedThemeKey);
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
    public ICommand ShowHomeCommand { get; }
    public ICommand ShowSkinsCommand { get; }
    public ICommand ShowLibraryCommand { get; }
    public ICommand ShowCreateCommand { get; }

    public ObservableCollection<MinecraftVersionItem> Versions { get; } = new();
    public ObservableCollection<AppLogEntry> Logs { get; } = new();
    public ObservableCollection<int> RamOptions { get; } = new();
    public ObservableCollection<UiOption> ThemeOptions { get; } = new();
    public ObservableCollection<UiOption> LanguageOptions { get; } = new();
    public ObservableCollection<LoaderType> LoaderTypes { get; } = new(Enum.GetValues<LoaderType>());
    public ObservableCollection<MinecraftVersionItem> FilteredVersions { get; } = new();
    public ObservableCollection<string> LoaderVersions { get; } = new();
    public ObservableCollection<LauncherProfile> Installations { get; } = new();

    public LauncherSection CurrentSection
    {
        get => _currentSection;
        private set
        {
            if (_currentSection == value)
                return;

            _currentSection = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsHomeSelected));
            OnPropertyChanged(nameof(IsSkinsSelected));
            OnPropertyChanged(nameof(IsLibrarySelected));
            OnPropertyChanged(nameof(IsCreateSelected));
        }
    }

    public string TaglineText => T("tagline");
    public string ThemeLabelText => T("theme_label");
    public string LanguageLabelText => T("language_label");
    public string AccountTitleText => T("account_title");
    public string PlayerLabelText => TF("player_label", PlayerName);
    public string LoginMicrosoftButtonText => T("login_microsoft");
    public string PlayOnlineButtonText => T("play_online");
    public string PlayOfflineButtonText => T("play_offline");
    public string InstallationsTitleText => T("installations_title");
    public string NewInstallationButtonText => T("new_installation");
    public string CopyInstallationButtonText => T("copy_installation");
    public string DeleteInstallationButtonText => T("delete_installation");
    public string CurrentDeleteInstallationButtonText =>
        SelectedProfile is { } profile && _pendingDeleteProfileId == profile.Id ? "Confirm Delete" : DeleteInstallationButtonText;
    public string OpenInstallationFolderButtonText => T("open_installation_folder");
    public string GameFolderTitleText => T("game_folder_title");
    public string OpenGameFolderButtonText => T("open_game_folder");
    public string CurrentInstallationTitleText => T("current_installation");
    public string NameLabelText => T("name_label");
    public string MinecraftLabelText => T("minecraft_label");
    public string InstanceRamLabelText => T("instance_ram_label");
    public string ReleasesText => T("releases");
    public string SnapshotsText => T("snapshots");
    public string OldBetaText => T("old_beta");
    public string OldAlphaText => T("old_alpha");
    public string LoaderLabelText => T("loader_label");
    public string LoaderVersionLabelText => T("loader_version_label");
    public string VanillaLoaderMessageText => T("vanilla_loader_message");
    public string ProgressTitleText => T("progress_title");
    public string FilesLabelText => T("files_label");
    public string DownloadLabelText => T("download_label");
    public string MissionControlTitleText => T("mission_control_title");
    public string EventsTitleText => T("events_title");
    public string EventsSubtitleText => T("events_subtitle");
    public bool IsHomeSelected => CurrentSection == LauncherSection.Home;
    public bool IsSkinsSelected => CurrentSection == LauncherSection.Skins;
    public bool IsLibrarySelected => CurrentSection == LauncherSection.Library;
    public bool IsCreateSelected => CurrentSection == LauncherSection.Create;

    public UiOption? SelectedThemeOption
    {
        get => ThemeOptions.FirstOrDefault(option => option.Key == _selectedThemeKey);
        set
        {
            if (value is null)
                return;

            SetTheme(value.Key);
        }
    }

    public UiOption? SelectedLanguageOption
    {
        get => LanguageOptions.FirstOrDefault(option => option.Key == _selectedLanguageCode);
        set
        {
            if (value is null)
                return;

            SetLanguage(value.Key, announceChange: true);
        }
    }

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
            OnPropertyChanged(nameof(PlayerLabelText));
            OnPropertyChanged(nameof(SelectedLoaderType));
            OnPropertyChanged(nameof(LoaderVersion));
            OnPropertyChanged(nameof(ModLoaderSummary));
            OnPropertyChanged(nameof(IsVanillaSelected));
            OnPropertyChanged(nameof(IsLoaderSelected));
        }
    }

    public LauncherProfile? SelectedProfile => Settings.GetSelectedProfile();
    public bool HasSelectedInstallation => SelectedProfile is not null;
    public bool HasNoInstallation => !HasSelectedInstallation;
    public string InstallationName
    {
        get => SelectedProfile?.Name ?? string.Empty;
        set
        {
            if (SelectedProfile is null)
                return;

            var normalized = string.IsNullOrWhiteSpace(value) ? T("instance_default_name") : value.Trim();
            if (SelectedProfile.Name == normalized)
                return;

            SelectedProfile.Name = normalized;
            RefreshInstallations();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedInstallation));
            OnPropertyChanged(nameof(InstallationSummary));
            OnPropertyChanged(nameof(InstallationDirectory));
        }
    }
    public string InstallationDirectory =>
    SelectedProfile is null ? string.Empty : Settings.GetInstanceDirectory(SelectedProfile);

    public string InstallationSummary =>
    SelectedProfile is null
        ? "No installations available"
        : $"{InstallationName} - {MinecraftVersion} - {SelectedRamLabel} - {ModLoaderSummary}";

    public LauncherProfile? SelectedInstallation
    {
        get => SelectedProfile;
        set
        {
            if (value is null)
            {
                if (Settings.SelectedProfileId is null)
                    return;

                Settings.SelectedProfileId = null;
                OnSelectedProfileChanged();
                return;
            }

            if (Settings.SelectedProfileId == value.Id)
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
        get => SelectedProfile?.MinecraftVersion ?? string.Empty;
        set
        {
            if (SelectedProfile is null || SelectedProfile.MinecraftVersion == value)
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
        get => SelectedProfile?.MaximumRamMb ?? 2048;
        set
        {
            if (SelectedProfile is null || SelectedProfile.MaximumRamMb == value)
                return;

            SelectedProfile.MaximumRamMb = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRamLabel));
            OnPropertyChanged(nameof(InstallationSummary));

            if (!_isUpdatingRam)
                SelectClosestRamOption(value);
        }
    }

    public int? SelectedRamOption
    {
        get => MaximumRamMb;
        set
        {
            if (!value.HasValue)
                return;

            MaximumRamMb = value.Value;
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

    public string PlayerName
    {
        get => SelectedProfile?.PlayerName ?? "Player";
        set
        {
            if (SelectedProfile is null || SelectedProfile.PlayerName == value)
                return;

            SelectedProfile.PlayerName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlayerLabelText));
        }
    }

    public LoaderType SelectedLoaderType
    {
        get => SelectedProfile?.ModLoader.LoaderType ?? LoaderType.Vanilla;
        set
        {
            if (SelectedProfile is null || SelectedProfile.ModLoader.LoaderType == value)
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
        get => SelectedProfile?.ModLoader.LoaderVersion ?? string.Empty;
        set
        {
            if (SelectedProfile is null)
                return;

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
                return T("modloader_vanilla");

            return string.IsNullOrWhiteSpace(LoaderVersion)
                ? TF("modloader_no_version", SelectedLoaderType)
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
        SetLanguage(settings.LanguageCode, announceChange: false);
        SetTheme(settings.ThemeMode);
        UpdateAccountText();
        SetStatus(T("status_settings_loaded"));

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
        ClearPendingDelete();
        OnPropertyChanged(nameof(SelectedInstallation));
        OnPropertyChanged(nameof(InstallationName));
        OnPropertyChanged(nameof(InstallationDirectory));
        OnPropertyChanged(nameof(InstallationSummary));
        OnPropertyChanged(nameof(GameDirectory));
        OnPropertyChanged(nameof(MinecraftVersion));
        OnPropertyChanged(nameof(MaximumRamMb));
        OnPropertyChanged(nameof(SelectedRamOption));
        OnPropertyChanged(nameof(PlayerName));
        OnPropertyChanged(nameof(PlayerLabelText));
        OnPropertyChanged(nameof(SelectedLoaderType));
        OnPropertyChanged(nameof(LoaderVersion));
        OnPropertyChanged(nameof(ModLoaderSummary));
        OnPropertyChanged(nameof(IsVanillaSelected));
        OnPropertyChanged(nameof(IsLoaderSelected));
        SelectClosestRamOption(MaximumRamMb);
        ApplyMinecraftVersionFilters();
        RaiseCommandStates();
    }
    public void CreateInstallationFromDraft(string name, string minecraftVersion, LoaderType loaderType, string? coverImagePath = null)
    {
        var profile = new LauncherProfile
        {
            Name = string.IsNullOrWhiteSpace(name) ? TF("instance_numbered", Settings.Profiles.Count + 1) : name.Trim(),
            PlayerName = PlayerName,
            MinecraftVersion = string.IsNullOrWhiteSpace(minecraftVersion) ? "latest-release" : minecraftVersion,
            MaximumRamMb = MaximumRamMb,
            CoverImagePath = string.IsNullOrWhiteSpace(coverImagePath)
                ? "avares://LinkLauncher.App/Assets/logo.png"
                : coverImagePath
        };

        profile.ModLoader.LoaderType = loaderType;

        if (loaderType == LoaderType.Vanilla)
            profile.ModLoader.LoaderVersion = null;

        Settings.Profiles.Add(profile);
        Settings.SelectedProfileId = profile.Id;
        RefreshInstallations();
        OnSelectedProfileChanged();
        _ = RefreshLoaderDataAsync();
        SetStatus(TF("status_installation_created", profile.Name));
        AddLog("Info", T("log_installation_created"), profile.Name);
    }

    private Task NewInstallationAsync()
    {
        var profile = new LauncherProfile
        {
            Name = TF("instance_numbered", Settings.Profiles.Count + 1),
            PlayerName = PlayerName,
            MinecraftVersion = string.IsNullOrWhiteSpace(MinecraftVersion) ? "latest-release" : MinecraftVersion,
            MaximumRamMb = MaximumRamMb
        };

        Settings.Profiles.Add(profile);
        Settings.SelectedProfileId = profile.Id;
        RefreshInstallations();
        OnSelectedProfileChanged();
        SetStatus(TF("status_installation_created", profile.Name));
        AddLog("Info", T("log_installation_created"), profile.Name);
        return Task.CompletedTask;
    }

    private Task DuplicateInstallationAsync()
    {
        if (SelectedProfile is null)
        return Task.CompletedTask;

        var profile = SelectedProfile.Clone($"{SelectedProfile.Name} {T("copy_suffix")}");
        Settings.Profiles.Add(profile);
        Settings.SelectedProfileId = profile.Id;
        RefreshInstallations();
        OnSelectedProfileChanged();
        SetStatus(TF("status_installation_copied", profile.Name));
        AddLog("Info", T("log_installation_copied"), profile.Name);
        return Task.CompletedTask;
    }

    private Task DeleteInstallationAsync()
    {
        if (SelectedProfile is null)
            return Task.CompletedTask;

        var profile = SelectedProfile;

        if (_pendingDeleteProfileId != profile.Id)
        {
            _pendingDeleteProfileId = profile.Id;
            OnPropertyChanged(nameof(CurrentDeleteInstallationButtonText));
            SetStatus($"Confirm delete for {profile.Name}");
            return Task.CompletedTask;
        }

        ClearPendingDelete();
        Settings.Profiles.Remove(profile);
        Settings.SelectedProfileId = Settings.Profiles.Count == 0 ? null : Settings.Profiles[0].Id;

        RefreshInstallations();
        OnSelectedProfileChanged();
        SetStatus(TF("status_installation_removed", profile.Name));
        AddLog("Info", T("log_installation_removed"), profile.Name);
        return Task.CompletedTask;
    }
    public void ApplyVersions(IEnumerable<MinecraftVersionItem> versions)
    {
        _allVersions.Clear();
        _allVersions.AddRange(versions);

        ApplyMinecraftVersionFilters();

        SetStatus(FilteredVersions.Count > 0
            ? TF("status_versions_loaded", FilteredVersions.Count)
            : T("status_no_versions_found"));
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
            SetStatus(TF("status_loading_loader_versions", SelectedLoaderType));
            _supportedMinecraftVersions =
                await _modLoaderVersionService.GetSupportedMinecraftVersionsAsync(SelectedLoaderType);

            ApplyMinecraftVersionFilters();
            await RefreshLoaderVersionsAsync();
        }
        catch (Exception ex)
        {
            HandleError($"Load {SelectedLoaderType} versions", ex);
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
                ? TF("status_loader_versions_found", LoaderVersions.Count, SelectedLoaderType)
                : TF("status_no_loader_versions_found", SelectedLoaderType, MinecraftVersion));
        }
        catch (Exception ex)
        {
            HandleError($"Load {SelectedLoaderType} versions", ex);
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
        HandleError(T("operation_startup"), ex);
    }

    private async Task LoginMicrosoftAsync()
    {
        IsBusy = true;
        SetStatus(T("status_opening_microsoft_login"));
        AddLog("Info", T("log_microsoft_login_started"));

        try
        {
            _microsoftSession = await _authService.LoginWithDeviceCodeAsync(message =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SetStatus(message);
                    AddLog("Info", T("log_microsoft_code_received"), message);
                });

                return Task.CompletedTask;
            });

            PlayerName = _microsoftSession.Username ?? "Player";
            UpdateAccountText();
            SetStatus(T("status_microsoft_login_done"));

            AddLog(
                "Info",
                TF("log_signed_in_as", _microsoftSession.Username ?? "Player"),
                $"Username: {_microsoftSession.Username}{Environment.NewLine}UUID: {_microsoftSession.UUID}");
        }
        catch (Exception ex)
        {
            HandleError("Microsoft login", ex);
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    private async Task PlayOfflineAsync()
    {
        if (!HasSelectedInstallation)
        {
            SetStatus("No installation selected.");
            return;
        }

        await LaunchAsync("Offline", null);
    }

    private async Task PlayOnlineAsync()
    {
        if (!HasSelectedInstallation)
        {
            SetStatus("No installation selected.");
            return;
        }

        if (_microsoftSession is null)
        {
            SetStatus(T("warning_login_before_online"));
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
        SetStatus(TF("status_preparing_launch", launchMode.ToLowerInvariant()));

        try
        {
            if (SelectedLoaderType != LoaderType.Vanilla && string.IsNullOrWhiteSpace(LoaderVersion))
            {
                SetStatus(TF("warning_set_loader_version", SelectedLoaderType));
                AddLog("Warning", T("warning_loader_without_version"), $"Loader selected: {SelectedLoaderType}");
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

            SetStatus(TF("status_checking_java", request.VersionName));
            AddLog("Info", TF("log_launch_started", launchMode), $"Version: {request.VersionName}");

            var result = await _launcherService.LaunchOfflineAsync(request);

            if (!result.Success)
            {
                SetStatus(TF("status_launch_failed", launchMode, result.Message));
                AddLog(
                    "Error",
                    TF("log_launch_failed", launchMode),
                    result.Exception is null ? result.Message : BuildExceptionDetails(result.Exception));
                return;
            }

            var processIdText = result.ProcessId?.ToString(CultureInfo.InvariantCulture) ?? "-";
            SetStatus(TF("status_minecraft_started", processIdText));
            AddLog("Info", TF("log_launch_success", launchMode), $"Process ID: {processIdText}");
        }
        catch (Exception ex)
        {
            HandleError($"{launchMode} launch", ex);
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

        AddLog("Info", T("log_game_folder_opened"), GameDirectory);
        return Task.CompletedTask;
    }

    private Task OpenInstallationFolderAsync()
    {
        if (!HasSelectedInstallation)
        return Task.CompletedTask;

        Directory.CreateDirectory(InstallationDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = InstallationDirectory,
            UseShellExecute = true
        });

        AddLog("Info", T("log_installation_folder_opened"), InstallationDirectory);
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
                    result.Crashed ? "Error" : "Info",
                    TF("log_process_ended", launchMode),
                    $"Exit code: {result.ExitCode}{Environment.NewLine}{result.Message}");
            });
        };
    }

    private void LoadRamOptions()
    {
        RamOptions.Clear();

        for (var ram = 1024; ram <= 16384; ram += 1024)
            RamOptions.Add(ram);

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
        OnPropertyChanged(nameof(SelectedRamOption));
        OnPropertyChanged(nameof(InstallationSummary));
    }

    private static void ApplyTheme(string themeKey)
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeVariant = themeKey switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void SetTheme(string themeKey)
    {
        var normalized = themeKey switch
        {
            "Light" => "Light",
            "Dark" => "Dark",
            _ => "System"
        };

        if (_selectedThemeKey == normalized && ThemeOptions.Count > 0)
            return;

        _selectedThemeKey = normalized;
        Settings.ThemeMode = normalized;
        ApplyTheme(normalized);
        RebuildThemeOptions();
        OnPropertyChanged(nameof(SelectedThemeOption));
    }

    private void SetLanguage(string languageCode, bool announceChange)
    {
        var normalized = UiText.NormalizeLanguageCode(languageCode);
        if (_selectedLanguageCode == normalized && LanguageOptions.Count > 0)
            return;

        _selectedLanguageCode = normalized;
        Settings.LanguageCode = normalized;
        RebuildLanguageOptions();
        RebuildThemeOptions();
        RefreshLocalizedProperties();
        UpdateAccountText();

        if (announceChange)
            SetStatus(T("status_language_changed"));
    }

    private void RebuildThemeOptions()
    {
        ThemeOptions.Clear();
        ThemeOptions.Add(new UiOption("System", T("theme_system")));
        ThemeOptions.Add(new UiOption("Light", T("theme_light")));
        ThemeOptions.Add(new UiOption("Dark", T("theme_dark")));
    }

    private void RebuildLanguageOptions()
    {
        LanguageOptions.Clear();
        LanguageOptions.Add(new UiOption("en", T("language_english")));
        LanguageOptions.Add(new UiOption("pt-PT", T("language_portuguese")));
    }

    private void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(TaglineText));
        OnPropertyChanged(nameof(ThemeLabelText));
        OnPropertyChanged(nameof(LanguageLabelText));
        OnPropertyChanged(nameof(AccountTitleText));
        OnPropertyChanged(nameof(PlayerLabelText));
        OnPropertyChanged(nameof(LoginMicrosoftButtonText));
        OnPropertyChanged(nameof(PlayOnlineButtonText));
        OnPropertyChanged(nameof(PlayOfflineButtonText));
        OnPropertyChanged(nameof(InstallationsTitleText));
        OnPropertyChanged(nameof(NewInstallationButtonText));
        OnPropertyChanged(nameof(CopyInstallationButtonText));
        OnPropertyChanged(nameof(DeleteInstallationButtonText));
        OnPropertyChanged(nameof(OpenInstallationFolderButtonText));
        OnPropertyChanged(nameof(GameFolderTitleText));
        OnPropertyChanged(nameof(OpenGameFolderButtonText));
        OnPropertyChanged(nameof(CurrentInstallationTitleText));
        OnPropertyChanged(nameof(NameLabelText));
        OnPropertyChanged(nameof(MinecraftLabelText));
        OnPropertyChanged(nameof(InstanceRamLabelText));
        OnPropertyChanged(nameof(ReleasesText));
        OnPropertyChanged(nameof(SnapshotsText));
        OnPropertyChanged(nameof(OldBetaText));
        OnPropertyChanged(nameof(OldAlphaText));
        OnPropertyChanged(nameof(LoaderLabelText));
        OnPropertyChanged(nameof(LoaderVersionLabelText));
        OnPropertyChanged(nameof(VanillaLoaderMessageText));
        OnPropertyChanged(nameof(ProgressTitleText));
        OnPropertyChanged(nameof(FilesLabelText));
        OnPropertyChanged(nameof(DownloadLabelText));
        OnPropertyChanged(nameof(MissionControlTitleText));
        OnPropertyChanged(nameof(EventsTitleText));
        OnPropertyChanged(nameof(EventsSubtitleText));
        OnPropertyChanged(nameof(SelectedLanguageOption));
        OnPropertyChanged(nameof(SelectedThemeOption));
        OnPropertyChanged(nameof(ModLoaderSummary));
        OnPropertyChanged(nameof(InstallationSummary));
    }

    private void UpdateAccountText()
    {
        AccountText = _microsoftSession is null
            ? T("account_no_session")
            : TF("account_session", _microsoftSession.Username ?? "Player");
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
        SetStatus(TF("status_operation_failed", operation, message));
        AddLog("Error", TF("log_operation_failed", operation), BuildExceptionDetails(ex));
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
    private void ClearPendingDelete()
    {
        if (_pendingDeleteProfileId is null)
            return;

        _pendingDeleteProfileId = null;
        OnPropertyChanged(nameof(CurrentDeleteInstallationButtonText));
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

    private void SetSection(LauncherSection section)
    {
        CurrentSection = section;
    }

    private string T(string key) => UiText.Get(_selectedLanguageCode, key);

    private string TF(string key, params object[] args) => UiText.Format(_selectedLanguageCode, key, args);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

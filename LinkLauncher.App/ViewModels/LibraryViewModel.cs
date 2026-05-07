using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.ModLoaders;

namespace LinkLauncher.App.ViewModels;

public sealed class LibraryViewModel : INotifyPropertyChanged
{
    private bool _isDetailsEditMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LibraryViewModel(MainWindowViewModel app)
    {
        App = app;
        App.PropertyChanged += OnAppPropertyChanged;
        ToggleDetailsEditModeCommand = new RelayCommand(ToggleDetailsEditMode);
    }

    public MainWindowViewModel App { get; }

    public string CurrentInstallationTitleText => App.CurrentInstallationTitleText;
    public string InstallationSummary => App.InstallationSummary;
    public string NameLabelText => App.NameLabelText;
    public string MinecraftVersion => App.MinecraftVersion;
    public string SelectedRamLabel => App.SelectedRamLabel;
    public string ModLoaderSummary => App.ModLoaderSummary;

    public string InstallationName
    {
        get => App.InstallationName;
        set => App.InstallationName = value;
    }

    public string MinecraftLabelText => App.MinecraftLabelText;
    public ObservableCollection<MinecraftVersionItem> FilteredVersions => App.FilteredVersions;

    public MinecraftVersionItem? SelectedVersion
    {
        get => App.SelectedVersion;
        set => App.SelectedVersion = value;
    }

    public string InstanceRamLabelText => App.InstanceRamLabelText;
    public ObservableCollection<int> RamOptions => App.RamOptions;

    public int? SelectedRamOption
    {
        get => App.SelectedRamOption;
        set => App.SelectedRamOption = value;
    }

    public bool IsVanillaSelected => App.IsVanillaSelected;

    public string ReleasesText => App.ReleasesText;
    public bool ShowReleases
    {
        get => App.ShowReleases;
        set => App.ShowReleases = value;
    }

    public string SnapshotsText => App.SnapshotsText;
    public bool ShowSnapshots
    {
        get => App.ShowSnapshots;
        set => App.ShowSnapshots = value;
    }

    public string OldBetaText => App.OldBetaText;
    public bool ShowOldBeta
    {
        get => App.ShowOldBeta;
        set => App.ShowOldBeta = value;
    }

    public string OldAlphaText => App.OldAlphaText;
    public bool ShowOldAlpha
    {
        get => App.ShowOldAlpha;
        set => App.ShowOldAlpha = value;
    }

    public string LoaderLabelText => App.LoaderLabelText;
    public ObservableCollection<LoaderType> LoaderTypes => App.LoaderTypes;

    public LoaderType SelectedLoaderType
    {
        get => App.SelectedLoaderType;
        set => App.SelectedLoaderType = value;
    }

    public bool IsLoaderSelected => App.IsLoaderSelected;
    public string LoaderVersionLabelText => App.LoaderVersionLabelText;
    public ObservableCollection<string> LoaderVersions => App.LoaderVersions;

    public string LoaderVersion
    {
        get => App.LoaderVersion;
        set => App.LoaderVersion = value;
    }

    public bool IsDetailsEditMode
    {
        get => _isDetailsEditMode;
        set
        {
            if (_isDetailsEditMode == value)
                return;

            _isDetailsEditMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDetailsSummaryVisible));
            OnPropertyChanged(nameof(DetailsEditButtonText));
        }
    }

    public bool IsDetailsSummaryVisible => !IsDetailsEditMode;
    public bool CanEditLoaderVersion => HasSelectedInstallation && IsLoaderSelected;
    public string DetailsEditButtonText => IsDetailsEditMode ? "Fechar edicao" : "Editar instancia";
    public ICommand ToggleDetailsEditModeCommand { get; }

    public string VanillaLoaderMessageText => App.VanillaLoaderMessageText;
    public int MaximumRamIndex => App.MaximumRamIndex;

    public double SelectedRamIndexValue
    {
        get => App.SelectedRamIndexValue;
        set => App.SelectedRamIndexValue = value;
    }

    public bool IsProgressVisible => App.IsProgressVisible;
    public string ProgressTitleText => App.ProgressTitleText;
    public string FilesLabelText => App.FilesLabelText;
    public double FileProgressPercent => App.FileProgressPercent;
    public string DownloadLabelText => App.DownloadLabelText;
    public double ByteProgressPercent => App.ByteProgressPercent;

    public string AccountTitleText => App.AccountTitleText;
    public string AccountText => App.AccountText;
    public string PlayerLabelText => App.PlayerLabelText;

    public string LoginMicrosoftButtonText => App.LoginMicrosoftButtonText;
    public ICommand LoginMicrosoftCommand => App.LoginMicrosoftCommand;

    public string PlayOnlineButtonText => App.PlayOnlineButtonText;
    public ICommand PlayOnlineCommand => App.PlayOnlineCommand;

    public string PlayOfflineButtonText => App.PlayOfflineButtonText;
    public ICommand PlayOfflineCommand => App.PlayOfflineCommand;

    public string InstallationsTitleText => App.InstallationsTitleText;
    public ObservableCollection<LauncherProfile> Installations => App.Installations;

    public LauncherProfile? SelectedInstallation
    {
        get => App.SelectedInstallation;
        set => App.SelectedInstallation = value;
    }

    public string NewInstallationButtonText => App.NewInstallationButtonText;
    public ICommand NewInstallationCommand => App.NewInstallationCommand;

    public string CopyInstallationButtonText => App.CopyInstallationButtonText;
    public ICommand DuplicateInstallationCommand => App.DuplicateInstallationCommand;
    public string DeleteInstallationButtonText => App.CurrentDeleteInstallationButtonText;
    public ICommand DeleteInstallationCommand => App.DeleteInstallationCommand;

    public string OpenInstallationFolderButtonText => App.OpenInstallationFolderButtonText;
    public ICommand OpenInstallationFolderCommand => App.OpenInstallationFolderCommand;

    public string GameFolderTitleText => App.GameFolderTitleText;
    public string GameDirectory => App.GameDirectory;
    public string OpenGameFolderButtonText => App.OpenGameFolderButtonText;
    public ICommand OpenGameFolderCommand => App.OpenGameFolderCommand;

    public string MissionControlTitleText => App.MissionControlTitleText;
    public string StatusText => App.StatusText;
    public string MinecraftConsoleText => App.MinecraftConsoleText;

    public string EventsTitleText => App.EventsTitleText;
    public string EventsSubtitleText => App.EventsSubtitleText;
    public ObservableCollection<AppLogEntry> Logs => App.Logs;

    public bool HasSelectedInstallation => App.HasSelectedInstallation;
    public bool HasNoInstallation => App.HasNoInstallation;
    public string EmptyLibraryMessage => "Ainda nao tens instancias. Vai a Criar para comecar.";


    public AppLogEntry? SelectedLogEntry
    {
        get => App.SelectedLogEntry;
        set => App.SelectedLogEntry = value;
    }

    public string SelectedLogDetails => App.SelectedLogDetails;

    private void OnAppPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName == nameof(App.HasSelectedInstallation))
        {
            OnPropertyChanged(nameof(HasNoInstallation));
            OnPropertyChanged(nameof(EmptyLibraryMessage));
            OnPropertyChanged(nameof(CanEditLoaderVersion));

            if (!HasSelectedInstallation)
                IsDetailsEditMode = false;
        }

        if (e.PropertyName == nameof(App.IsLoaderSelected))
            OnPropertyChanged(nameof(CanEditLoaderVersion));
    }

    private void ToggleDetailsEditMode()
    {
        if (!HasSelectedInstallation)
            return;

        IsDetailsEditMode = !IsDetailsEditMode;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LinkLauncher.Core.Models;
using LinkLauncher.Core.ModLoaders;

namespace LinkLauncher.App.ViewModels;

public sealed class CreateViewModel : INotifyPropertyChanged
{
    private string _newInstanceName = string.Empty;
    private const string DefaultCoverImagePath = "avares://LinkLauncher.App/Assets/logo.png";
    private string _selectedImagePath = DefaultCoverImagePath;
    private MinecraftVersionItem? _selectedVersion;
    private LoaderType _selectedLoaderType = LoaderType.Vanilla;
    public event PropertyChangedEventHandler? PropertyChanged;

    public CreateViewModel(MainWindowViewModel app)
    {
        App = app;
        CreateInstanceCommand = new RelayCommand(CreateInstance);

        if (FilteredVersions.Count > 0)
            _selectedVersion = FilteredVersions[0];
    }
    public MainWindowViewModel App { get; }

    public string InstallationSummary => App.InstallationSummary;
    public ObservableCollection<MinecraftVersionItem> FilteredVersions => App.FilteredVersions;
    public ObservableCollection<LoaderType> LoaderTypes => App.LoaderTypes;
    public string SelectedImagePath
    {
        get => _selectedImagePath;
        set
        {
            if (_selectedImagePath == value)
                return;

            _selectedImagePath = value;
            OnPropertyChanged();
        }
    }

    public string NewInstanceName
    {
        get => _newInstanceName;
        set
        {
            if (_newInstanceName == value)
                return;

            _newInstanceName = value;
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
            OnPropertyChanged();
        }
    }

    public LoaderType SelectedLoaderType
    {
        get => _selectedLoaderType;
        set
        {
            if (_selectedLoaderType == value)
                return;

            _selectedLoaderType = value;
            OnPropertyChanged();
        }
    }

    public ICommand CreateInstanceCommand { get; }
    public ICommand UseDefaultImageCommand => new RelayCommand(() =>
    {
        SelectedImagePath = DefaultCoverImagePath;
    });


    private void CreateInstance()
    {
        App.CreateInstallationFromDraft(
            string.IsNullOrWhiteSpace(NewInstanceName) ? "Nova Instancia" : NewInstanceName.Trim(),
            SelectedVersion?.Name ?? "latest-release",
            SelectedLoaderType,
            SelectedImagePath);

        NewInstanceName = string.Empty;
        SelectedImagePath = DefaultCoverImagePath;

        if (FilteredVersions.Count > 0)
            SelectedVersion = FilteredVersions[0];

        SelectedLoaderType = LoaderType.Vanilla;
    }


    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

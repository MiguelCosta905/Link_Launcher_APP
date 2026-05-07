using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using LinkLauncher.Core.Models;

namespace LinkLauncher.App.ViewModels;

public sealed class HomeViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public HomeViewModel(MainWindowViewModel app)
    {
        App = app;
        App.PropertyChanged += OnAppPropertyChanged;
        App.Installations.CollectionChanged += OnInstallationsChanged;

        LoadMockContent();
        Refresh();
    }

    public MainWindowViewModel App { get; }

    public ObservableCollection<LauncherProfile> RecentInstallations { get; } = new();
    public ObservableCollection<HomeNewsItem> NewsItems { get; } = new();
    public ObservableCollection<LauncherChangelogItem> ChangelogItems { get; } = new();

    public string RecentInstallationsSubtitleText =>
        RecentInstallations.Count == 0
            ? "Ainda nao ha instancias jogadas recentemente."
            : "As 3 ultimas instancias lancadas aparecem aqui.";

    public string NewsSubtitleText =>
        "Bloco inicial com noticias mockadas para depois ligares a uma fonte real.";

    public string ChangelogSubtitleText =>
        "Resumo curto do que mudou no LinkLauncher.";

    public void Refresh()
    {
        RebuildRecentInstallations();
        OnPropertyChanged(nameof(RecentInstallationsSubtitleText));
    }

    private void LoadMockContent()
    {
        NewsItems.Clear();
        NewsItems.Add(new HomeNewsItem
        {
            Category = "Minecraft Update",
            Title = "Nova area de noticias pronta",
            Summary = "Esta coluna ja esta preparada para receber noticias reais do Minecraft, snapshots e updates de loaders.",
            Footer = "Mock inicial do launcher"
        });
        NewsItems.Add(new HomeNewsItem
        {
            Category = "Loaders",
            Title = "Fabric, Forge e NeoForge",
            Summary = "Podes usar esta secao para mostrar compatibilidades, novas builds ou avisos temporarios por versao.",
            Footer = "Mock inicial do launcher"
        });
        NewsItems.Add(new HomeNewsItem
        {
            Category = "Comunidade",
            Title = "Espaco para destaques",
            Summary = "Tambem serve para modpacks recomendados, eventos ou avisos do proprio LinkLauncher.",
            Footer = "Mock inicial do launcher"
        });

        ChangelogItems.Clear();
        ChangelogItems.Add(new LauncherChangelogItem
        {
            Version = "v0.1.0",
            Summary = "Nova navegacao com Home, Skins, Biblioteca e Criar. Separacao inicial das paginas e limpeza da base do launcher."
        });
        ChangelogItems.Add(new LauncherChangelogItem
        {
            Version = "Proximo",
            Summary = "Home com instancias recentes reais, importacao de instancias/modpacks, biblioteca mais completa e primeira base da area de skins."
        });
    }

    private void RebuildRecentInstallations()
    {
        RecentInstallations.Clear();

        var recentProfiles = App.Installations
            .Where(profile => profile.LastPlayedAtUtc.HasValue)
            .OrderByDescending(profile => profile.LastPlayedAtUtc)
            .ThenBy(profile => profile.Name)
            .Take(3)
            .ToList();

        foreach (var profile in recentProfiles)
            RecentInstallations.Add(profile);
    }

    private void OnAppPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(App.SelectedInstallation) ||
            e.PropertyName == nameof(App.InstallationName) ||
            e.PropertyName == nameof(App.StatusText))
        {
            Refresh();
        }
    }

    private void OnInstallationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Refresh();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

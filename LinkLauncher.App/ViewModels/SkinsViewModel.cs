namespace LinkLauncher.App.ViewModels;

public sealed class SkinsViewModel
{
    public SkinsViewModel(MainWindowViewModel app)
    {
        App = app;
    }

    public MainWindowViewModel App { get; }
}

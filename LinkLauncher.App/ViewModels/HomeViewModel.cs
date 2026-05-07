namespace LinkLauncher.App.ViewModels;

public sealed class HomeViewModel
{
    public HomeViewModel(MainWindowViewModel app)
    {
        App = app;
    }

    public MainWindowViewModel App { get; }
}

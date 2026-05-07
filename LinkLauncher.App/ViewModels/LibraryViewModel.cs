namespace LinkLauncher.App.ViewModels;

public sealed class LibraryViewModel
{
    public LibraryViewModel(MainWindowViewModel app)
    {
        App = app;
    }

    public MainWindowViewModel App { get; }
}

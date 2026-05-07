namespace LinkLauncher.App.ViewModels;

public sealed class CreateViewModel
{
    public CreateViewModel(MainWindowViewModel app)
    {
        App = app;
    }

    public MainWindowViewModel App { get; }
}

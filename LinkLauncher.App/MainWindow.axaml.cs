using Avalonia.Controls;
using LinkLauncher.App.ViewModels;

namespace LinkLauncher.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}

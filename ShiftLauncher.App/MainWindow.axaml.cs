using Avalonia.Controls;
using ShiftLauncher.App.ViewModels;

namespace ShiftLauncher.App;

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

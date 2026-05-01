using Avalonia.Controls;
using ShiftLauncher.App.ViewModels;

namespace ShiftLauncher.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

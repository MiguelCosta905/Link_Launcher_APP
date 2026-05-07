using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LinkLauncher.App.ViewModels;

namespace LinkLauncher.App.Views;

public partial class CreateView : UserControl
{
    public CreateView()
    {
        InitializeComponent();
    }

    private async void ChooseImageButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return;

        if (DataContext is not CreateViewModel viewModel)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Escolher imagem da instancia",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Imagens")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"]
                }
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
            return;

        var localPath = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
            return;

        viewModel.SelectedImagePath = localPath;
    }
}

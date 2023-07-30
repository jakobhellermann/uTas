using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TasEditor.ViewModels;

namespace TasEditor.Views;

public partial class MainWindow : Window {
    private MainViewModel MainViewModel => (MainViewModel)DataContext!;

    public MainWindow() {
        InitializeComponent();
    }

    private static FilePickerFileType FileType => new("TAS") { Patterns = new[] { "*.tas" } };


    private async void OpenFile(object? sender, RoutedEventArgs e) {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            AllowMultiple = false,
            FileTypeFilter = new[] { FileType }
        });
        if (files.Count == 0) return;
        var file = files[0];

        var path = file.TryGetLocalPath();
        if (path is null) return; // TODO error

        MainViewModel.CurrentFilePath = path;
        MainViewModel.FrameByFrameEditorOpen = false;

        App.SettingsService.Save(App.SettingsService.Settings with { CurrentFile = path });
    }

    private async void SaveFileAs(object? sender, RoutedEventArgs e) {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            DefaultExtension = "tas",
            SuggestedFileName = MainViewModel.CurrentFileName,
            FileTypeChoices = new[] { FileType }
        });

        if (file is null) return;

        var path = file.TryGetLocalPath();
        if (path is null) return; // TODO error

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(MainView.Editor.TextEditor.Text);

        MainViewModel.CurrentFilePath = path;
    }
}
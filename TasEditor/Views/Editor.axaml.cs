using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using TasEditor.Services;
using TasEditor.ViewModels;
using TasEditor.Views.Editing;
using TextMateSharp.Grammars;
using uTas.Communication;
using uTas.TasFormat;

namespace TasEditor.Views;

public partial class Editor : UserControl {
    private readonly CurrentFrameBackgroundRenderer _currentFrameBackgroundRenderer;

    private MainViewModel MainViewModel => (MainViewModel)DataContext!;
    private ITasEditingService TasEditingService => App.TasEditingService;

    private const ThemeName DefaultTheme = ThemeName.Monokai;

    public Editor() {
        InitializeComponent();

        TextEditor.TextChanged += TextChanged;

        // TODO: make this not suck
        ((TasEditingService)App.TasEditingService).TextArea = TextEditor.TextArea;

        FrameByFrameEditor.OnChange = OnFrameByFrameEditorChange;

        var registryOptions = new RegistryOptions(DefaultTheme);
        var textMateInstallation = TextEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId("julia"));

        TextEditor.Options.HighlightCurrentLine = true;

        TextEditor.TextArea.PushStackedInputHandler(new FormattingInputHandler(TextEditor.TextArea));

        _currentFrameBackgroundRenderer = new CurrentFrameBackgroundRenderer();
        TextEditor.TextArea.TextView.BackgroundRenderers.Add(_currentFrameBackgroundRenderer);

        TextEditor.ContextMenu = (ContextMenu)Resources["EditorContextMenu"]!;
        TextEditor.TextArea.RightClickMovesCaret = true;

        DataContextChanged += (_, _) => {
            OnCurrentFilePathChanged(MainViewModel.CurrentFilePath);

            MainViewModel.PropertyChanged += (_, e) => {
                if (e.PropertyName == "StudioInfo")
                    Dispatcher.UIThread.Invoke(() => { OnStudioInfoChanged(MainViewModel.StudioInfo); });
                else if (e.PropertyName == "CurrentFilePath")
                    OnCurrentFilePathChanged(MainViewModel.CurrentFilePath);
            };
        };
    }

    private void OnCurrentFilePathChanged(string? currentFilePath) {
        if (currentFilePath is not { } path) {
            TextEditor.Text = "";
            return;
        }

        try {
            var content = File.ReadAllText(path);
            TextEditor.Text = content;
        } catch (Exception exception) {
            Console.WriteLine($"failed to read text: {exception}");
        }
    }

    private void OnStudioInfoChanged(StudioInfo? info) {
        if (info is not { } studioInfo) {
            _currentFrameBackgroundRenderer.ActiveLineNumber = -1;
            TextEditor.TextArea.TextView.InvalidateMeasure();
            return;
        }

        _currentFrameBackgroundRenderer.ActiveLineNumber = studioInfo.CurrentLine;
        _currentFrameBackgroundRenderer.CurrentFrame = studioInfo.CurrentLineSuffix;

        var line = TextEditor.Document.GetLineByNumber(studioInfo.CurrentLine);
        var text = TextEditor.Document.GetText(line.Offset, line.Length);
        var lineOffset = line.Offset;
        var commaIndex = text.IndexOf(',');
        var columnIndex = commaIndex == -1 ? line.Length - 1 : commaIndex;

        TextEditor.Select(lineOffset + columnIndex, 0);
        TextEditor.ScrollToLine(studioInfo.CurrentLine);

        TextEditor.TextArea.TextView.InvalidateMeasure();
    }

    private void TextChanged(object? sender, EventArgs e) {
        MainViewModel.EditorTextDirty = true;

        if (MainViewModel.CurrentFilePath is null) return;

        var text = TextEditor.Text;
        var path = MainViewModel.CurrentFilePath;
        Task.Run(async () => {
            await File.WriteAllTextAsync(path, text);
            MainViewModel.EditorTextDirty = false;
        });
    }


    private void OpenFrameByFrameEditor(object sender, RoutedEventArgs e) {
        TasEditingService.ExtendSelectionLineBoundaries();

        var yPos = TextEditor.TextArea.TextView.GetVisualTopByDocumentLine(
            TextEditor.TextArea.Selection.StartPosition.Line
        ) - TextEditor.VerticalOffset;

        FrameByFrameEditorContainer.Margin = new Thickness(0, yPos, 0, 0);

        try {
            var tasInputs = TasFile.Parse(TextEditor.SelectedText);
            FrameByFrameEditor.GenerateGrid(tasInputs);
        } catch (Exception exception) {
            Console.WriteLine(exception);
        }

        MainViewModel.OpenFrameByFrameEditor();

        /*var flyout = (Flyout)Resources["FrameByFrameFlyout"]!;
        if (flyout.IsOpen) {
            flyout.Hide();
        } else {
            flyout.ShowAt((Control)this, true);
        }*/
    }


    private void OnFrameByFrameEditorChange(string tas) {
        TextEditor.SelectedText = tas;
    }

    private void CloseFrameByFrameEditor(object? sender, PointerPressedEventArgs e) {
        MainViewModel.CloseFrameByFrameEditor();
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using TasEditor.ViewModels;
using TasEditor.Views.Editing;
using TasFormat;
using TextMateSharp.Grammars;

namespace TasEditor.Views;

public partial class Editor : UserControl {
    private readonly CurrentFrameBackgroundRenderer _currentFrameBackgroundRenderer;

    private MainViewModel MainViewModel => (MainViewModel)DataContext!;

    public Editor() {
        InitializeComponent();

        TextEditor.TextChanged += TextChanged;


        FrameByFrameEditor.OnChange = OnFrameByFrameEditorChange;


        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var textMateInstallation = TextEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId("julia"));

        TextEditor.Options.HighlightCurrentLine = true;

        TextEditor.TextArea.PushStackedInputHandler(new FormattingInputHandler(TextEditor.TextArea));

        _currentFrameBackgroundRenderer = new CurrentFrameBackgroundRenderer();
        TextEditor.TextArea.TextView.BackgroundRenderers.Add(_currentFrameBackgroundRenderer);

        TextEditor.ContextMenu = (ContextMenu)Resources["EditorContextMenu"]!;
        TextEditor.TextArea.RightClickMovesCaret = true;


        AddHandler(KeyDownEvent, (o, i) => {
            _currentFrameBackgroundRenderer.CurrentFrame += 1;
            _currentFrameBackgroundRenderer.CurrentFrame = 1;
            _currentFrameBackgroundRenderer.ActiveLineNumber += 1;
        });
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

    private void ExtendSelectionLineBoundaries() {
        var selection = TextEditor.TextArea.Selection;
        if (selection.IsEmpty) {
            var caretLine = TextEditor.TextArea.Caret.Line;
            var line = TextEditor.Document.GetLineByNumber(caretLine);
            TextEditor.TextArea.Selection = Selection.Create(TextEditor.TextArea, line.Offset, line.EndOffset);
        } else {
            var startLine = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
            var endLine = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);

            var startOffset = TextEditor.Document.GetLineByNumber(Math.Max(1, startLine)).Offset;
            var endOffset = TextEditor.Document.GetLineByNumber(Math.Max(1, endLine)).EndOffset;

            TextEditor.TextArea.Selection = Selection.Create(
                TextEditor.TextArea,
                startOffset, endOffset
            );
        }
    }

    private void OpenFrameByFrameEditor(object sender, RoutedEventArgs e) {
        ExtendSelectionLineBoundaries();

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

        ((MainViewModel)DataContext!).OpenFrameByFrameEditor();

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
        ((MainViewModel)DataContext!).CloseFrameByFrameEditor();
    }
}
using System;
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

    public Editor() {
        InitializeComponent();

        FrameByFrameEditor.OnChange = OnFrameByFrameEditorChange;


        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var textMateInstallation = TextEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId("julia"));

        TextEditor.TextArea.PushStackedInputHandler(new FormattingInputHandler(TextEditor.TextArea));

        TextEditor.Text = SampleText;

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


    private const string SampleText = @"RecordCount: 1150
console load SecretSanta2023/2-Medium/powerav
   1

#Start
 110

#lvl_a-0
   1,S
   1,D,J
  34
   5,L,D,X
   1,R,J
   7,R
   1,R,J
   5,R,K,G
   7,R
   1,R,J
  12,R,D,X
  40

#lvl_x-1
   1,R,D,X
   2,R,J
   1,J
   1,R
   1,R,J
   3,J
   1
   1,J
   1
   1,J
   3
   7,R,Z
   1,R,J
   6,R
   1,R,J
  14,R,D,X
   7,R,J
  10,R,X
  13,R
  14,R,X
  15,L,C
   8,R
  15,U,X
   5,R
  13,R,J,G
   2,R,K,G
   2,R
   1
   1,R
   2
   1,R
   1,L
   1
  14,R,Z
   1,R,J
  27,R,D,X
   1,R,J
   5,R
  40

#lvl_x-2
   9,R
  24,R,J
  15,U,C
  15,R,X
   8,R
   6
  18,L,J
  13,U,X
  15,L,C
";

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
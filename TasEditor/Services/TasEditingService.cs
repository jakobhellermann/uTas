using System;
using AvaloniaEdit.Editing;

namespace TasEditor.Services;

public class TasEditingService : ITasEditingService {
    public TextArea TextArea = null!; // Inititalized by Editor.axaml.cs

    public void ToggleCommentLineByLine() {
    }

    public void ToggleComment() {
    }

    public void CombineConsecutiveInputs() {
    }

    public void ExtendSelectionLineBoundaries() {
        var selection = TextArea.Selection;
        if (selection.IsEmpty) {
            var caretLine = TextArea.Caret.Line;
            var line = TextArea.Document.GetLineByNumber(caretLine);
            TextArea.Selection = Selection.Create(TextArea, line.Offset, line.EndOffset);
        } else {
            var startLine = Math.Min(selection.StartPosition.Line, selection.EndPosition.Line);
            var endLine = Math.Max(selection.StartPosition.Line, selection.EndPosition.Line);

            var startOffset = TextArea.Document.GetLineByNumber(Math.Max(1, startLine)).Offset;
            var endOffset = TextArea.Document.GetLineByNumber(Math.Max(1, endLine)).EndOffset;

            TextArea.Selection = Selection.Create(TextArea, startOffset, endOffset);
        }
    }
}
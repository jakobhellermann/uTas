using System;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using TasFormat;

namespace TasEditor.Services;

public class TasEditingService : ITasEditingService {
    public TextArea TextArea = null!; // Inititalized by Editor.axaml.cs

    private const char CommentToken = '#';

    private int SelectionStartLine =>
        Math.Min(TextArea.Selection.StartPosition.Line, TextArea.Selection.EndPosition.Line);

    private int SelectionEndLine =>
        Math.Max(TextArea.Selection.StartPosition.Line, TextArea.Selection.EndPosition.Line);


    public void ToggleCommentLineByLine() {
        if (TextArea.Selection.Length == 0) {
            var line = TextArea.Document.GetLineByNumber(TextArea.Caret.Line);
            var text = GetText(line);
            if (text.AsSpan().TrimStart().IsWhiteSpace()) return;

            LineToggleComment(line, text);
            TextArea.Caret.Line += 1;
        } else {
            var startLine = TextArea.Document.GetLineByNumber(SelectionStartLine);
            var endLine = SelectionEndLine;

            TextArea.Document.BeginUpdate();
            for (var line = startLine; line != null && line.LineNumber <= endLine; line = line.NextLine) {
                var text = GetText(line);
                if (text.AsSpan().TrimStart().IsWhiteSpace()) continue;

                LineToggleComment(line, text);
            }

            TextArea.Document.EndUpdate();
            ExtendSelectionLineBoundaries();
        }
    }

    public void ToggleComment() {
        if (TextArea.Selection.Length == 0) {
            ToggleCommentLineByLine();
            return;
        }

        var startLine = TextArea.Document.GetLineByNumber(SelectionStartLine);
        var endLine = SelectionEndLine;

        var anyHasComment = false;
        for (var line = startLine; line != null && line.LineNumber <= endLine; line = line.NextLine) {
            var text = GetText(line);
            var lineTrimmed = text.AsSpan().TrimStart();
            if (lineTrimmed.IsWhiteSpace()) continue;
            anyHasComment |= lineTrimmed.StartsWith(new ReadOnlySpan<char>(CommentToken));
        }

        TextArea.Document.BeginUpdate();
        for (var line = startLine; line != null && line.LineNumber <= endLine; line = line.NextLine) {
            var text = GetText(line);
            var lineTrimmed = text.AsSpan().TrimStart();
            if (lineTrimmed.IsWhiteSpace()) continue;
            LineSetComment(line, text, !anyHasComment);
        }

        TextArea.Document.EndUpdate();

        ExtendSelectionLineBoundaries();
    }

    public void CombineConsecutiveInputs() {
        if (TextArea.Selection.Length == 0) {
            var caretLine = TextArea.Caret.Line;

            while (true)
                if (!CombineLines(Math.Max(caretLine - 1, 1), Math.Min(caretLine + 1, TextArea.Document.LineCount)))
                    break;
        } else {
            CombineLines(SelectionStartLine, SelectionEndLine);
        }
    }

    private bool CombineLines(int startLine, int endLine) {
        var startDocumentLine = TextArea.Document.GetLineByNumber(startLine);
        var endDocumentLine = TextArea.Document.GetLineByNumber(endLine);
        var length = endDocumentLine.EndOffset - startDocumentLine.Offset;

        var text = TextArea.Document.GetText(startDocumentLine.Offset, length);

        while (text.StartsWith("\n")) {
            startDocumentLine = startDocumentLine.NextLine;
            length = endDocumentLine.EndOffset - startDocumentLine.Offset;
            text = TextArea.Document.GetText(startDocumentLine.Offset, length);
        }


        var file = TasFile.Parse(text);
        var changed = file.Combine();

        if (changed) {
            var combined = file.ToTasFormat();
            TextArea.Document.Replace(startDocumentLine.Offset, length, combined,
                OffsetChangeMappingType.Normal);
        }

        return changed;
    }

    private void LineToggleComment(ISegment line, ReadOnlySpan<char> lineText) {
        var hasComment = lineText.StartsWith(new ReadOnlySpan<char>(CommentToken));
        LineSetComment(line, lineText, !hasComment);
    }

    private void LineSetComment(ISegment line, ReadOnlySpan<char> lineText, bool shouldHaveComment) {
        if (shouldHaveComment) {
            TextArea.Document.Insert(line.Offset, "#");
        } else {
            var commentIndex = lineText.IndexOf(CommentToken);
            TextArea.Document.Replace(line.Offset, commentIndex + 1, "");
        }
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


    private string GetText(ISegment line) => TextArea.Document.GetText(line.Offset, line.Length);
}
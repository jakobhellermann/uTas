using System;
using System.Linq;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace TasEditor.Views.Editing;

public class FormattingInputHandler : TextAreaStackedInputHandler {
    private const int AlignFrameCountTo = 4;

    public FormattingInputHandler(TextArea textArea) : base(textArea) {
    }

    private static bool InterestedInKey(Key key) =>
        key is >= Key.A and <= Key.Z or >= Key.D0 and <= Key.D9 or >= Key.NumPad0 and <= Key.NumPad9
            or Key.Enter or Key.Back;

    private TextViewPosition _newPosition;

    private string GetText(ISegment line) => TextArea.Document.GetText(line.Offset, line.Length);

    public override void OnPreviewKeyDown(KeyEventArgs e) {
        if (e.KeyModifiers != KeyModifiers.None) return;
        if (!InterestedInKey(e.Key)) return;

        var selection = TextArea.Selection;
        if (selection.Length > 0) return;

        var position = TextArea.Caret.Position;
        var line = TextArea.Document.GetLineByNumber(position.Line);
        var lineText = GetText(line);
        _newPosition = position;

        string? handled = null;
        switch (e.Key) {
            case >= Key.D0 and <= Key.D9:
                handled = OnNumberKeyDown(e.Key - Key.D0, lineText, position.Column);
                break;
            case >= Key.NumPad0 and <= Key.NumPad9:
                handled = OnNumberKeyDown(e.Key - Key.NumPad0, lineText, position.Column);
                break;
            case >= Key.A and <= Key.Z:
                handled = OnInputKeyDown((char)('A' + (e.Key - Key.A)), lineText);
                break;
            case Key.Enter:
                e.Handled = OnEnter(line, lineText, position.Column);
                return;
            case Key.Back:
                e.Handled = OnBackspace(line, lineText, position.Column);
                break;
        }

        if (handled is { } text) {
            TextArea.Document.Replace(line.Offset, line.Length, text);
            TextArea.Caret.Position = _newPosition;
            e.Handled = true;
        }
    }

    private string? OnNumberKeyDown(int digit, string line, int column) {
        var columnIndex = column - 1;

        var isFrameInputLine = IsFrameInputLine(line, true);
        if (!isFrameInputLine) return null;

        var commaIndex = line.IndexOf(',');
        var beforeComma = commaIndex == -1 ? line : line[..commaIndex];
        var afterComma = commaIndex == -1 ? ReadOnlySpan<char>.Empty : line.AsSpan()[(commaIndex + 1)..];

        var clampedColumn = Math.Min(columnIndex, commaIndex == -1 ? line.Length : commaIndex);
        var withAddedDigit = beforeComma.Insert(clampedColumn, digit.ToString())
            .Replace(" ", "");
        if (!int.TryParse(withAddedDigit, out var number)) return null;

        if (commaIndex != -1 && columnIndex > commaIndex)
            _newPosition.Column = commaIndex + 1;
        else if (string.IsNullOrWhiteSpace(beforeComma)) _newPosition.Column = AlignFrameCountTo + 1;

        var numberClamped = Math.Min(number, Math.Pow(10, AlignFrameCountTo) - 1);

        var comma = afterComma.IsEmpty ? "" : ",";
        return $"{numberClamped,AlignFrameCountTo}{comma}{afterComma}";
    }

    private string? OnInputKeyDown(char key, string line) {
        var isFrameInputLine = IsFrameInputLine(line);
        if (!isFrameInputLine) return null;

        return ToggleKey(line, key);
    }


    private static string ToggleKey(string line, char key) {
        var commaIndex = line.IndexOf(',');
        var beforeComma = commaIndex == -1 ? line.AsSpan() : line.AsSpan()[..commaIndex];
        var afterComma = commaIndex == -1 ? "" : line[(commaIndex + 1)..];

        var keyString = key.ToString();

        var keys = afterComma
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (keys.Contains(keyString))
            keys.Remove(keyString);
        else
            keys.Add(keyString);

        keys.Sort();

        return $"{beforeComma},{string.Join(',', keys)}";
    }

    private bool OnEnter(IDocumentLine documentLine, string line, int column) {
        if (line.Length == 0) return false;
        if (column == 1) {
            TextArea.Document.Insert(documentLine.Offset - documentLine.DelimiterLength, "\n",
                AnchorMovementType.BeforeInsertion);
            return true;
        }

        var isFrameInputLine = IsFrameInputLine(line);
        if (!isFrameInputLine) return false;

        TextArea.Document.Insert(documentLine.Offset + documentLine.Length + documentLine.DelimiterLength, "\n",
            AnchorMovementType.AfterInsertion);

        TextArea.Caret.Position = new TextViewPosition(TextArea.Caret.Position.Line + 1, 1);
        return true;
    }

    private bool OnBackspace(IDocumentLine documentLine, string line, int column) {
        var columnIndex = column - 1;
        var isFrameInputLine = IsFrameInputLine(line);
        if (!isFrameInputLine) return false;

        var commaIndex = line.IndexOf(',');
        if (commaIndex == -1) return false;
        var beforeComma = line.AsSpan()[..commaIndex];
        var afterComma = line.AsSpan()[(commaIndex + 1)..];

        var onChar = columnIndex == 0 ? ' ' : line[columnIndex - 1];

        if (columnIndex < commaIndex + 1) {
            if (onChar == ' ') {
                if (columnIndex == 0 && !IsFrameInputLine(GetText(documentLine.PreviousLine))) return false;

                var position = TextArea.Caret.Position;
                TextArea.Caret.Position = position with {
                    Line = position.Line - 1, Column = documentLine.PreviousLine.Length + 1
                };
                return true;
            } else {
                var withRemovedDigit = beforeComma.ToString().Remove(columnIndex - 1, 1);

                string newLine;
                var comma = afterComma.IsEmpty ? "" : ",";
                if (withRemovedDigit.Trim().Length == 0) {
                    newLine = $"{new string(' ', AlignFrameCountTo)}{comma}{afterComma}";
                } else {
                    if (!int.TryParse(withRemovedDigit, out var number)) return false;
                    var numberClamped = Math.Min(number, Math.Pow(10, AlignFrameCountTo) - 1);
                    newLine = $"{numberClamped,AlignFrameCountTo}{comma}{afterComma}";
                }

                TextArea.Document.Replace(documentLine.Offset, documentLine.Length, newLine);
                TextArea.Caret.Column = column;

                return true;
            }
        } else if (columnIndex > commaIndex + 1) {
            var prevComma = line.LastIndexOf(',', columnIndex - 2, columnIndex - 2);
            var nextComma = line.IndexOf(',', columnIndex - 1);
            if (nextComma == -1) nextComma = line.Length;
            if (prevComma == -1) return false;

            var newLine = line.Remove(prevComma, nextComma - prevComma);
            TextArea.Document.Replace(documentLine.Offset, documentLine.Length, newLine);
            TextArea.Caret.Column = prevComma + 2;

            return true;
        } else {
            return OnBackspace(documentLine, line, column - 1);
        }
    }

    private bool IsFrameInputLine(ReadOnlySpan<char> line, bool countEmpty = false) {
        var lineTrimmed = line.Trim();
        if (lineTrimmed.Length == 0) return countEmpty;
        return lineTrimmed[0] is >= '0' and <= '9' or ',';
    }
}
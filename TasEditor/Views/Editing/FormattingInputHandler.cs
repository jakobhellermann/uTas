using System;
using System.Linq;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Key = Avalonia.Input.Key;

namespace TasEditor.Views.Editing;

public class FormattingInputHandler : TextAreaStackedInputHandler {
    private const int AlignFrameCountTo = 4;

    public FormattingInputHandler(TextArea textArea) : base(textArea) {
    }

    private static bool InterestedInKey(Key key) {
        return key is >= Key.A and <= Key.Z or >= Key.D0 and <= Key.D9 or >= Key.NumPad0 and <= Key.NumPad9
            or Key.Enter;
    }

    private TextViewPosition _newPosition;

    public override void OnPreviewKeyDown(KeyEventArgs e) {
        if (e.KeyModifiers != KeyModifiers.None) return;
        if (!InterestedInKey(e.Key)) return;

        var selection = TextArea.Selection;
        if (selection.Length > 0) return;

        var position = TextArea.Caret.Position;
        var line = TextArea.Document.GetLineByNumber(position.Line);
        var lineText = TextArea.Document.GetText(line.Offset, line.Length);
        _newPosition = position;

        string? handled;
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
                OnEnter(line);
                e.Handled = true;
                return;
            default:
                return;
        }

        if (handled is { } text) {
            TextArea.Document.Replace(line.Offset, line.Length, text);
            TextArea.Caret.Position = _newPosition;
            e.Handled = true;
        }
    }

    private string? OnNumberKeyDown(int number, string line, int column) {
        var columnIndex = column - 1;

        var lineTrimmed = line.AsSpan().Trim();
        var isFrameInputLine = lineTrimmed.Length == 0 || char.IsNumber(lineTrimmed[0]) || lineTrimmed[0] == ',';
        if (!isFrameInputLine) return null;

        var commaIndex = line.IndexOf(',');
        var beforeComma = commaIndex == -1 ? line : line[..commaIndex];
        var afterComma = commaIndex == -1 ? ReadOnlySpan<char>.Empty : line.AsSpan()[(commaIndex + 1)..];

        var clampedColumn = Math.Min(columnIndex, commaIndex == -1 ? line.Length : commaIndex);
        var withAddedDigit = beforeComma.Insert(clampedColumn, number.ToString())
            .Replace(" ", "");
        if (!int.TryParse(withAddedDigit, out number)) return null;

        if (commaIndex != -1 && columnIndex > commaIndex) {
            _newPosition.Column = commaIndex + 1;
        } else if (string.IsNullOrWhiteSpace(beforeComma)) {
            _newPosition.Column = AlignFrameCountTo + 1;
        }

        var numberClamped = Math.Min(number, Math.Pow(10, AlignFrameCountTo) - 1);
        return $"{numberClamped,AlignFrameCountTo},{afterComma}";
    }

    private string? OnInputKeyDown(char key, string line) {
        var lineTrimmed = line.AsSpan().Trim();
        var isFrameInputLine = lineTrimmed.Length > 0 && char.IsNumber(lineTrimmed[0]);
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
        if (keys.Contains(keyString)) {
            keys.Remove(keyString);
        } else {
            keys.Add(keyString);
        }

        keys.Sort();

        return $"{beforeComma},{string.Join(',', keys)}";
    }

    private void OnEnter(DocumentLine line) {
        TextArea.Document.Insert(line.Offset + line.Length + line.DelimiterLength, "\n",
            AnchorMovementType.AfterInsertion);

        TextArea.Caret.Position = new TextViewPosition(TextArea.Caret.Position.Line + 1, 1);
    }
}
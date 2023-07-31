using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace TasEditor.Views.Editing;

public class FormattingInputHandler : TextAreaStackedInputHandler {
    private const int AlignFrameCountTo = 4;
    private const bool RemoveExclusiveActions = true;

    public FormattingInputHandler(TextArea textArea) : base(textArea) {
    }

    private static bool InterestedInKey(Key key) =>
        key is >= Key.A and <= Key.Z or >= Key.D0 and <= Key.D9 or >= Key.NumPad0 and <= Key.NumPad9
            or Key.Enter or Key.Back or Key.Space or Key.OemComma;

    private TextViewPosition _newPosition;

    private string GetText(ISegment line) => TextArea.Document.GetText(line.Offset, line.Length);

    public override void OnPreviewKeyDown(KeyEventArgs e) {
        var isShift = (e.KeyModifiers & KeyModifiers.Shift) != 0;

        if (e.KeyModifiers != KeyModifiers.None && !(isShift && e.Key == Key.Enter)) return;
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
                e.Handled = OnEnter(line, lineText, position.Column, isShift);
                return;
            case Key.Back:
                e.Handled = OnBackspace(line, lineText, position.Column);
                break;
            case Key.Space:
                e.Handled = OnSpace(line, lineText, position.Column);
                break;
            case Key.OemComma:
                e.Handled = OnComma(line, lineText, position.Column);
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

        if (IsInsideValuedAction(line, columnIndex)) return null;

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

    private bool IsAction(string action, string actionValue) {
        var parenIndex = actionValue.IndexOf('(', StringComparison.Ordinal);
        var beforeParen = parenIndex == -1 ? actionValue : actionValue[..parenIndex];
        return beforeParen == action;
    }


    private string ToggleKey(string line, char key) {
        var commaIndex = line.IndexOf(',');
        var beforeComma = commaIndex == -1 ? line.AsSpan() : line.AsSpan()[..commaIndex];
        var afterComma = commaIndex == -1 ? "" : line[(commaIndex + 1)..];

        var keyString = key.ToString();

        var actionValueCount = ValuedActions.GetValueOrDefault(keyString);

        bool added;

        var keys = SplitWithBalancedParenthesis(afterComma,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (keys.Any(k => IsAction(keyString, k))) {
            keys.RemoveAll(k => IsAction(keyString, k));
            added = false;
        } else {
            keys.Add(actionValueCount == 0 ? keyString : $"{keyString}({new string(',', actionValueCount - 1)})");

            if (RemoveExclusiveActions && ExclusiveActions.TryGetValue(keyString, out var excludes))
                keys.RemoveAll(k => excludes.Contains(k));
            added = true;
        }

        SortKeys(keys);

        var newLine = $"{beforeComma},{string.Join(',', keys)}";

        if (added && actionValueCount > 0) {
            var columnIndex = newLine.IndexOf($"{keyString}(", StringComparison.Ordinal);
            _newPosition.Column = columnIndex + 1 + keyString.Length + 1;
        } else {
            _newPosition.Column = commaIndex + 2;
        }

        return newLine;
    }

    private void RemoveLine(ISegment line) {
        TextArea.Document.Replace(line.Offset, line.Length, "");
    }

    private void InsertLine(IDocumentLine line, bool before = false) {
        var offset = before ? line.Offset - line.DelimiterLength : line.Offset + line.Length + line.DelimiterLength;
        TextArea.Document.Insert(offset, "\n");
        TextArea.Caret.Position = new TextViewPosition(TextArea.Caret.Position.Line + (before ? -1 : 1), 1);
    }

    private bool OnEnter(IDocumentLine documentLine, string line, int column, bool isShift) {
        if (isShift) {
            InsertLine(documentLine, true);
            return true;
        } else {
            var isFrameInputLine = IsFrameInputLine(line);
            if (!isFrameInputLine) return false;

            InsertLine(documentLine);
            return true;
        }
    }

    private bool OnBackspace(IDocumentLine documentLine, string line, int column) {
        var columnIndex = column - 1;
        var isFrameInputLine = IsFrameInputLine(line);
        if (!isFrameInputLine) return false;

        if (IsInsideValuedAction(line, columnIndex)) return false;

        var commaIndex = line.IndexOf(',');
        if (commaIndex == -1) return false;
        var beforeComma = line.AsSpan()[..commaIndex];
        var afterComma = line.AsSpan()[(commaIndex + 1)..];

        var onChar = columnIndex == 0 ? ' ' : line[columnIndex - 1];

        if (columnIndex < commaIndex + 1) {
            if (onChar == ' ') {
                RemoveLine(documentLine);
                return true;
            } else {
                if (!int.TryParse(beforeComma, out var numberBefore)) return false;

                var withRemovedDigit = beforeComma.ToString().Remove(columnIndex - 1, 1);

                var comma = afterComma.IsEmpty ? "" : ",";

                string newLine;
                if (numberBefore == 0) {
                    RemoveLine(documentLine);
                    return true;
                } else if (withRemovedDigit.Trim().Length == 0) {
                    newLine = $"{new string(' ', AlignFrameCountTo - 1)}0{comma}{afterComma}";
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


    private bool OnSpace(IDocumentLine documentLine, string line, int column) {
        if (!IsFrameInputLine(line)) return false;
        if (IsInsideValuedAction(line, column - 1)) return false;

        var commaIndex = line.IndexOf(',');
        var newColumnIndex = commaIndex == -1 ? line.Length : commaIndex;

        TextArea.Caret.Column = newColumnIndex + 1;
        return true;
    }

    private bool OnComma(IDocumentLine documentLine, string line, int column) {
        var columnIndex = column - 1;

        if (!IsFrameInputLine(line)) return false;
        if (!IsInsideValuedAction(line, columnIndex)) return false;

        if (TextArea.Document.GetCharAt(documentLine.Offset + columnIndex) == ',') {
            TextArea.Caret.Offset += 1;
            return true;
        }

        return false;
    }

    private bool IsFrameInputLine(ReadOnlySpan<char> line, bool countEmpty = false) {
        var lineTrimmed = line.Trim();
        if (lineTrimmed.Length == 0) return countEmpty;
        return lineTrimmed[0] is >= '0' and <= '9' or ',';
    }


    private static readonly string[] SortedKeys = {
        "L", "R", "U", "D", "J", "X"
    };

    private static readonly Dictionary<string, string[]> ExclusiveActions = new() {
        { "L", new[] { "R" } },
        { "R", new[] { "L" } },
        { "U", new[] { "D" } },
        { "D", new[] { "U" } }
    };

    private static readonly Dictionary<string, int> ValuedActions = new() {
        { "M", 2 },
        { "S", 1 }
    };

    private static void SortKeys(List<string> keys) {
        keys.Sort((a, b) => {
            var indexA = Array.IndexOf(SortedKeys, a);
            var indexB = Array.IndexOf(SortedKeys, b);

            if (indexA == -1 && indexB != -1) return 1;
            if (indexA != -1 && indexB == -1) return -1;
            if (indexA == -1 && indexB == -1) return string.Compare(a, b, StringComparison.Ordinal);
            if (indexA != -1 && indexB != -1) return indexA.CompareTo(indexB);

            throw new Exception();
        });
    }

    private static bool IsInsideValuedAction(string line, int index) {
        if (index == 0) return false;
        var prevParenOpen = line.LastIndexOf('(', index - 1, index - 1);
        var prevParenClose = line.LastIndexOf(')', index - 1, index - 1);
        return prevParenOpen > prevParenClose;
    }


    private static List<string> SplitWithBalancedParenthesis(string input, StringSplitOptions options) {
        var result = new List<string>();
        var parenthesisCount = 0;
        var startIndex = 0;

        var addItem = (string item) => {
            if ((options & StringSplitOptions.TrimEntries) != 0) item = item.Trim();
            if (!((options & StringSplitOptions.RemoveEmptyEntries) != 0 && item.Length == 0)) result.Add(item);
        };


        for (var i = 0; i < input.Length; i++) {
            var c = input[i];

            switch (c) {
                case '(':
                    parenthesisCount++;
                    break;
                case ')':
                    parenthesisCount--;
                    break;
                case ',' when parenthesisCount == 0:
                    var item = input.Substring(startIndex, i - startIndex);
                    addItem(item);
                    startIndex = i + 1;

                    break;
            }
        }

        addItem(input[startIndex..]);

        return result;
    }
}
using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;

namespace TasEditor.Views.Editing;

public class CurrentFrameBackgroundRenderer : IBackgroundRenderer {
    public int ActiveLineNumber = 1;
    public string CurrentFrame = "";

    public void Draw(TextView textView, DrawingContext drawingContext) {
        if (ActiveLineNumber == -1) return;

        var emSize = textView.GetValue(TextBlock.FontSizeProperty);
        var typeface = textView.CreateTypeface();

        var line = textView.VisualLines.FirstOrDefault(line => line.FirstDocumentLine.LineNumber == ActiveLineNumber);
        if (line is null) return;

        var text = new FormattedText(
            CurrentFrame,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            emSize,
            textView.GetValue(TextBlock.ForegroundProperty)
        );
        var x = textView.Bounds.Width - text.Width - textView.WideSpaceWidth;
        var y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop) - textView.VerticalOffset;
        drawingContext.DrawText(text, new Point(x, y));
    }

    public KnownLayer Layer => KnownLayer.Background;
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TasFormat;

public record TasLineInfo(TasLine Line, int LineNumber);

public partial record TasFile(List<TasLineInfo> Lines) {
    public override string ToString() =>
        $"TasFile {{\n    Lines = [\n        {string.Join(",\n        ", Lines)}\n    ] }}";

    public string ToTasFormat() {
        var builder = new StringBuilder();
        for (var i = 0; i < Lines.Count; i++) {
            var line = Lines[i];
            builder.Append(line.Line.ToTasFormat());
            if (i != Lines.Count - 1) builder.Append('\n');
        }

        return builder.ToString();
    }


    public void Expand() {
        for (var i = Lines.Count - 1; i >= 0; i--) {
            var info = Lines[i];
            if (info.Line is not TasLine.FrameInput { FrameCount: > 1 } input) continue;

            Lines.RemoveAt(i);

            var expanded = Enumerable.Range(0, input.FrameCount).Select(_ => {
                var line = new TasLine.FrameInput(1, input.Inputs.Select(inp => new Input(inp.Key)).ToHashSet());
                return new TasLineInfo(line, info.LineNumber);
            });
            Lines.InsertRange(i, expanded);
        }
    }

    public void Collapse() {
        if (Lines.Count == 0) return;

        var newLines = new List<TasLineInfo>();

        var current = Lines[0].Line;
        var counter = 1;

        void Flush(TasLine? newLine) {
            if (current is TasLine.FrameInput input) {
                newLines.Add(new TasLineInfo(input with { FrameCount = counter }, 0));
            } else {
                if (counter != 1) throw new Exception();
                newLines.Add(new TasLineInfo(current, 0));
            }

            current = newLine;
            counter = 1;
        }

        for (var index = 1; index < Lines.Count; index++) {
            var line = Lines[index];

            if (line.Line is TasLine.FrameInput input) {
                if (current is not TasLine.FrameInput currentInput) {
                    Flush(input);
                    continue;
                }

                if (input != currentInput) {
                    Flush(input);
                    continue;
                }

                counter += 1;
            } else {
                Flush(line.Line);
            }

            if (line.Line == current) counter += 1;
        }

        Flush(null);

        Lines.Clear();
        Lines.AddRange(newLines);
    }
}
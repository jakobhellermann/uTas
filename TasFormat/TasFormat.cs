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


    public record FrameCursorState(
        TasLine.FrameInput Input,
        TasLine.FrameInput? PreviousInput,
        List<TasLine> Other,
        int LineNumber,
        int FrameInsideLine
    ) {
        public (HashSet<Input>, HashSet<Input>) ReleasedPressed() {
            HashSet<Input> released;
            if (PreviousInput != null) {
                released = new HashSet<Input>(PreviousInput.Inputs);
                released.ExceptWith(Input.Inputs);
            } else {
                released = new HashSet<Input>();
            }

            HashSet<Input> pressed;
            if (PreviousInput != null) {
                pressed = new HashSet<Input>(Input.Inputs);
                pressed.ExceptWith(PreviousInput.Inputs);
            } else {
                pressed = new HashSet<Input>(Input.Inputs);
            }

            return (released, pressed);
        }
    }


    public FrameCursorState? GetCursorStateAt(int activeFrame) {
        var framesToSkip = activeFrame;


        var other = new List<TasLine>();
        TasLine.FrameInput? previousInput = null;

        foreach (var line in Lines) {
            if (line.Line is not TasLine.FrameInput frameInput) {
                other.Add(line.Line);
                continue;
            }

            if (frameInput.FrameCount <= framesToSkip) {
                framesToSkip -= frameInput.FrameCount;
                other.Clear();
                previousInput = frameInput;
                continue;
            }

            var frameInsideLine = framesToSkip;

            return new FrameCursorState(
                frameInput,
                previousInput,
                frameInsideLine == 0 ? other : new List<TasLine>(),
                line.LineNumber,
                frameInsideLine
            );
        }

        return null;
    }

    public void Expand() {
        for (var i = Lines.Count - 1; i >= 0; i--) {
            var info = Lines[i];
            if (info.Line is not TasLine.FrameInput { FrameCount: > 1 } input) continue;

            Lines.RemoveAt(i);

            var expanded = Enumerable.Range(0, input.FrameCount).Select(_ => {
                var line = new TasLine.FrameInput(1,
                    input.Inputs.Select(inp => new Input(inp.Key)).ToHashSet());
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
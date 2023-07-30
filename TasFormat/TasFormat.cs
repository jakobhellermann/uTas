using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uTas.TasFormat;

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
        int FrameInsideLine,
        int TasFileLineIndex
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

        for (var i = 0; i < Lines.Count; i++) {
            var line = Lines[i];
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
                frameInsideLine,
                i
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

    public bool Combine() {
        if (Lines.Count == 0) return false;

        var changed = false;
        var newLines = new List<TasLineInfo>();

        TasLine.FrameInput? current = null;
        var frameCount = 0;
        var currentLineNumber = 0;

        void Flush(TasLine newLine, int lineNumber) {
            if (current != null) {
                newLines.Add(new TasLineInfo(current with { FrameCount = frameCount }, currentLineNumber));
                current = null;
                currentLineNumber = lineNumber;
            }

            if (newLine is TasLine.FrameInput inputLine) {
                current = inputLine;
                frameCount = inputLine.FrameCount;
            } else {
                newLines.Add(new TasLineInfo(newLine, lineNumber));
            }
        }

        foreach (var line in Lines)
            if (line.Line is TasLine.FrameInput input) {
                if (current is null) {
                    current = input;
                    frameCount = input.FrameCount;
                } else if (input.Inputs.SetEquals(current.Inputs)) {
                    frameCount += input.FrameCount;
                    changed = true;
                } else {
                    Flush(input, line.LineNumber);
                }
            } else {
                Flush(line.Line, line.LineNumber);
            }

        if (current != null) newLines.Add(new TasLineInfo(current with { FrameCount = frameCount }, currentLineNumber));

        Lines.Clear();
        Lines.AddRange(newLines);

        return changed;
    }
}
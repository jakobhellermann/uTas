using System.Text;
using System.Text.RegularExpressions;

namespace TasFormat;

public readonly record struct Input(string Key) {
    public override string ToString() {
        return Key;
    }
}

// ReSharper disable once InconsistentNaming
public interface TasLine {
    string ToTasFormat();


    public record FrameInput(int FrameCount, HashSet<Input> Inputs) : TasLine {
        public override string ToString() {
            return $"FrameInput {{ FrameCount = {FrameCount}, Inputs = {string.Join(',', Inputs)} }}";
        }

        public string ToTasFormat() {
            const int align = 4;
            var comma = Inputs.Count > 0 ? "," : "";
            return $"{FrameCount,align}{comma}{string.Join(',', Inputs.Select(input => input.Key))}";
        }

        public virtual bool Equals(FrameInput? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FrameCount == other.FrameCount && Inputs.SetEquals(other.Inputs);
        }

        public override int GetHashCode() {
            return HashCode.Combine(FrameCount, Inputs);
        }
    }

    public record Call(string Method, string[] Arguments) : TasLine {
        public override string ToString() {
            return $"Call {{ Method = {Method}, Arguments = {string.Join(',', Arguments)} }}";
        }


        public string ToTasFormat() {
            return $"{Method}, {string.Join(", ", Arguments)}";
        }
    }

    public record Property(string Key, string Value) : TasLine {
        public string ToTasFormat() {
            return $"{Key}: {Value}";
        }
    }

    public record Comment(string Text) : TasLine {
        public string ToTasFormat() {
            return $"#{Text}";
        }
    }
}

public record TasLineInfo(TasLine Line, int LineNumber);

public record TasFile(List<TasLineInfo> Lines) {
    public static TasFile Parse(string file) {
        var lines = file.Split("\n");

        var tasLines = new List<TasLineInfo>();

        var lineNumber = -1;
        foreach (var line in lines) {
            lineNumber++;

            var span = line.AsSpan();
            var commentIndex = span.IndexOf('#');

            if (commentIndex != -1) {
                var beforeComment = span[..commentIndex];
                var afterComment = span[(commentIndex + 1) ..];

                if (beforeComment.Trim().IsWhiteSpace()) {
                    tasLines.Add(new TasLineInfo(new TasLine.Comment(afterComment.ToString()), lineNumber));
                    continue;
                }

                span = beforeComment;
            }

            if (span.IsWhiteSpace()) continue;

            var commaIndex = span.IndexOf(',');
            var beforeComma = commaIndex == -1 ? span : span[..commaIndex];
            var afterComma = commaIndex == -1 ? ReadOnlySpan<char>.Empty : span[(commaIndex + 1)..];

            if (int.TryParse(beforeComma, out var frameCount)) {
                var inputs = new HashSet<Input>();
                var inputStrings = afterComma.ToString().Split(",");
                foreach (var inputString in inputStrings) {
                    var inputStringTrimmed = inputString.AsSpan().Trim();
                    if (inputStringTrimmed.IsWhiteSpace()) continue;

                    if (Regex.IsMatch(inputStringTrimmed, @"^[a-zA-Z]$")) {
                        inputs.Add(new Input(inputStringTrimmed.ToString()));
                    } else {
                        throw new Exception($"unexpected input `{inputStringTrimmed}` in line `{afterComma}`");
                    }
                }

                tasLines.Add(new TasLineInfo(new TasLine.FrameInput(frameCount, inputs), lineNumber));
                continue;
            }


            var colonIndex = span.IndexOf(':');
            if (colonIndex != -1) {
                var key = span[..colonIndex].Trim();
                var value = span[(colonIndex + 1)..].Trim();
                tasLines.Add(new TasLineInfo(new TasLine.Property(key.ToString(), value.ToString()), lineNumber));
                continue;
            }


            var method = beforeComma.ToString();
            var arguments = afterComma.ToString().Split(',').Select(argument => argument.Trim()).ToArray();
            tasLines.Add(new TasLineInfo(new TasLine.Call(method, arguments), lineNumber));
        }

        return new TasFile(tasLines);
    }

    public void Expand() {
        for (var i = Lines.Count - 1; i >= 0; i--) {
            var info = Lines[i];
            if (info.Line is TasLine.FrameInput { FrameCount: > 1 } input) {
                Lines.RemoveAt(i);

                var expanded = Enumerable.Range(0, input.FrameCount).Select(_ => {
                    var line = new TasLine.FrameInput(1, input.Inputs.Select(inp => new Input(inp.Key)).ToHashSet());
                    return new TasLineInfo(line, info.LineNumber);
                });
                Lines.InsertRange(i, expanded);
            }
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

            if (line.Line == current) {
                counter += 1;
            }
        }

        Flush(null);

        Lines.Clear();
        Lines.AddRange(newLines);
    }

    public string ToTasFormat() {
        var builder = new StringBuilder();
        for (var i = 0; i < Lines.Count; i++) {
            var line = Lines[i];
            builder.Append(line.Line.ToTasFormat());
            if (i != Lines.Count - 1) {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

/*
 console a b c
 Set,A,B,C
 Invoke,A,B,C
 Assert Equal True value
 
 Block,Parameter
 <inputs>
 EndBlock
 
 Value: x
 */
}
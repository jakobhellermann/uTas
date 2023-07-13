using System.Text.RegularExpressions;

namespace TasFormat;

public readonly record struct Input(string Key) {
    public override string ToString() {
        return Key;
    }
}

// ReSharper disable once InconsistentNaming
public interface TasLine {
    public record FrameInput(int FrameCount, List<Input> Inputs) : TasLine {
        public override string ToString() {
            return $"FrameInput {{ FrameCount = {FrameCount}, Inputs = {string.Join(',', Inputs)} }}";
        }
    }

    public record Call(string Method, string[] Arguments) : TasLine {
        public override string ToString() {
            return $"Call {{ Method = {Method}, Arguments = {string.Join(',', Arguments)} }}";
        }
    }


    public record Property(string Key, string Value) : TasLine;

    public record Comment(string Text) : TasLine;
}

public record TasLineInfo(TasLine Line, int LineNumber);

public record TasFile(List<TasLineInfo> Lines);

public class Parser {
    public static TasFile Parse(string file) {
        var lines = file.Split("\n");

        List<TasLineInfo> tasLines = new List<TasLineInfo>();

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
                var inputs = new List<Input>();
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

        Console.WriteLine(string.Join('\n', tasLines.Select(line => line.ToString())));
        return new TasFile(tasLines);
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
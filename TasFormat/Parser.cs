using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// ReSharper disable ReplaceSubstringWithRangeIndexer net472 compat :(

namespace TasFormat;

public partial record TasFile {
    public static TasFile Parse(string file) {
        var lines = file.Split('\n');

        var tasLines = new List<TasLineInfo>();

        var lineNumber = 0;
        foreach (var l in lines) {
            var line = l;
            lineNumber++;

            var commentIndex = line.IndexOf('#');

            if (commentIndex != -1) {
                var beforeComment = line.Substring(0, commentIndex);
                var afterComment = line.Substring(commentIndex + 1);

                if (beforeComment.Trim().IsWhiteSpace()) {
                    tasLines.Add(new TasLineInfo(new TasLine.Comment(afterComment), lineNumber));
                    continue;
                }

                line = beforeComment;
            }

            if (line.IsWhiteSpace()) continue;

            var commaIndex = line.IndexOf(',');
            var beforeComma = commaIndex == -1 ? line : line.Substring(0, commaIndex);
            var afterComma = commaIndex == -1 ? "" : line.Substring(commaIndex + 1);

            if (int.TryParse(beforeComma, out var frameCount)) {
                var inputs = new HashSet<Input>();
                var inputStrings = afterComma.Split(',');
                foreach (var inputString in inputStrings) {
                    var inputStringTrimmed = inputString.Trim();
                    if (inputStringTrimmed.IsWhiteSpace()) continue;

                    if (Regex.IsMatch(inputStringTrimmed, @"^[a-zA-Z]$"))
                        inputs.Add(new Input(inputStringTrimmed));
                    else
                        throw new Exception($"unexpected input `{inputStringTrimmed}` in line `{afterComma}`");
                }

                tasLines.Add(new TasLineInfo(new TasLine.FrameInput(frameCount, inputs), lineNumber));
                continue;
            }


            var colonIndex = line.IndexOf(':');
            if (colonIndex != -1) {
                var key = line.Substring(0, colonIndex);
                var value = line.Substring(colonIndex + 1);
                tasLines.Add(new TasLineInfo(new TasLine.Property(key, value), lineNumber));
                continue;
            }


            var method = beforeComma;
            var arguments = afterComma.Split(',').Select(argument => argument.Trim()).ToArray();
            tasLines.Add(new TasLineInfo(new TasLine.Call(method, arguments), lineNumber));
        }

        return new TasFile(tasLines);
    }
}

internal static class Extensions {
    public static bool IsWhiteSpace(this string s) {
        return s.All(k => k is ' ' or '\t');
    }
}
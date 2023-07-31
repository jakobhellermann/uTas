using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// ReSharper disable ReplaceSubstringWithRangeIndexer net472 compat :(

namespace uTas.TasFormat;

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

            var lineTrimmed = line.TrimStart();
            if (lineTrimmed.StartsWith("***")) {
                var afterBreakpoint = line.Substring(3).TrimStart();
                var couldParse = float.TryParse(afterBreakpoint, out var factor);
                tasLines.Add(new TasLineInfo(new TasLine.Breakpoint(couldParse ? factor : null), lineNumber));

                continue;
            }

            var commaIndex = line.IndexOf(',');
            var beforeComma = commaIndex == -1 ? line : line.Substring(0, commaIndex);
            var afterComma = commaIndex == -1 ? "" : line.Substring(commaIndex + 1);

            if (int.TryParse(beforeComma, out var frameCount)) {
                var inputs = new HashSet<Input>();
                var inputStrings = afterComma.SplitWithBalancedParenthesis();
                foreach (var inputString in inputStrings) {
                    var inputStringTrimmed = inputString.Trim();
                    if (inputStringTrimmed.IsWhiteSpace()) continue;

                    if (Regex.Match(inputStringTrimmed, @"^([a-zA-Z])\(([^)]*)\)$") is { Success: true } match) {
                        var action = match.Groups[1];
                        var content = match.Groups[2];

                        var values = content.ToString().Split(',')
                            .Select(valueString => {
                                if (double.TryParse(valueString.Trim(), out var value))
                                    return value;
                                else
                                    throw new ArgumentException(
                                        $"Cannot parse '{inputStringTrimmed}' as action: '{valueString}' is not a double"
                                    );
                            })
                            .ToList();

                        inputs.Add(new Input(action.ToString(), values));
                    } else if (Regex.IsMatch(inputStringTrimmed, @"^[a-zA-Z]$")) {
                        inputs.Add(new Input(inputStringTrimmed));
                    } else {
                        throw new Exception($"unexpected input `{inputStringTrimmed}` in line `{afterComma}`");
                    }
                }

                tasLines.Add(
                    new TasLineInfo(new TasLine.FrameInput(frameCount, inputs), lineNumber)
                );
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


    public static List<string> SplitWithBalancedParenthesis(this string input) {
        var result = new List<string>();
        var parenthesisCount = 0;
        var startIndex = 0;

        var addItem = (string item) => {
            item = item.Trim();
            result.Add(item);
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

        addItem(input.Substring(startIndex));

        return result;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace uTas.TasFormat;

// ReSharper disable once InconsistentNaming
public interface TasLine {
    string ToTasFormat();


    public record Call(string Method, string[] Arguments) : TasLine {
        public override string ToString() => $"Call {{ Method = {Method}, Arguments = {string.Join(",", Arguments)} }}";


        public string ToTasFormat() => $"{Method}, {string.Join(", ", Arguments)}";
    }

    public record Property(string Key, string Value) : TasLine {
        public string ToTasFormat() => $"{Key}: {Value}";
    }

    public record Comment(string Text) : TasLine {
        public string ToTasFormat() => $"#{Text}";
    }

    public record Breakpoint(float? Factor) : TasLine {
        public string ToTasFormat() => $"***{(Factor != null ? $",{Factor}" : "")}";
    }

    public record FrameInput(int FrameCount, HashSet<Input> Inputs) : TasLine {
        public override string ToString() =>
            $"FrameInput {{ FrameCount = {FrameCount}, Inputs = {(Inputs.Count == 0 ? "()" : string.Join(", ", Inputs))} }}";

        public string ToTasFormat() {
            const int align = 4;
            var comma = Inputs.Count > 0 ? "," : "";
            return $"{FrameCount,align}{comma}{string.Join(",", Inputs.Select(input => input.Key))}";
        }

        public virtual bool Equals(FrameInput? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return FrameCount == other.FrameCount && Inputs.SetEquals(other.Inputs);
        }

        public override int GetHashCode() {
            unchecked {
                return (FrameCount * 397) ^ Inputs.GetHashCode();
            }
        }
    }
}

public record Input(string Key, List<double> Values) {
    public Input(string Key) : this(Key, new List<double>()) {
    }

    public override string ToString() => Values.Count == 0 ? Key : $"{Key}({string.Join(",", Values)})";

    public virtual bool Equals(Input? other) {
        if (other is null) return false;

        return this.Key == other.Key && this.Values.SequenceEqual(other.Values);
    }

    public override int GetHashCode() {
        unchecked {
            var key = Key.GetHashCode() * 379;

            var hash = 19;
            foreach (var item in Values) {
                hash = hash * 31 + item.GetHashCode();
            }

            return key ^ hash;
        }
    }
}
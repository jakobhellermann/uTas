using System.Collections.Generic;
using System.Linq;

namespace TasFormat;

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

public readonly record struct Input(string Key) {
    public override string ToString() => Key;
}
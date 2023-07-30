using System;
using NUnit.Framework;

namespace uTas.TasFormat;

public class TasFormatTests {
    [Test]
    public void Expand() {
        const string input = "4,R,U,X\n2,R";
        const string expected = """
   1,R,U,X
   1,R,U,X
   1,R,U,X
   1,R,U,X
   1,R
   1,R
""";
        var inputs = TasFile.Parse(input);
        inputs.Expand();
        var result = inputs.ToTasFormat();

        Assert.AreEqual(expected, result);
    }

    [Test]
    public void ExpandImmutable() {
        const string input = "2,R";
        var inputs = TasFile.Parse(input);
        inputs.Expand();
        if (inputs.Lines[1].Line is TasLine.FrameInput i) i.Inputs.Add(new Input("U"));

        Console.WriteLine(inputs);

        Assert.AreEqual("   1,R\n   1,R,U", inputs.ToTasFormat());
    }


    [Test]
    public void Combine() {
        const string input = """
           2,R
           3,R
           2,R,J
           1,R
        """;
        const string expected = """
           5,R
           2,R,J
           1,R
        """;
        var inputs = TasFile.Parse(input);
        inputs.Combine();
        var result = inputs.ToTasFormat();

        Assert.AreEqual(expected, result);
    }


    [Test]
    public void CombineWithNoninputlines() {
        const string input = """
        Set, Something, 42
           2,R
        Set, Player.Position, 10
           3,R
           2,R,J
           1,R
        """;
        var inputs = TasFile.Parse(input);
        inputs.Combine();
        var result = inputs.ToTasFormat();

        Assert.AreEqual(input, result);
    }


    [Test]
    public void ExpandCollapseRoundtrip() {
        const string input = """
           4,R,U,X
           2,R
        """;
        var inputs = TasFile.Parse(input);
        inputs.Expand();
        inputs.Combine();
        var result = inputs.ToTasFormat();

        Assert.AreEqual(input, result);
    }
}
using NUnit.Framework;

namespace TasFormat;

public class ParserTests {
    [Test]
    public void Test() {
        var exampleTas = @"RecordCount: 500
console load 1
   1 # comment

Set, Player.Speed.X, 100

#Start
  88
#lvl_1
   5,R,X
   6,R,J
";

        TasFile.Parse(exampleTas);
    }

    [Test]
    public void ParsesWithoutException() {
        var inputs = new[] {
            @"#21:28.209
#Start from Begin

Read,StartFullGameFile

Read,0 - Prologue,Start

Read,LoadANoCollects,Prologue

Read,1A,Start

Read,LoadANoCollects

Read,2A,Start

Read,LoadANoCollects

Read,3A,Start

Read,LoadA

Read,4A,Start

Read,LoadANoCollects

Read,5HC,Start,lvl_b-17 (1)

#lvl_b-17
   3,R,D
   1,R,J
   1,R
  11,R,D,X
   2,R,J,G
  10,R
   3,R,J
   5,R
  40

#lvl_b-22
  14,R,D,X
   1,L,J
   6,L,U,X
   3,R,J
   7,R
  15,R,D,X
  15,D,C
   2
   6,R,X
  44,R,C
  36,G
   1
   2,J
   5,R,Z
   4,R,J,G
   6,R
   9,R,J
   4
  15,R,D,X
  15,D,C
  10,L,X
   6,R,J
   1,R
  15,R,X
  10,R,C
   9,R,J
   7,R
  15,U,Z
   1,L,J,G
   1,R,J
   1,L,J
   1,R
   6,L
   4,Z
   1,L,J
   6,L
   2,L,D,J
   4,L
  14,L,X
   1,L,J
  15,D,C
  15,D,X
  14,L,Z
   1,R,J
  13,R,Z
   1,L,J
   1,R
  14,L,D,X
   1,L,J
   3,L,K,G
  15,L,D,X
   1,L
   8,L,J
 257
   1,J

Read,LoadBFromA

Read,5B,Start

Read,LoadAFromB

Read,6HC,Start,Return

Read,LoadBFromA

Read,6B,Start

Read,LoadAFromB,0,Summit
#Summit
  45
Add 29
   1,J
  32
Add 1

Read,7A,Start

Read,LoadCoreFromSummit,0,Core
 300
   1,J
Add 58
 427

Read,LoadJournal,0,Any%
"
        };

        foreach (var input in inputs) {
            TasFile.Parse(input);
        }
    }

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
        if (inputs.Lines[1].Line is TasLine.FrameInput i) {
            i.Inputs.Add(new Input("U"));
        }

        Console.WriteLine(inputs);

        Assert.AreEqual("   1,R\n   1,R,U", inputs.ToTasFormat());
    }


    [Test]
    public void ExpandCollapseRoundtrip() {
        const string input = "   4,R,U,X\n   2,R";
        var inputs = TasFile.Parse(input);
        inputs.Expand();
        inputs.Collapse();
        var result = inputs.ToTasFormat();

        Assert.AreEqual(input, result);
    }
}
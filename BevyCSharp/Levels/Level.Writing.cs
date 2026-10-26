using System.Text;

namespace Bevy;

// Writing map text as TrenchBroom lays it out, the game and the format in the first two comments,
// each entity and each brush after a comment numbering it, a keyvalue and a face a line, and each
// number as the shortest text that reads back as the same double, so what this writes reads and
// writes back byte for byte.
public sealed partial class Level
{
    /// <summary>The level as map text in the Valve 220 format, as TrenchBroom lays it out.</summary>
    public string Write()
    {
        var text = new StringBuilder();
        text.Append("// Game: ").Append(Game).Append('\n');
        text.Append("// Format: Valve\n");
        for (var e = 0; e < Entities.Count; e++)
        {
            var entity = Entities[e];
            text.Append("// entity ").Append(e).Append("\n{\n");
            foreach (var (key, value) in entity.Keys) text.Append(Quote(key)).Append(' ').Append(Quote(value)).Append('\n');
            for (var b = 0; b < entity.Brushes.Count; b++)
            {
                text.Append("// brush ").Append(b).Append("\n{\n");
                foreach (var face in entity.Brushes[b].Faces) Line(text, face);
                text.Append("}\n");
            }

            text.Append("}\n");
        }

        return text.ToString();
    }

    private static void Line(StringBuilder text, LevelFace face)
    {
        static string N(double value) => LevelPoint.Number(value);
        text.Append("( ").Append(face.First).Append(" ) ( ").Append(face.Second).Append(" ) ( ").Append(face.Third).Append(" ) ")
            .Append(face.Material)
            .Append(" [ ").Append(face.UAxis).Append(' ').Append(N(face.UOffset)).Append(" ] [ ")
            .Append(face.VAxis).Append(' ').Append(N(face.VOffset)).Append(" ] ")
            .Append(N(face.Rotation)).Append(' ').Append(N(face.ScaleU)).Append(' ').Append(N(face.ScaleV)).Append('\n');
    }

    private static string Quote(string text) => "\"" + text.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}

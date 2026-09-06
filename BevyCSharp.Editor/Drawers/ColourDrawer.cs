using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// Three numbers that are a colour: what it comes to, and the numbers under it.
/// </summary>
/// <remarks>
/// <para>
/// The first row says what the three numbers come to, as the six digits everybody who has ever
/// picked a colour already reads. Nobody reads 0.8, 0.2, 0.15 as a shade of red, and an inspector
/// that makes somebody run the game to find out what colour they set is one they stop using.
/// </para>
/// <para>
/// A patch of the colour itself would be better and is not possible: painting an element makes
/// the interface restyle it, and a restyle undoes the panel's own decisions about what the rest of
/// the row is showing.
/// </para>
/// </remarks>
public sealed class ColourDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) =>
        field.Hints.Colour && field.Kind == FieldKind.Vec3;

    /// <inheritdoc/>
    /// <remarks>One for the patch, and one for each of the three numbers.</remarks>
    public int Lines(ComponentField field) => 4;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        var colour = target.Read() as Vec3? ?? default;

        if (part == 0)
        {
            row.Name(target.Field.Title);
            row.Box(Digits(colour), null, editable: false);
            return;
        }

        row.Name(string.Empty);
        row.Box(
            EditorFields.Text(Part(colour, part - 1)),
            Grips.Axis(part - 1),
            target.Field.IsWritable);
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (part == 0 || !target.Field.IsWritable) return;

        var typed = row.Typed.Trim();
        if (typed.Length == 0) return;
        if (!EditorFields.TryNumber(typed, out var value)) return;

        var colour = target.Read() as Vec3? ?? default;
        if (Math.Abs(Part(colour, part - 1) - value) < 0.0001d) return;

        target.Write(With(colour, part - 1, (float)value));
    }

    /// <inheritdoc/>
    public double? Number(int part, FieldTarget target) => part == 0
        ? null
        : Part(target.Read() as Vec3? ?? default, part - 1);

    /// <inheritdoc/>
    public void Nudge(int part, FieldTarget target, double value)
    {
        if (part == 0) return;

        var colour = target.Read() as Vec3? ?? default;
        target.Write(With(colour, part - 1, (float)value));
    }

    /// <inheritdoc/>
    /// <remarks>A hundredth of the way from black to bright per pixel.</remarks>
    public float Step(int part, FieldTarget target) =>
        part == 0 ? 0f : (float)(target.Field.Hints.Step ?? 0.005d);

    /// <summary>One of the three numbers.</summary>
    private static double Part(Vec3 colour, int part) => part switch
    {
        1 => colour.Y,
        2 => colour.Z,
        _ => colour.X,
    };

    /// <summary>The same colour with one of its numbers changed.</summary>
    private static Vec3 With(Vec3 colour, int part, float value) => part switch
    {
        1 => new Vec3(colour.X, value, colour.Z),
        2 => new Vec3(colour.X, colour.Y, value),
        _ => new Vec3(value, colour.Y, colour.Z),
    };

    /// <summary>
    /// The colour as the six digits everybody reads.
    /// </summary>
    /// <remarks>
    /// Clamped, because a colour that is a light's tint can be brighter than one and six digits
    /// cannot say so. The three numbers underneath are the truth; this is the part a person
    /// recognises.
    /// </remarks>
    private static string Digits(Vec3 colour) =>
        $"#{Byte(colour.X):X2}{Byte(colour.Y):X2}{Byte(colour.Z):X2}";

    /// <summary>One channel as a byte, as a screen would show it.</summary>
    private static int Byte(float channel) =>
        (int)Math.Round(Math.Clamp(channel, 0f, 1f) * 255f);
}

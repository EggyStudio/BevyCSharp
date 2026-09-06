using Bevy;
using BevyCSharp.Editor.Framework;
using BevyCSharp.Editor.Panels;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// Three numbers that are a colour: the colour itself, and the numbers under it.
/// </summary>
/// <remarks>
/// <para>
/// The first row is the colour, as a patch of it. Nobody reads 0.8, 0.2, 0.15 as a shade of red,
/// and an inspector that makes somebody run the game to find out what colour they set is one they
/// stop using. Pressing it opens a picker, which is what a patch of colour does everywhere else.
/// </para>
/// <para>
/// The three numbers sit under it on one line, because they are one value and because the patch
/// above them has already said what they come to. They are still the truth: a colour that is a
/// light's tint can be brighter than white, and a patch cannot show that.
/// </para>
/// </remarks>
public sealed class ColourDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) =>
        field.Hints.Colour && field.Kind == FieldKind.Vec3;

    /// <inheritdoc/>
    /// <remarks>One for the patch, one for the three numbers beside each other.</remarks>
    public int Lines(ComponentField field) => 2;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        var colour = target.Read() as Vec3? ?? default;

        if (part == 0)
        {
            row.Name(target.Field.Title);
            row.Swatch(Packed(colour));
            return;
        }

        row.Name(string.Empty);

        for (var channel = 0; channel < 3; channel++)
        {
            row.Box(
                channel,
                EditorFields.Text(Part(colour, channel)),
                Grips.Axis(channel),
                target.Field.IsWritable);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Pressing the patch asks for a colour. What answers is a panel like any other, so the same
    /// picker serves a light's tint, a material's base and anything a game adds later.
    /// </remarks>
    public void Press(InspectorRow row, int part, FieldTarget target)
    {
        if (part != 0 || !target.Field.IsWritable) return;

        var colour = target.Read() as Vec3? ?? default;
        var (x, y) = row.Below;

        ColourPanel.Ask(target.Field.Title, colour, x, y, picked => target.Write(picked));
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (part == 0 || !target.Field.IsWritable) return;

        var colour = target.Read() as Vec3? ?? default;
        var changed = colour;
        var moved = false;

        for (var channel = 0; channel < 3; channel++)
        {
            var typed = row.TypedIn(channel).Trim();

            if (typed.Length == 0) continue;
            if (!EditorFields.TryNumber(typed, out var value)) continue;
            if (Math.Abs(Part(changed, channel) - value) < 0.0001d) continue;

            changed = With(changed, channel, (float)value);
            moved = true;
        }

        if (moved) target.Write(changed);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The row of numbers is three of them, so which one is being dragged arrives as the part past
    /// the row's own: one for red, two for green, three for blue.
    /// </remarks>
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
    /// <remarks>A two hundredth of the way from black to bright per pixel.</remarks>
    public float Step(int part, FieldTarget target) =>
        part == 0 ? 0f : (float)(target.Field.Hints.Step ?? 0.005d);

    /// <summary>The colour as one number, which is how a patch of it is painted.</summary>
    private static uint Packed(Vec3 colour) =>
        ((uint)Byte(colour.X) << 24)
        | ((uint)Byte(colour.Y) << 16)
        | ((uint)Byte(colour.Z) << 8)
        | 0xFFu;

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
    internal static string Digits(Vec3 colour) =>
        $"#{Byte(colour.X):X2}{Byte(colour.Y):X2}{Byte(colour.Z):X2}";

    /// <summary>One channel as a byte, as a screen would show it.</summary>
    private static int Byte(float channel) =>
        (int)Math.Round(Math.Clamp(channel, 0f, 1f) * 255f);
}

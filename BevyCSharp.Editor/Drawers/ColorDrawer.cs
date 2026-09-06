using Bevy;
using BevyCSharp.Editor.Framework;
using BevyCSharp.Editor.Panels;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// Three numbers that are a color: the color itself, and the numbers under it.
/// </summary>
/// <remarks>
/// <para>
/// The first row is the color, as a patch of it. Nobody reads 0.8, 0.2, 0.15 as a shade of red,
/// and an inspector that makes somebody run the game to find out what color they set is one they
/// stop using. Pressing it opens a picker, which is what a patch of color does everywhere else.
/// </para>
/// <para>
/// The three numbers sit under it on one line, because they are one value and because the patch
/// above them has already said what they come to. They are still the truth: a color that is a
/// light's tint can be brighter than white, and a patch cannot show that.
/// </para>
/// </remarks>
public sealed class ColorDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) =>
        field.Hints.Color && field.Kind == FieldKind.Vec3;

    /// <inheritdoc/>
    /// <remarks>One for the patch, one for the three numbers beside each other.</remarks>
    public int Lines(ComponentField field) => 2;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        var color = target.Read() as Vec3? ?? default;

        if (part == 0)
        {
            row.Name(target.Field.Title);
            row.Swatch(Packed(color));
            return;
        }

        row.Name(string.Empty);

        for (var channel = 0; channel < 3; channel++)
        {
            row.Box(
                channel,
                EditorFields.Text(Part(color, channel)),
                Grips.Axis(channel),
                target.Field.IsWritable);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Pressing the patch asks for a color. What answers is a panel like any other, so the same
    /// picker serves a light's tint, a material's base and anything a game adds later.
    /// </remarks>
    public void Press(InspectorRow row, int part, FieldTarget target)
    {
        if (part != 0 || !target.Field.IsWritable) return;

        var color = target.Read() as Vec3? ?? default;
        var (x, y) = row.Below;

        ColorPanel.Ask(target.Field.Title, color, x, y, picked => target.Write(picked));
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (part == 0 || !target.Field.IsWritable) return;

        var color = target.Read() as Vec3? ?? default;
        var changed = color;
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

        var color = target.Read() as Vec3? ?? default;
        target.Write(With(color, part - 1, (float)value));
    }

    /// <inheritdoc/>
    /// <remarks>A two hundredth of the way from black to bright per pixel.</remarks>
    public float Step(int part, FieldTarget target) =>
        part == 0 ? 0f : (float)(target.Field.Hints.Step ?? 0.005d);

    /// <summary>The color as one number, which is how a patch of it is painted.</summary>
    private static uint Packed(Vec3 color) =>
        ((uint)Byte(color.X) << 24)
        | ((uint)Byte(color.Y) << 16)
        | ((uint)Byte(color.Z) << 8)
        | 0xFFu;

    /// <summary>One of the three numbers.</summary>
    private static double Part(Vec3 color, int part) => part switch
    {
        1 => color.Y,
        2 => color.Z,
        _ => color.X,
    };

    /// <summary>The same color with one of its numbers changed.</summary>
    private static Vec3 With(Vec3 color, int part, float value) => part switch
    {
        1 => new Vec3(color.X, value, color.Z),
        2 => new Vec3(color.X, color.Y, value),
        _ => new Vec3(value, color.Y, color.Z),
    };

    /// <summary>
    /// The color as the six digits everybody reads.
    /// </summary>
    /// <remarks>
    /// Clamped, because a color that is a light's tint can be brighter than one and six digits
    /// cannot say so. The three numbers underneath are the truth; this is the part a person
    /// recognises.
    /// </remarks>
    internal static string Digits(Vec3 color) =>
        $"#{Byte(color.X):X2}{Byte(color.Y):X2}{Byte(color.Z):X2}";

    /// <summary>One channel as a byte, as a screen would show it.</summary>
    private static int Byte(float channel) =>
        (int)Math.Round(Math.Clamp(channel, 0f, 1f) * 255f);
}

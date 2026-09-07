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
/// One row, and no numbers under it. Three channels between nought and one are not what anybody
/// came to read: what a color field is asked is "which color", and the answer is the picker. The
/// numbers are still reachable, in the picker, where they belong.
/// </para>
/// </remarks>
public sealed class ColorDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) =>
        field.Hints.Color && field.Kind == FieldKind.Vec3;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        row.Name(target.Field.Title);
        row.Swatch(Packed(target.Read() as Vec3? ?? default));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Pressing the patch asks for a color. What answers is a panel like any other, so the same
    /// picker serves a light's tint, a material's base and anything a game adds later.
    /// </remarks>
    public void Press(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        var color = target.Read() as Vec3? ?? default;
        var (x, y) = row.Below;

        ColorPanel.Ask(target.Field.Title, color, x, y, picked => target.Write(picked));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Nothing to read back. A patch of color is pressed, not typed into, and what the picker
    /// chooses it writes itself.
    /// </remarks>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
    }

    /// <summary>The color as one number, which is how a patch of it is painted.</summary>
    private static uint Packed(Vec3 color) =>
        ((uint)Byte(color.X) << 24)
        | ((uint)Byte(color.Y) << 16)
        | ((uint)Byte(color.Z) << 8)
        | 0xFFu;

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

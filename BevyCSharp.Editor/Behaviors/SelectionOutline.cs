using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Behaviors;

/// <summary>
/// Draws a box around whatever is selected.
/// </summary>
/// <remarks>
/// <para>
/// Without this, selecting something in the viewport changes two lines of text in a panel and
/// nothing where the person is looking. The box is the whole point of clicking a thing rather
/// than a row in a list.
/// </para>
/// <para>
/// Bounds come from the engine, which computes them for everything it draws, so the box is the
/// object's own rather than a guess from its transform: a scaled, rotated mesh gets a box around
/// where it actually is. An entity the engine draws nothing for, a camera or a light, has no
/// bounds, and nothing is drawn rather than a box around a point.
/// </para>
/// </remarks>
[Behavior]
public partial struct SelectionOutline
{
    /// <summary>The accent, matching the one the panels use.</summary>
    private static readonly (float R, float G, float B, float A) Accent = (0.30f, 0.49f, 1f, 1f);

    /// <summary>Draws the box, once a frame, for as long as something is selected.</summary>
    [OnUpdate]
    public static void Draw(BehaviorContext ctx)
    {
        if (!App.HasRenderer) return;
        if (!EditorSelection.Any) return;
        if (!Render.TryGetBounds(EditorSelection.Current, out var min, out var max)) return;

        // Twelve edges, written as three groups of four parallel lines, which is the order that
        // makes a mistake in one of them obvious.
        for (var i = 0; i < 4; i++)
        {
            var y = (i & 1) == 0 ? min.Y : max.Y;
            var z = (i & 2) == 0 ? min.Z : max.Z;
            Gizmos.Line(new Vec3(min.X, y, z), new Vec3(max.X, y, z), Accent);
        }

        for (var i = 0; i < 4; i++)
        {
            var x = (i & 1) == 0 ? min.X : max.X;
            var z = (i & 2) == 0 ? min.Z : max.Z;
            Gizmos.Line(new Vec3(x, min.Y, z), new Vec3(x, max.Y, z), Accent);
        }

        for (var i = 0; i < 4; i++)
        {
            var x = (i & 1) == 0 ? min.X : max.X;
            var y = (i & 2) == 0 ? min.Y : max.Y;
            Gizmos.Line(new Vec3(x, y, min.Z), new Vec3(x, y, max.Z), Accent);
        }
    }
}

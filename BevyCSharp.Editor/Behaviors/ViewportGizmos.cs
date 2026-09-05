using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Behaviors;

/// <summary>
/// What the editor draws into the scene: the selection, its handles, and which way the camera is
/// pointing.
/// </summary>
/// <remarks>
/// <para>
/// Without this, selecting something changes two lines of text in a panel and nothing where the
/// person is looking. The box is the whole point of clicking a thing rather than a row in a list,
/// and the handles are what makes a tool visible before it is used.
/// </para>
/// <para>
/// Everything here is lines in the world. There is no overlay to draw into, so the orientation
/// cross is drawn as a small set of axes a fixed distance in front of the camera, scaled to that
/// distance, which reads as a corner widget while being an ordinary thing in the scene.
/// </para>
/// </remarks>
[Behavior]
public partial struct ViewportGizmos
{
    /// <summary>The accent, matching the one the panels use.</summary>
    private static readonly (float R, float G, float B, float A) Accent = (0.30f, 0.49f, 1f, 1f);

    /// <summary>Red, green and blue for X, Y and Z, which is what every editor uses.</summary>
    private static readonly (float R, float G, float B, float A)[] AxisColours =
    [
        (0.90f, 0.25f, 0.28f, 1f),
        (0.45f, 0.85f, 0.30f, 1f),
        (0.28f, 0.55f, 0.95f, 1f),
    ];

    /// <summary>What a handle is drawn along.</summary>
    internal static readonly Vec3[] Axes = [Vec3.UnitX, Vec3.UnitY, Vec3.UnitZ];

    /// <summary>
    /// Draws the box, the handles and the orientation cross.
    /// </summary>
    /// <remarks>
    /// At the end of the frame rather than during the update, because the bounds a box is drawn
    /// from come from the global transform, and that is propagated after the update: drawing
    /// earlier draws where the thing was, which reads as a gizmo lagging behind what it is on.
    /// </remarks>
    [OnLast]
    public static void Draw(BehaviorContext ctx)
    {
        if (!App.HasRenderer) return;

        Orientation(ctx);

        if (!EditorSelection.Any) return;

        var entity = EditorSelection.Current;
        if (!Render.TryGetBounds(entity, out var min, out var max)) return;

        var centre = (min + max) * 0.5f;

        Outline(min, max);
        Handles(ctx, centre, Reach(EditorSelection.Camera, centre));
    }

    /// <summary>Draws the twelve edges of the selection's box.</summary>
    private static void Outline(Vec3 min, Vec3 max)
    {
        // Three groups of four parallel lines, which is the order that makes a mistake in one of
        // them obvious.
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

    /// <summary>Draws the tool's handles at the selection.</summary>
    private static void Handles(BehaviorContext ctx, Vec3 centre, float reach)
    {
        if (EditorTools.Current == EditorTool.Select) return;

        var held = TransformGizmo.Axis;

        for (var i = 0; i < 3; i++)
        {
            var colour = held == i ? Accent : AxisColours[i];
            var axis = Axes[i];

            switch (EditorTools.Current)
            {
                case EditorTool.Rotate:
                    Circle(centre, axis, reach, colour);
                    break;

                case EditorTool.Scale:
                    Gizmos.Line(centre, centre + (axis * reach), colour);
                    Gizmos.Sphere(centre + (axis * reach), reach * 0.08f, colour);
                    break;

                default:
                    Gizmos.Line(centre, centre + (axis * reach), colour);
                    Arrow(centre + (axis * reach), axis, reach * 0.12f, colour);
                    break;
            }
        }
    }

    /// <summary>Draws a ring about an axis, which is what a turn is dragged along.</summary>
    private static void Circle(
        Vec3 centre, Vec3 axis, float radius, (float R, float G, float B, float A) colour)
    {
        var (first, second) = Perpendiculars(axis);
        const int Steps = 32;

        var previous = centre + (first * radius);

        for (var i = 1; i <= Steps; i++)
        {
            var angle = i / (float)Steps * MathF.Tau;
            var point = centre
                + (first * (MathF.Cos(angle) * radius))
                + (second * (MathF.Sin(angle) * radius));

            Gizmos.Line(previous, point, colour);
            previous = point;
        }
    }

    /// <summary>Draws the head of an arrow, as four lines back from its point.</summary>
    private static void Arrow(
        Vec3 tip, Vec3 direction, float size, (float R, float G, float B, float A) colour)
    {
        var (first, second) = Perpendiculars(direction);
        var back = tip - (direction * size);

        Gizmos.Line(tip, back + (first * size * 0.4f), colour);
        Gizmos.Line(tip, back - (first * size * 0.4f), colour);
        Gizmos.Line(tip, back + (second * size * 0.4f), colour);
        Gizmos.Line(tip, back - (second * size * 0.4f), colour);
    }

    /// <summary>
    /// A small set of axes in front of the camera, showing which way it is pointing.
    /// </summary>
    /// <remarks>
    /// Drawn in the world because there is nowhere else to draw: everything the editor puts in the
    /// viewport is a line in the scene. Placing it a fixed distance ahead and to one side, and
    /// sizing it by that distance, makes it sit still in the corner however the camera moves.
    /// </remarks>
    private static void Orientation(BehaviorContext ctx)
    {
        var camera = EditorSelection.Camera;
        if (camera.IsNone) return;

        // Where it goes is the interface's answer, not a guess: the toolbar reserves an empty
        // square in the bottom right and reports where the layout put it, so the cross sits beside
        // the buttons and moves with them. The viewport's own corner is the fallback for a frame
        // before the bar has been measured, or with the bar closed.
        var viewport = EditorShell.Layout.Viewport;
        if (viewport.Width < 1f) return;

        const float Inset = 74f;
        const float Fallback = 48f;

        var (x, y) = EditorGizmoSlot.Known
            ? EditorGizmoSlot.Centre
            : (viewport.Right - Inset, viewport.Bottom - Inset);

        var square = EditorGizmoSlot.Known ? EditorGizmoSlot.Size : Fallback;

        if (!Render.TryRay(camera, x, y, out var origin, out var direction)) return;

        // Close enough to the camera that nothing in the scene can get in front of it. Drawn in
        // the world, a cross two metres out is behind the first wall the camera flies up to; a few
        // centimetres out is inside everything.
        const float Ahead = 0.3f;

        var centre = origin + (direction * Ahead);

        // How long an arm has to be, asked rather than assumed: the ray through a point half the
        // square away lands somewhere at the same depth, and how far that is from the centre is
        // exactly what a half-square measures in the world. No field of view appears here, so a
        // camera set up any way at all draws a cross the size of its square.
        if (!Render.TryRay(camera, x + (square * 0.42f), y, out var edge, out var sideways)) return;

        var size = (edge + (sideways * Ahead) - centre).Length;

        // Six arms rather than three. Three says which way X, Y and Z point and leaves a person to
        // work out where the other halves went; six is the widget every editor draws, and the
        // negative halves are dimmed so the positive ones are still the ones read first.
        for (var i = 0; i < 3; i++)
        {
            var (r, g, b, _) = AxisColours[i];
            var arm = Axes[i] * size;

            Gizmos.Line(centre, centre + arm, AxisColours[i]);
            Gizmos.Sphere(centre + arm, size * 0.18f, AxisColours[i]);
            Gizmos.Line(centre, centre - arm, (r * 0.45f, g * 0.45f, b * 0.45f, 1f));
        }
    }

    /// <summary>
    /// How far a handle reaches: whatever a fixed number of pixels is worth where the thing is.
    /// </summary>
    /// <remarks>
    /// A handle is a control, not a part of the scene, and a control is the size a hand needs it
    /// to be. Sized from the object it is on, a handle on a coin is too small to grab and one on a
    /// building fills the screen; sized in metres it shrinks to nothing as the camera backs away.
    /// It also has to be a size that does not change while it is being used, and the object's own
    /// bounds change with every frame of a scale drag.
    /// </remarks>
    internal static float Reach(Entity camera, Vec3 centre)
    {
        if (camera.IsNone) return Fallback;
        if (!Render.TryProject(camera, centre, out var screenX, out var screenY)) return Fallback;
        if (!Render.TryRay(camera, screenX, screenY, out var origin, out var forward))
        {
            return Fallback;
        }

        var depth = Vec3.Dot(centre - origin, forward);
        if (depth <= 0f) return Fallback;

        // The ray through a point a fixed number of pixels away, taken to the same depth. How far
        // that lands from the centre is what those pixels are worth in the world there, which is
        // the whole calculation: no field of view is assumed, so a camera set up any way at all
        // gets a handle the size it asked for.
        if (!Render.TryRay(camera, screenX + Pixels, screenY, out var edge, out var sideways))
        {
            return Fallback;
        }

        return (edge + (sideways * depth) - centre).Length;
    }

    /// <summary>How long a handle is on screen, in logical pixels.</summary>
    private const float Pixels = 90f;

    /// <summary>What a handle reaches when the camera cannot be asked.</summary>
    private const float Fallback = 1f;

    /// <summary>Two directions at right angles to an axis and to each other.</summary>
    internal static (Vec3 First, Vec3 Second) Perpendiculars(Vec3 axis)
    {
        // Crossing with whichever world axis this one is least aligned to, so the result is never
        // the zero vector.
        var helper = MathF.Abs(axis.Y) < 0.9f ? Vec3.UnitY : Vec3.UnitX;

        var first = Vec3.Cross(helper, axis).Normalized;
        return (first, Vec3.Cross(axis, first));
    }
}

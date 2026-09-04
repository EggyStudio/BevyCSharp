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

    /// <summary>Draws the box, the handles and the orientation cross.</summary>
    [OnUpdate]
    public static void Draw(BehaviorContext ctx)
    {
        if (!App.HasRenderer) return;

        Orientation(ctx);

        if (!EditorSelection.Any) return;

        var entity = EditorSelection.Current;
        if (!Render.TryGetBounds(entity, out var min, out var max)) return;

        Outline(min, max);
        Handles(ctx, (min + max) * 0.5f, Reach(min, max));
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
        if (!ctx.Ecs.TryGet<Transform>(camera, out var transform)) return;

        var rotation = transform.Rotation;
        var forward = rotation * new Vec3(0f, 0f, -1f);
        var right = rotation * Vec3.UnitX;
        var up = rotation * Vec3.UnitY;

        const float Ahead = 2f;
        const float Size = 0.09f;

        // Bottom right of the view, far enough in not to be clipped by the edge of the screen at
        // the field of view the editor's camera uses.
        var origin = transform.Translation
            + (forward * Ahead)
            + (right * Ahead * 0.62f)
            - (up * Ahead * 0.36f);

        for (var i = 0; i < 3; i++)
        {
            Gizmos.Line(origin, origin + (Axes[i] * Size * Ahead), AxisColours[i]);
        }
    }

    /// <summary>How far a handle reaches, given what it is attached to.</summary>
    /// <remarks>
    /// From the thing's own size, so a handle on a small object is small and one on a building is
    /// not lost inside it, with a floor for something that has no size at all.
    /// </remarks>
    internal static float Reach(Vec3 min, Vec3 max)
    {
        var extent = (max - min) * 0.5f;
        var largest = MathF.Max(MathF.Max(extent.X, extent.Y), extent.Z);

        return MathF.Max(largest * 1.6f, 0.6f);
    }

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

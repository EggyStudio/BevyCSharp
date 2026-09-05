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

        Ground(ctx);
        Orientation(ctx);

        if (!EditorSelection.Any) return;

        var entity = EditorSelection.Current;
        if (!Render.TryGetBounds(entity, out var min, out var max)) return;

        var centre = (min + max) * 0.5f;
        var rotation = ctx.Ecs.GetOrDefault<GlobalTransform>(entity).Rotation;

        Outline(min, max);
        Handles(
            centre,
            Reach(EditorSelection.Camera, centre),
            EditorTools.AxesFor(rotation),
            Facing(ctx, EditorSelection.Camera));
    }

    /// <summary>Whether the ground is drawn as a grid.</summary>
    public static bool ShowGrid { get; set; } = true;

    /// <summary>
    /// A grid on the ground, under everything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What tells a person where the floor is and how big things are. A scene without one is a
    /// handful of objects in a void: nothing says which way is level, nothing says whether a cube
    /// is a metre across or ten, and moving something is a guess about how far it went.
    /// </para>
    /// <para>
    /// Drawn about the camera rather than about the origin, and at a spacing that steps by tens as
    /// the camera climbs, so it is the same density on screen at any height. A fixed grid is either
    /// a solid sheet from far away or four lines from close up.
    /// </para>
    /// </remarks>
    private static void Ground(BehaviorContext ctx)
    {
        if (!ShowGrid) return;

        var camera = EditorSelection.Camera;
        if (camera.IsNone) return;

        var basis = ctx.Ecs.GetOrDefault<GlobalTransform>(camera);
        var eye = basis.Translation;

        // How far apart the lines are: the power of ten that keeps a cell about a finger wide on
        // screen at this height, so the grid neither disappears nor turns into a sheet.
        var height = MathF.Max(0.5f, MathF.Abs(eye.Y));
        var step = MathF.Max(0.1f, MathF.Pow(10f, MathF.Floor(MathF.Log10(height))));

        const int Half = 26;

        var reach = step * Half;

        // Centred where the camera is looking rather than where it is. Looking out across a scene
        // from head height puts the camera's own patch of ground behind and below the view, and a
        // grid nobody can see is not a grid.
        var forward = (basis.ZAxis * -1f).Normalized;
        var look = eye;

        if (forward.Y < -0.05f)
        {
            var travel = MathF.Min(-eye.Y / forward.Y, reach * 2f);
            look = eye + (forward * travel);
        }

        var centreX = MathF.Round(look.X / step) * step;
        var centreZ = MathF.Round(look.Z / step) * step;

        // A hair above the floor rather than exactly on it. Scenes have a ground plane at zero and
        // two surfaces at the same depth fight over every pixel; the lift is a thousandth of a
        // cell, which is invisible at any height and enough to settle the argument.
        var lift = step * 0.002f;

        for (var i = -Half; i <= Half; i++)
        {
            var x = centreX + (i * step);
            var z = centreZ + (i * step);

            // Every tenth line is brighter, which is what gives a grid a scale to read rather than
            // an even wash. The lines through the origin get their axis's own colour.
            var alongZ = Line(x, step);
            var alongX = Line(z, step);

            // Behind what is in front of it. The grid is part of the scene, not a control drawn
            // about it: one that shows through the objects standing on it says nothing about where
            // they are.
            Gizmos.Line(
                new Vec3(x, lift, centreZ - reach),
                new Vec3(x, lift, centreZ + reach),
                MathF.Abs(x) < step * 0.5f ? AxisColours[2] : alongZ,
                inFront: false);

            Gizmos.Line(
                new Vec3(centreX - reach, lift, z),
                new Vec3(centreX + reach, lift, z),
                MathF.Abs(z) < step * 0.5f ? AxisColours[0] : alongX,
                inFront: false);
        }
    }

    /// <summary>How bright one grid line is: every tenth stands out from the rest.</summary>
    private static (float R, float G, float B, float A) Line(float at, float step)
    {
        var tenth = MathF.Abs(MathF.IEEERemainder(at, step * 10f)) < step * 0.5f;
        var value = tenth ? 0.34f : 0.19f;

        return (value, value, value * 1.05f, 1f);
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
    private static void Handles(Vec3 centre, float reach, Vec3[] axes, Vec3 facing)
    {
        if (EditorTools.Current == EditorTool.Select) return;

        var held = TransformGizmo.Axis;

        // The middle handle, which is the one that does not pick an axis: a drag across the screen
        // for a move, a turn about whatever way the pointer went for a rotation, and every axis at
        // once for a scale. Drawn first so the arms are over it rather than under.
        // Everything solid is drawn at a size in pixels, so how densely it has to be filled is
        // known from that alone: the gizmo is the same size on screen wherever it is.
        float InPixels(float world) => world / reach * Pixels;

        Disc(
            centre,
            facing,
            reach * CentreSize,
            InPixels(reach * CentreSize),
            held == TransformGizmo.Centre ? Accent : Middle);

        for (var i = 0; i < 3; i++)
        {
            var colour = held == i ? Accent : AxisColours[i];
            var axis = axes[i];

            switch (EditorTools.Current)
            {
                case EditorTool.Rotate:
                    Circle(centre, axis, reach, colour);
                    break;

                case EditorTool.Scale:
                    Gizmos.Line(centre, centre + (axis * reach), colour);
                    Disc(
                        centre + (axis * reach),
                        facing,
                        reach * HeadSize,
                        InPixels(reach * HeadSize),
                        colour);
                    break;

                default:
                    Gizmos.Line(centre, centre + (axis * reach), colour);
                    Arrow(
                        centre + (axis * reach),
                        axis,
                        reach * ArrowSize,
                        InPixels(reach * ArrowSize * ArrowWidth),
                        colour);
                    break;
            }
        }
    }

    /// <summary>How large the middle handle is, as a fraction of a handle's reach.</summary>
    internal const float CentreSize = 0.11f;

    /// <summary>How large the ball on the end of a stretch handle is.</summary>
    private const float HeadSize = 0.075f;

    /// <summary>How long the head of a move handle's arrow is.</summary>
    private const float ArrowSize = 0.14f;

    /// <summary>How wide that head is at its base, as a fraction of its length.</summary>
    private const float ArrowWidth = 0.42f;

    /// <summary>The middle handle when it is not held: no axis, so no axis colour.</summary>
    private static readonly (float R, float G, float B, float A) Middle = (0.85f, 0.85f, 0.88f, 1f);

    /// <summary>Which way the camera is pointing, in the world.</summary>
    private static Vec3 Facing(BehaviorContext ctx, Entity camera) =>
        camera.IsNone
            ? Vec3.UnitZ
            : (ctx.Ecs.GetOrDefault<GlobalTransform>(camera).ZAxis * -1f).Normalized;

    /// <summary>
    /// Draws a filled disc facing the camera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Filled rather than outlined, because a ring in the middle of three arms reads as a fourth
    /// thing to aim at the edge of rather than as one thing to press. There is nothing to draw
    /// with but lines, so it is filled the way a pen fills a circle: rings from the middle out,
    /// each a little wider than the last, and spokes across them.
    /// </para>
    /// <para>
    /// How many of each comes from how large the disc is on screen rather than from a number that
    /// looked right once — which is why the same routine fills the ball on a stretch handle and
    /// the base of a move handle's cone without any of them being tuned separately. A spoke's
    /// neighbours are furthest apart at the rim, so the count is whatever puts that gap under the
    /// width of a line; the rings do the same for the space between one ring and the next.
    /// Twenty-eight spokes and no rings left a band of dots two thirds of the way out, which is
    /// where the spokes had spread past a line's width and the gaps began to show.
    /// </para>
    /// </remarks>
    private static void Disc(
        Vec3 centre,
        Vec3 facing,
        float radius,
        float pixels,
        (float R, float G, float B, float A) colour)
    {
        var (first, second) = Perpendiculars(facing);

        var spokes = Sides(pixels);
        var rings = Math.Clamp((int)(pixels / Covered), 2, 16);

        Vec3 At(float angle, float from) =>
            centre
            + (first * (MathF.Cos(angle) * from))
            + (second * (MathF.Sin(angle) * from));

        for (var spoke = 0; spoke < spokes; spoke++)
        {
            Gizmos.Line(centre, At(spoke / (float)spokes * MathF.Tau, radius), colour);
        }

        for (var ring = 1; ring <= rings; ring++)
        {
            var at = radius * ring / rings;
            var steps = Math.Max(8, spokes * ring / rings);
            var previous = At(0f, at);

            for (var step = 1; step <= steps; step++)
            {
                var point = At(step / (float)steps * MathF.Tau, at);

                Gizmos.Line(previous, point, colour);
                previous = point;
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

    /// <summary>
    /// Draws a solid cone, point first, as the head of a move handle.
    /// </summary>
    /// <remarks>
    /// Four lines back from the point was a wire outline, and a wire outline of a small thing is a
    /// scribble. The cone is its base filled in and a fan of lines from the point down to the rim
    /// of that base, which covers the side facing the camera; the other side is behind it and
    /// never seen.
    /// </remarks>
    private static void Arrow(
        Vec3 tip,
        Vec3 direction,
        float size,
        float pixels,
        (float R, float G, float B, float A) colour)
    {
        var radius = size * ArrowWidth;
        var back = tip - (direction * size);

        Disc(back, direction, radius, pixels, colour);

        var (first, second) = Perpendiculars(direction);
        var sides = Sides(pixels);

        for (var side = 0; side < sides; side++)
        {
            var angle = side / (float)sides * MathF.Tau;

            Gizmos.Line(
                tip,
                back
                    + (first * (MathF.Cos(angle) * radius))
                    + (second * (MathF.Sin(angle) * radius)),
                colour);
        }
    }

    /// <summary>What a line covers, in pixels: anything closer together is one solid mark.</summary>
    private const float Covered = 1.4f;

    /// <summary>How many spokes a circle of this many pixels needs to have no gaps at its rim.</summary>
    private static int Sides(float pixels) =>
        Math.Clamp((int)(MathF.Tau * pixels / Covered), 12, 96);

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

        // How large the knob on an arm is, and how large that is on screen — which is what says
        // how finely it has to be filled. A wire sphere at this size is four pixels of hoops with
        // the scene showing through them.
        const float Knob = 0.18f;

        var pixels = square * 0.42f * Knob;

        // Six arms rather than three. Three says which way X, Y and Z point and leaves a person to
        // work out where the other halves went; six is the widget every editor draws, and the
        // negative halves are dimmed so the positive ones are still the ones read first.
        for (var i = 0; i < 3; i++)
        {
            var (r, g, b, _) = AxisColours[i];
            var arm = Axes[i] * size;

            Gizmos.Line(centre, centre + arm, AxisColours[i]);
            Disc(centre + arm, direction, size * Knob, pixels, AxisColours[i]);
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

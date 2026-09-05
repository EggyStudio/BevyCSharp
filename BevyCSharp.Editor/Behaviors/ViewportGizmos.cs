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
    /// What height the grid is drawn at.
    /// </summary>
    /// <remarks>
    /// Below the scene rather than through it. A grid on the same plane as a floor fights it for
    /// every pixel, and one at the height of whatever is standing on it cuts those things in half.
    /// Settable because where the bottom of a world is depends entirely on the world.
    /// </remarks>
    public static float GridHeight { get; set; } = -10f;

    /// <summary>
    /// A grid under the scene, fading out to nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What tells a person where the floor is and how big things are. A scene without one is a
    /// handful of objects in a void: nothing says which way is level, nothing says whether a cube
    /// is a metre across or ten, and moving something is a guess about how far it went.
    /// </para>
    /// <para>
    /// Two grids a decade apart, not one. A single grid that snaps from metres to tens as the
    /// camera climbs changes the whole floor in one frame, which reads as the picture breaking;
    /// drawn as a coarse grid that is always there and a fine one that fades away as its cells
    /// shrink, the change is something nobody notices happening. The fine grid leaves out the lines
    /// the coarse one already draws, so nothing is drawn twice and nothing appears out of nowhere
    /// when the two swap roles.
    /// </para>
    /// <para>
    /// Round rather than square, and fading as it goes out. A square of lines ending all at once
    /// announces where the editor stopped drawing, which is a fact about the editor and not about
    /// the scene; a disc that thins into nothing says only "the floor carries on".
    /// </para>
    /// </remarks>
    private static void Ground(BehaviorContext ctx)
    {
        if (!ShowGrid) return;

        var camera = EditorSelection.Camera;
        if (camera.IsNone) return;

        var basis = ctx.Ecs.GetOrDefault<GlobalTransform>(camera);
        var eye = basis.Translation;

        // Which two spacings to draw. The whole part of the decade picks them; how far the camera
        // is through that decade is not used here at all, because how solid a spacing is asks a
        // question about that spacing rather than about which pair happens to be on screen.
        var above = MathF.Max(0.5f, MathF.Abs(eye.Y - GridHeight));
        var whole = MathF.Floor(MathF.Log10(above));

        var fine = MathF.Max(0.1f, MathF.Pow(10f, whole));
        var coarse = fine * 10f;

        // One height for every spacing, worked out once. A hair above the height asked for, because
        // a grid on the same plane as a floor fights it for every pixel — but a hair scaled to the
        // spacing would put each spacing on a plane of its own, and the lines two of them draw in
        // the same place would run parallel a few millimetres apart instead of being one line.
        var plane = GridHeight + (above * 0.0004f);

        // Under the camera, always. Following where the camera is looking sounds helpful and is
        // not: turning on the spot then drags the whole floor around with the view, and the grid
        // stops being a fixed thing the camera moves over. Straight down from the eye is where it
        // ends up anyway when there is nothing to look at, and behaving the same either way is
        // worth more than reaching a little further ahead.
        var look = new Vec3(eye.X, plane, eye.Z);

        // Three, because a spacing takes two decades to come in and one to go: at any height there
        // is the one being read, the one behind it, and one further back still barely showing.
        // Coarsest first, so the finer lines are drawn over them rather than under.
        var coarsest = coarse * 10f;

        Sheet(look, plane, coarsest, Reach(above, coarsest), Solid(above, coarsest));
        Sheet(look, plane, coarse, Reach(above, coarse), Solid(above, coarse));
        Sheet(look, plane, fine, Reach(above, fine), Solid(above, fine));

        Axis(eye, plane, Reach(above, coarsest));
    }

    /// <summary>
    /// The two lines through the world's origin, in the colours of the axes they lie along.
    /// </summary>
    /// <remarks>
    /// Drawn once rather than by each grid. Every grid has a line at zero and would colour it, so
    /// the axis came out three times over at three strengths — and each of those faded outwards
    /// from its own grid's centre, which is snapped to that grid's own spacing. The lines were on
    /// top of each other and their fades were not, which reads as one line that will not line up
    /// with itself. There is one axis; it is drawn once.
    /// </remarks>
    private static void Axis(Vec3 eye, float height, float reach)
    {
        var gone = (0f, 0f, 0f, 0f);

        // From the point on each axis nearest the camera, which needs no snapping: one line has no
        // spacing to be snapped to, and sliding smoothly is what a single line should do.
        var alongX = new Vec3(eye.X, height, 0f);
        var alongZ = new Vec3(0f, height, eye.Z);

        var red = Tint(AxisColours[0], AxisSolid);
        var blue = Tint(AxisColours[2], AxisSolid);

        Gizmos.Fade(alongX, alongX + new Vec3(-reach, 0f, 0f), red, gone, inFront: false);
        Gizmos.Fade(alongX, alongX + new Vec3(reach, 0f, 0f), red, gone, inFront: false);
        Gizmos.Fade(alongZ, alongZ + new Vec3(0f, 0f, -reach), blue, gone, inFront: false);
        Gizmos.Fade(alongZ, alongZ + new Vec3(0f, 0f, reach), blue, gone, inFront: false);
    }

    /// <summary>How solid the two lines through the origin are, which do not fade with a spacing.</summary>
    private const float AxisSolid = 0.5f;

    /// <summary>How many cells across a grid is, before the height has its say.</summary>
    private const int Half = 20;

    /// <summary>
    /// How far a grid reaches before it has faded away entirely.
    /// </summary>
    /// <remarks>
    /// The smaller of what the spacing wants and what the height allows. A grid sized only by its
    /// own spacing puts the coarsest one a hundred times further out than the finest, which is a
    /// haze of lines running to the horizon long after they have stopped saying anything about
    /// where things are. How far somebody can usefully see is a question about how high they are,
    /// and the answer is the same for every spacing.
    /// </remarks>
    private static float Reach(float above, float step)
    {
        const float Spread = 8f;

        return MathF.Min(step * Half, above * Spread);
    }

    /// <summary>How solid a grid line is at its strongest.</summary>
    private const float Base = 0.22f;

    /// <summary>
    /// How solid one spacing is, from how large its cells are for the height being looked from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One question asked of each spacing on its own, which is what makes the change between two of
    /// them impossible to catch happening. A spacing is at full strength when its cells are the size
    /// the height calls for, and fades away over the decade on either side of that: out below, as
    /// the cells shrink towards nothing, and <em>in</em> from above, as they come down from being
    /// too large to be a grid at all.
    /// </para>
    /// <para>
    /// Both halves matter and only one of them is obvious. A grid written without thinking about
    /// the way up fades out correctly and appears at full strength: descending looks right, and
    /// every step of the climb drops a whole new spacing onto the floor in one frame.
    /// </para>
    /// <para>
    /// Coming in takes twice as long as going, and eases rather than ramping. Something arriving is
    /// noticed and something leaving is not, so the two want different lengths to feel like the
    /// same speed — over one decade each, the way down is invisible and the way up is a spacing
    /// appearing.
    /// </para>
    /// <para>
    /// Nothing here knows which spacing is the fine one and which is the coarse one, so nothing
    /// changes at the moment they swap roles: a ten metre line is as solid at ninety metres up as
    /// at a hundred and ten.
    /// </para>
    /// </remarks>
    private static float Solid(float above, float step)
    {
        // Decades a spacing takes to arrive, and to leave once it has been passed.
        const float In = 2f;
        const float Out = 1f;

        var decades = MathF.Log10(above / step);

        if (decades >= 0f) return Base * MathF.Max(0f, 1f - (decades / Out));

        return Base * MathF.Pow(MathF.Max(0f, 1f + (decades / In)), 1.6f);
    }

    /// <summary>
    /// One grid of a single spacing, as a disc fading to nothing at its rim.
    /// </summary>
    /// <param name="look">Where the middle of it goes, before being snapped to the spacing.</param>
    /// <param name="height">
    /// What height to draw at, worked out once for every spacing. Given rather than computed here,
    /// because two spacings on two planes draw the lines they share as two parallel lines a few
    /// millimetres apart.
    /// </param>
    /// <param name="step">How far apart the lines are.</param>
    /// <param name="reach">How far out it goes before it has faded away entirely.</param>
    /// <param name="solid">How solid a line is at its strongest.</param>
    private static void Sheet(Vec3 look, float height, float step, float reach, float solid)
    {
        if (solid <= 0.004f) return;

        var centreX = MathF.Round(look.X / step) * step;
        var centreZ = MathF.Round(look.Z / step) * step;
        var count = Math.Min(Half, (int)MathF.Ceiling(reach / step));

        for (var i = -count; i <= count; i++)
        {
            // How long the line is before it leaves the disc, and how bright it starts. Both come
            // from how far the line passes from the middle: a line through the middle runs the
            // full width at full strength, and one near the rim is a short faint stroke.
            var offset = i * step;
            var away = MathF.Abs(offset);
            if (away >= reach) continue;

            var span = MathF.Sqrt((reach * reach) - (away * away));
            var strength = solid * Falloff(away / reach);

            var x = centreX + offset;
            var z = centreZ + offset;

            // The lines through the origin belong to the axes, which are drawn once for all three
            // spacings rather than three times at three strengths.
            var atX = MathF.Abs(x) < step * 0.5f;
            var atZ = MathF.Abs(z) < step * 0.5f;

            var onZ = Shade(x, step, strength);
            var onX = Shade(z, step, strength);

            var gone = (0f, 0f, 0f, 0f);

            // Two halves out from the middle, each fading to nothing, which is what makes the far
            // edge a horizon rather than a boundary.
            if (!atX)
            {
                Gizmos.Fade(
                    new Vec3(x, height, centreZ),
                    new Vec3(x, height, centreZ - span),
                    onZ,
                    gone,
                    inFront: false);

                Gizmos.Fade(
                    new Vec3(x, height, centreZ),
                    new Vec3(x, height, centreZ + span),
                    onZ,
                    gone,
                    inFront: false);
            }

            if (!atZ)
            {
                Gizmos.Fade(
                    new Vec3(centreX, height, z),
                    new Vec3(centreX - span, height, z),
                    onX,
                    gone,
                    inFront: false);

                Gizmos.Fade(
                    new Vec3(centreX, height, z),
                    new Vec3(centreX + span, height, z),
                    onX,
                    gone,
                    inFront: false);
            }
        }
    }

    /// <summary>
    /// What a grid line looks like at a world position: every tenth one brighter than its
    /// neighbours, and anything else plain.
    /// </summary>
    /// <remarks>
    /// The emphasis belongs to each grid rather than to the set of them. Letting a coarser grid
    /// draw over a finer one and calling the overlap a marked line ties the marking to the
    /// crossfade, so the scale of the floor becomes harder and easier to read as the camera moves —
    /// and at the moment one of them has faded out there is no marking at all.
    /// </remarks>
    private static (float R, float G, float B, float A) Shade(
        float at, float step, float strength)
    {
        var tenth = MathF.Abs(MathF.IEEERemainder(at, step * 10f)) < step * 0.5f;
        return Grey(tenth ? strength * Marked : strength);
    }

    /// <summary>How much brighter every tenth line is, which is what gives the floor a scale.</summary>
    private const float Marked = 2f;

    /// <summary>
    /// How bright the grid is at a fraction of the way out to its edge.
    /// </summary>
    /// <remarks>
    /// Fading from the middle, with no part of it held at full. A grid that holds its strength and
    /// then drops away has a visible ring where it starts to go; one that has been thinning the
    /// whole way simply runs out, and nowhere along it is there a place the eye can point at and
    /// call the edge.
    /// </remarks>
    private static float Falloff(float outward) =>
        MathF.Pow(MathF.Max(0f, 1f - outward), 1.5f);

    /// <summary>A colour at a fraction of its strength, which for a grid means its alpha.</summary>
    private static (float R, float G, float B, float A) Tint(
        (float R, float G, float B, float A) colour, float strength) =>
        (colour.R, colour.G, colour.B, MathF.Min(1f, colour.A * strength));

    /// <summary>What an ordinary grid line is: white, at whatever strength it has left.</summary>
    private static (float R, float G, float B, float A) Grey(float strength) =>
        (0.72f, 0.74f, 0.80f, strength);

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

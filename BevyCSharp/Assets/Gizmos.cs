using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Debug drawing: lines, spheres and axis markers, for one frame at a time.
/// </summary>
/// <remarks>
/// <para>
/// For watching what a program is doing rather than for building anything. A gizmo is drawn on
/// top of the scene, is not lit, and cannot be selected or interacted with.
/// </para>
/// <para>
/// <b>Immediate.</b> What is drawn lasts one frame, so a shape that should stay on screen has to be
/// asked for again every frame. That makes a gizmo useful for a value that changes and a poor
/// choice for anything permanent, which needs an entity instead.
/// </para>
/// <para>
/// Needs a window. The plugin that draws gizmos comes with one, so a windowless run reports that
/// rather than collecting shapes nothing will ever draw.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// public void Show(BehaviorContext ctx)
/// {
///     var world = ctx.Ecs.GetRef&lt;GlobalTransform&gt;(ctx.Entity);
///
///     Gizmos.Line(world.Translation, world.Translation + Velocity, (1f, 1f, 0f, 1f));
///     Gizmos.Sphere(world.Translation, Radius, (1f, 0f, 0f, 1f));
/// }
/// </code>
/// </example>
public static unsafe class Gizmos
{
    /// <summary>
    /// Draws a line that fades from one color to another along its length.
    /// </summary>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="from">Linear RGBA at the start.</param>
    /// <param name="to">Linear RGBA at the end. An alpha of zero is a line that runs out.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <remarks>
    /// What draws something that has no edge: a grid that thins into the distance rather than
    /// stopping at a square boundary, a trail that dies away behind what left it. Fading a line by
    /// cutting it into pieces and coloring each is the same picture with a seam every few
    /// centimeters and one call per piece.
    /// </remarks>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void Fade(
        Vec3 start,
        Vec3 end,
        (float R, float G, float B, float A) from,
        (float R, float G, float B, float A) to,
        bool inFront = true) =>
        Draw(new NativeGizmoConfig
        {
            Kind = 3,
            InFront = inFront ? 1 : 0,
            StartX = start.X,
            StartY = start.Y,
            StartZ = start.Z,
            EndX = end.X,
            EndY = end.Y,
            EndZ = end.Z,
            RotationW = 1f,
            ColorR = from.R,
            ColorG = from.G,
            ColorB = from.B,
            ColorA = from.A,
            EndColorR = to.R,
            EndColorG = to.G,
            EndColorB = to.B,
            EndColorA = to.A,
        });

    /// <summary>Draws a line between two points.</summary>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">
    /// Whether the scene can hide it. In front by default, because a gizmo is usually a control (a
    /// handle, an outline, a marker) and one hiding inside the thing it describes has failed at the
    /// only job it has. Pass <see langword="false"/> for a shape drawn *in* the scene rather
    /// than about it: a grid, a path, a wireframe, all of which have to be behind what is in front
    /// of them to describe the scene at all.
    /// </param>
    public static void Line(
        Vec3 start,
        Vec3 end,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(new NativeGizmoConfig
        {
            Kind = 0,
            InFront = inFront ? 1 : 0,
            StartX = start.X,
            StartY = start.Y,
            StartZ = start.Z,
            EndX = end.X,
            EndY = end.Y,
            EndZ = end.Z,
            RotationW = 1f,
            ColorR = color.R,
            ColorG = color.G,
            ColorB = color.B,
            ColorA = color.A,
        });

    /// <summary>Draws the outline of a sphere.</summary>
    /// <remarks>Three circles rather than a solid, which keeps it readable over a scene.</remarks>
    /// <param name="center">Where it sits, in world space.</param>
    /// <param name="radius">How large.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Sphere(
        Vec3 center,
        float radius,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(new NativeGizmoConfig
        {
            Kind = 1,
            InFront = inFront ? 1 : 0,
            StartX = center.X,
            StartY = center.Y,
            StartZ = center.Z,
            RotationW = 1f,
            Radius = radius,
            ColorR = color.R,
            ColorG = color.G,
            ColorB = color.B,
            ColorA = color.A,
        });

    /// <summary>Draws a line with a head on its far end.</summary>
    /// <remarks>
    /// What a line cannot say. A velocity, a normal, a direction to a target: all of them are a
    /// line plus which way along it, and a line drawn for one of those leaves the reader working
    /// out the direction from what they already believe.
    /// </remarks>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where the head is.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Arrow(
        Vec3 start,
        Vec3 end,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(new NativeGizmoConfig
        {
            Kind = 7,
            InFront = inFront ? 1 : 0,
            StartX = start.X,
            StartY = start.Y,
            StartZ = start.Z,
            EndX = end.X,
            EndY = end.Y,
            EndZ = end.Z,
            RotationW = 1f,
            ColorR = color.R,
            ColorG = color.G,
            ColorB = color.B,
            ColorA = color.A,
        });

    /// <summary>Draws the outline of a circle.</summary>
    /// <remarks>
    /// Flat, facing the way <paramref name="rotation"/> points, which is the difference between
    /// this and <see cref="Sphere"/>: a circle says where a plane is, and a sphere says where a
    /// point is and how far its influence reaches.
    /// </remarks>
    /// <param name="center">Where it sits, in world space.</param>
    /// <param name="rotation">Which way the circle faces. Unrotated is the XY plane.</param>
    /// <param name="radius">How large.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Circle(
        Vec3 center,
        Quat rotation,
        float radius,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(5, center, rotation, color, inFront, radius: radius));

    /// <summary>Draws part of a circle.</summary>
    /// <remarks>
    /// What shows an angle rather than a direction: a field of view, a turn still to be made, the
    /// sweep a limb is allowed. It starts where the rotation's own X axis points and runs
    /// anticlockwise, so aiming an arc is a matter of turning it.
    /// </remarks>
    /// <param name="center">The center the arc is struck about.</param>
    /// <param name="rotation">Where the arc starts and which plane it lies in.</param>
    /// <param name="radius">How far from the center.</param>
    /// <param name="angle">How much of the circle to draw, in radians.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Arc(
        Vec3 center,
        Quat rotation,
        float radius,
        float angle,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(6, center, rotation, color, inFront, radius: radius, end: new Vec3(angle, 0f, 0f)));

    /// <summary>Draws the outline of a rectangle.</summary>
    /// <param name="center">Where the middle of it sits, in world space.</param>
    /// <param name="rotation">Which way it faces. Unrotated is the XY plane.</param>
    /// <param name="width">How wide, along the rectangle's own X axis.</param>
    /// <param name="height">How tall, along its own Y axis.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Rect(
        Vec3 center,
        Quat rotation,
        float width,
        float height,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(4, center, rotation, color, inFront, end: new Vec3(width, height, 0f)));

    /// <summary>Draws the twelve edges of a box.</summary>
    /// <remarks>
    /// What a bounding volume looks like. Sized rather than given two corners, because a box that
    /// is drawn about something is placed where that thing is and sized to what it occupies, and
    /// the arithmetic to turn two corners into that is the caller's either way.
    /// </remarks>
    /// <param name="center">Where the middle of it sits, in world space.</param>
    /// <param name="rotation">Which way it is turned.</param>
    /// <param name="size">How large along each of its own axes.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Box(
        Vec3 center,
        Quat rotation,
        Vec3 size,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(9, center, rotation, color, inFront, end: size));

    /// <summary>Draws the outline of a capsule.</summary>
    /// <remarks>
    /// The shape a character controller and most colliders are, which makes it the one worth
    /// drawing beside a box. Its length is the straight part between the two caps, so the whole
    /// thing is that plus twice the radius.
    /// </remarks>
    /// <param name="center">Where the middle of it sits, in world space.</param>
    /// <param name="rotation">Which way it stands. Unrotated stands along Y.</param>
    /// <param name="radius">How wide.</param>
    /// <param name="length">The straight part between the caps.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Capsule(
        Vec3 center,
        Quat rotation,
        float radius,
        float length,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(10, center, rotation, color, inFront, radius, new Vec3(length, 0f, 0f)));

    /// <summary>Draws the outline of a cone.</summary>
    /// <param name="center">Where it sits, in world space.</param>
    /// <param name="rotation">Which way it points. Unrotated points along Y.</param>
    /// <param name="radius">How wide the base is.</param>
    /// <param name="height">How tall.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Cone(
        Vec3 center,
        Quat rotation,
        float radius,
        float height,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(11, center, rotation, color, inFront, radius, new Vec3(height, 0f, 0f)));

    /// <summary>Draws the outline of a cylinder.</summary>
    /// <param name="center">Where the middle of it sits, in world space.</param>
    /// <param name="rotation">Which way it stands. Unrotated stands along Y.</param>
    /// <param name="radius">How wide.</param>
    /// <param name="halfHeight">Half its height, which describes the shape.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Cylinder(
        Vec3 center,
        Quat rotation,
        float radius,
        float halfHeight,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(12, center, rotation, color, inFront, radius, new Vec3(halfHeight, 0f, 0f)));

    /// <summary>Draws the outline of a torus.</summary>
    /// <remarks>
    /// What an orbit, a turning circle or a radius of effect with thickness looks like. The major
    /// radius is the ring itself and the minor one is how thick that ring is.
    /// </remarks>
    /// <param name="center">Where the middle of the ring sits, in world space.</param>
    /// <param name="rotation">Which plane the ring lies in. Unrotated lies in XZ.</param>
    /// <param name="major">The radius of the ring.</param>
    /// <param name="minor">The thickness of the ring.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Torus(
        Vec3 center,
        Quat rotation,
        float major,
        float minor,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(13, center, rotation, color, inFront, major, new Vec3(minor, 0f, 0f)));

    /// <summary>Draws a flat grid of lines.</summary>
    /// <remarks>
    /// The one shape usually wanted behind the scene rather than in front of it, because a grid is
    /// drawn to describe where things are and one floating over them describes nothing, so
    /// <paramref name="inFront"/> is false by default here where it is true everywhere else.
    /// </remarks>
    /// <param name="center">Where the middle of the grid sits, in world space.</param>
    /// <param name="rotation">Which plane it lies in. Unrotated is the XY plane.</param>
    /// <param name="across">How many cells wide.</param>
    /// <param name="down">How many cells tall.</param>
    /// <param name="spacing">How large one cell is, along both axes.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Grid(
        Vec3 center,
        Quat rotation,
        uint across,
        uint down,
        float spacing,
        (float R, float G, float B, float A) color,
        bool inFront = false) =>
        Draw(Shape(
            8, center, rotation, color, inFront,
            radius: spacing,
            end: new Vec3(across, down, 0f)));

    /// <summary>Draws a cone with its point cut off.</summary>
    /// <remarks>
    /// What a spot light's beam, a funnel or a tapering shaft is. A radius of zero at the top is a
    /// cone and two equal radii is a cylinder, so this is the general case of both, and it is here
    /// because neither of those can describe a beam that starts wide.
    /// </remarks>
    /// <param name="center">Where the middle of it sits, in world space.</param>
    /// <param name="rotation">Which way it points. Unrotated stands on its Y axis.</param>
    /// <param name="bottom">The radius at the base.</param>
    /// <param name="top">The radius at the cut end.</param>
    /// <param name="height">How far apart the two ends are.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Frustum(
        Vec3 center,
        Quat rotation,
        float bottom,
        float top,
        float height,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(
            20, center, rotation, color, inFront,
            radius: bottom,
            end: new Vec3(top, height, 0f)));

    /// <summary>Draws the outline of a rectangle, flat, for a 2D camera.</summary>
    /// <remarks>
    /// <para>
    /// Every call below is the shape above it seen by <see cref="Render2d.SpawnCamera2d"/>. They take
    /// a point on the XY plane and an angle about Z, because that is all a flat shape can be turned
    /// by, and they are drawn by Bevy's own flat calls rather than by the solid ones at zero depth,
    /// which differ once a line has width.
    /// </para>
    /// <para>
    /// A 2D camera in Bevy sees the same world the 3D one does, so these are world space in the
    /// same sense as everything else here. What makes them flat is that nothing about them has a
    /// depth to give.
    /// </para>
    /// </remarks>
    /// <param name="center">Where the middle of it sits, on the XY plane.</param>
    /// <param name="width">How wide, along the rectangle's own X axis.</param>
    /// <param name="height">How tall, along its own Y axis.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Rect2d(
        (float X, float Y) center,
        float width,
        float height,
        (float R, float G, float B, float A) color,
        float angle = 0f,
        bool inFront = true) =>
        Draw(Shape(
            14, Flat(center), Turn(angle), color, inFront, end: new Vec3(width, height, 0f)));

    /// <summary>Draws the outline of a circle, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where it sits, on the XY plane.</param>
    /// <param name="radius">How large.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Circle2d(
        (float X, float Y) center,
        float radius,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Shape(15, Flat(center), Quat.Identity, color, inFront, radius: radius));

    /// <summary>Draws a line between two points, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="start">Where it begins, on the XY plane.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Line2d(
        (float X, float Y) start,
        (float X, float Y) end,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Gradient(16, start, end, color, color, inFront));

    /// <summary>Draws a line that fades from one color to another, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="start">Where it begins, on the XY plane.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="from">The color at the start, linear RGBA.</param>
    /// <param name="to">The color at the end.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Line2d(
        (float X, float Y) start,
        (float X, float Y) end,
        (float R, float G, float B, float A) from,
        (float R, float G, float B, float A) to,
        bool inFront = true) =>
        Draw(Gradient(16, start, end, from, to, inFront));

    /// <summary>Draws a line with a head on its far end, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="start">Where it begins, on the XY plane.</param>
    /// <param name="end">Where the head is.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Arrow2d(
        (float X, float Y) start,
        (float X, float Y) end,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Draw(Gradient(17, start, end, color, color, inFront));

    /// <summary>Draws part of a circle, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">The center the arc is struck about.</param>
    /// <param name="radius">How far from the center.</param>
    /// <param name="angle">How much of the circle to draw, in radians.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="from">Where the arc starts, as an angle about Z in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Arc2d(
        (float X, float Y) center,
        float radius,
        float angle,
        (float R, float G, float B, float A) color,
        float from = 0f,
        bool inFront = true) =>
        Draw(Shape(
            18, Flat(center), Turn(from), color, inFront,
            radius: radius,
            end: new Vec3(angle, 0f, 0f)));

    /// <summary>Draws a grid of lines, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where the middle of the grid sits, on the XY plane.</param>
    /// <param name="across">How many cells wide.</param>
    /// <param name="down">How many cells tall.</param>
    /// <param name="spacing">How large one cell is, along both axes.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Grid2d(
        (float X, float Y) center,
        uint across,
        uint down,
        float spacing,
        (float R, float G, float B, float A) color,
        float angle = 0f,
        bool inFront = false) =>
        Draw(Shape(
            19, Flat(center), Turn(angle), color, inFront,
            radius: spacing,
            end: new Vec3(across, down, 0f)));

    /// <summary>A point on the XY plane, as the queue's three numbers.</summary>
    private static Vec3 Flat((float X, float Y) point) => new(point.X, point.Y, 0f);

    /// <summary>An angle about Z, as the queue's quaternion.</summary>
    private static Quat Turn(float angle) => Quat.FromAxisAngle(Vec3.UnitZ, angle);

    /// <summary>Fills in a flat shape described by two points and two colors.</summary>
    private static NativeGizmoConfig Gradient(
        int kind,
        (float X, float Y) start,
        (float X, float Y) end,
        (float R, float G, float B, float A) from,
        (float R, float G, float B, float A) to,
        bool inFront)
    {
        var config = Shape(kind, Flat(start), Quat.Identity, from, inFront, end: Flat(end));

        config.EndColorR = to.R;
        config.EndColorG = to.G;
        config.EndColorB = to.B;
        config.EndColorA = to.A;

        return config;
    }

    /// <summary>
    /// Draws a whole run of lines in one crossing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a wireframe, a path or a grid is. Every other call here crosses the boundary on its
    /// own, which is fine at the few hundred shapes a frame an editor overlay asks for and stops
    /// being fine well before a mesh's worth. This costs the array rather than the number of lines
    /// in it.
    /// </para>
    /// <para>
    /// The lines are all drawn the same way, since <paramref name="inFront"/> is one answer for the
    /// run. A path drawn in two colors is two calls, which is still two rather than one per
    /// segment.
    /// </para>
    /// </remarks>
    /// <param name="lines">The segments, each with its own two ends and color.</param>
    /// <param name="inFront">
    /// Whether the scene can hide them. False by default here where it is true elsewhere, because
    /// what is drawn in runs is usually drawn *in* the scene rather than about it.
    /// </param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    /// <example>
    /// <code>
    /// Span&lt;GizmoSegment&gt; path = stackalloc GizmoSegment[points.Length - 1];
    ///
    /// for (var i = 0; i &lt; path.Length; i++)
    ///     path[i] = new GizmoSegment(points[i], points[i + 1], (0f, 1f, 0f, 1f));
    ///
    /// Gizmos.Lines(path);
    /// </code>
    /// </example>
    public static void Lines(ReadOnlySpan<GizmoSegment> lines, bool inFront = false)
    {
        if (lines.IsEmpty) return;

        // Built here rather than by the caller, so the wire format stays this file's business the
        // way it is for every single-shape call above.
        var configs = lines.Length <= 64
            ? stackalloc NativeGizmoConfig[lines.Length]
            : new NativeGizmoConfig[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            configs[i] = Shape(
                line.Fades ? 3 : 0,
                line.Start,
                Quat.Identity,
                line.Color,
                inFront,
                end: line.End);

            configs[i].EndColorR = line.EndColor.R;
            configs[i].EndColorG = line.EndColor.G;
            configs[i].EndColorB = line.EndColor.B;
            configs[i].EndColorA = line.EndColor.A;
        }

        fixed (NativeGizmoConfig* at = configs)
        {
            var status = Native.bcs_gizmo_draw_many(at, lines.Length);

            if (status == NativeStatus.Unsupported)
                throw new BevyNativeException(
                    NativeStatus.Unsupported,
                    "Drawing gizmos failed, because gizmos are drawn by a plugin that comes with "
                    + "the window, so a windowless run has nothing to draw on. Guard with "
                    + "App.HasRenderer and Config.Headless.");

            Native.Check(status, "drawing a run of gizmo lines");
        }
    }

    /// <summary>
    /// Sets how every gizmo is drawn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="which"/> is the only way one kind of debug drawing is told from another
    /// here. Bevy groups gizmos by a config group, which is a Rust type rather than a value, so
    /// the two that exist are the two a shape's <c>inFront</c> already chooses between, and a
    /// game wanting more than that runs out of groups rather than of settings.
    /// </para>
    /// <para>
    /// Those two are enough for the thing usually wanted, because what is drawn *in* a scene and
    /// what is drawn *about* it are already on opposite sides of the split. Turning off the group
    /// the scene can hide takes the floor grid and the paths away and leaves the handles.
    /// </para>
    /// <para>
    /// <paramref name="enabled"/> suits a debug overlay bound to a key, because it stops the
    /// drawing without the systems that ask for it having to know they should stop.
    /// </para>
    /// </remarks>
    /// <param name="width">Line thickness in pixels, or 0 to leave it as it is.</param>
    /// <param name="layers">
    /// Which render layers gizmos appear on, as a bit mask. Only a camera whose layers overlap
    /// draws them. Zero keeps Bevy's own default of layer zero.
    /// </param>
    /// <param name="enabled">Whether to draw gizmos at all.</param>
    /// <param name="which">Which group this applies to.</param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void Configure(
        float width = 0f,
        uint layers = 0,
        bool enabled = true,
        GizmoGroup which = GizmoGroup.Both) =>
        Native.Check(
            Native.bcs_gizmo_configure(width, layers, enabled ? 1 : 0, (int)which),
            "configuring gizmos");

    /// <summary>
    /// Sets what a gizmo line looks like.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Apart from <see cref="Configure"/> because how thick a line is and who can see it is one
    /// decision, and what the line looks like is another. Both cover every gizmo, for the same
    /// reason.
    /// </para>
    /// <para>
    /// A dotted or dashed line tells one meaning from another without a second color, so a path
    /// already walked can be drawn against the one still to come. <see cref="GizmoJoint"/> only
    /// shows on a shape whose lines meet, which is every closed shape and no single segment.
    /// </para>
    /// </remarks>
    /// <param name="style">Whether the line is solid, dotted or dashed.</param>
    /// <param name="gapScale">
    /// How long the gap in a dashed line is, in line widths. Zero takes Bevy's own default of one,
    /// and it is read only for <see cref="GizmoLine.Dashed"/>.
    /// </param>
    /// <param name="lineScale">
    /// How long the drawn run of a dashed line is, in line widths. Zero takes Bevy's own default
    /// of three.
    /// </param>
    /// <param name="joint">How two lines meet at a corner.</param>
    /// <param name="jointResolution">
    /// How many triangles a round joint is drawn with. Zero takes Bevy's own default of four, and
    /// it is read only for <see cref="GizmoJoint.Round"/>.
    /// </param>
    /// <param name="perspective">
    /// Whether the width is a size at the camera's near plane rather than a size on screen, so a
    /// line further away is drawn thinner. Only a perspective 3D camera can honor it.
    /// </param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void SetLineStyle(
        GizmoLine style = GizmoLine.Solid,
        float gapScale = 0f,
        float lineScale = 0f,
        GizmoJoint joint = GizmoJoint.None,
        uint jointResolution = 0,
        bool perspective = false) =>
        Native.Check(
            Native.bcs_gizmo_style(
                (int)style,
                gapScale,
                lineScale,
                (int)joint,
                jointResolution,
                perspective ? 1 : 0),
            "styling gizmo lines");

    /// <summary>
    /// Fills in the shape every call above builds, so each of them is its own arguments and
    /// nothing else.
    /// </summary>
    private static NativeGizmoConfig Shape(
        int kind,
        Vec3 center,
        Quat rotation,
        (float R, float G, float B, float A) color,
        bool inFront,
        float radius = 0f,
        Vec3 end = default) => new()
    {
        Kind = kind,
        InFront = inFront ? 1 : 0,
        StartX = center.X,
        StartY = center.Y,
        StartZ = center.Z,
        EndX = end.X,
        EndY = end.Y,
        EndZ = end.Z,
        RotationX = rotation.X,
        RotationY = rotation.Y,
        RotationZ = rotation.Z,
        RotationW = rotation.W,
        Radius = radius,
        ColorR = color.R,
        ColorG = color.G,
        ColorB = color.B,
        ColorA = color.A,
    };

    /// <summary>
    /// Draws a set of axes, so an orientation can be read at a glance.
    /// </summary>
    /// <remarks>
    /// Colored by Bevy: red for X, green for Y, blue for Z. Drawing these on an entity is the
    /// quickest way to see whether something is facing where it should.
    /// </remarks>
    /// <param name="transform">Where the axes sit and which way they point.</param>
    /// <param name="length">How long each arm is.</param>
    /// <param name="inFront">Whether the scene can hide them. See <see cref="Line"/>.</param>
    public static void Axes(Transform transform, float length = 1f, bool inFront = true) =>
        Draw(new NativeGizmoConfig
        {
            Kind = 2,
            InFront = inFront ? 1 : 0,
            StartX = transform.Translation.X,
            StartY = transform.Translation.Y,
            StartZ = transform.Translation.Z,
            RotationX = transform.Rotation.X,
            RotationY = transform.Rotation.Y,
            RotationZ = transform.Rotation.Z,
            RotationW = transform.Rotation.W,
            Radius = length,
            ColorA = 1f,
        });

    private static void Draw(NativeGizmoConfig config)
    {
        var status = Native.bcs_gizmo_draw(&config);
        if (status == NativeStatus.Unsupported)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                "Drawing a gizmo failed, because gizmos are drawn by a plugin that comes with the "
                + "window, so a windowless run has nothing to draw on. Guard with App.HasRenderer "
                + "and Config.Headless.");

        Native.Check(status, "drawing a gizmo");
    }
}

/// <summary>What a gizmo line is drawn as, along its length.</summary>
/// <remarks>
/// The way one meaning is told from another without spending a second color on it, which matters
/// where the color already says something else.
/// </remarks>
public enum GizmoLine
{
    /// <summary>One unbroken run.</summary>
    Solid = 0,

    /// <summary>A row of dots.</summary>
    Dotted = 1,

    /// <summary>Alternating runs and gaps, each measured in line widths.</summary>
    Dashed = 2,
}

/// <summary>How two gizmo lines meet at a corner.</summary>
/// <remarks>
/// Only visible on a shape whose lines meet, which is every closed shape and no single segment. At
/// a thin width the corners are too small to tell apart, so this is for the thick lines an overlay
/// drawn to be read at a glance uses.
/// </remarks>
public enum GizmoJoint
{
    /// <summary>Nothing is drawn, so a thick corner has a notch out of it.</summary>
    None = 0,

    /// <summary>Both lines are carried on until they meet at a point.</summary>
    Miter = 1,

    /// <summary>A rounded corner, drawn with as many triangles as it is given.</summary>
    Round = 2,

    /// <summary>A straight line across the gap between the two ends.</summary>
    Bevel = 3,
}

/// <summary>
/// One line of a run drawn by <see cref="Gizmos.Lines"/>.
/// </summary>
/// <remarks>
/// Its own two ends and its own color, because a run is usually a path or a wireframe where each
/// segment is somewhere different and some of them mean something different. <see cref="Fading"/>
/// gives it a second color, for a line running out to a horizon.
/// </remarks>
public readonly struct GizmoSegment
{
    /// <summary>A line of one color.</summary>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="color">Linear RGBA.</param>
    public GizmoSegment(Vec3 start, Vec3 end, (float R, float G, float B, float A) color)
    {
        Start = start;
        End = end;
        Color = color;
        EndColor = color;
    }

    private GizmoSegment(
        Vec3 start,
        Vec3 end,
        (float R, float G, float B, float A) from,
        (float R, float G, float B, float A) to)
    {
        Start = start;
        End = end;
        Color = from;
        EndColor = to;
        Fades = true;
    }

    /// <summary>Where it begins, in world space.</summary>
    public Vec3 Start { get; }

    /// <summary>Where it ends.</summary>
    public Vec3 End { get; }

    /// <summary>The color at the start, linear RGBA.</summary>
    public (float R, float G, float B, float A) Color { get; }

    /// <summary>The color at the end, which is the same one unless it fades.</summary>
    public (float R, float G, float B, float A) EndColor { get; }

    /// <summary>Whether the two colors are meant to be different.</summary>
    /// <remarks>
    /// Kept rather than compared, because a line fading from a color to the same color is a
    /// reasonable thing to ask for and would otherwise be silently turned into a plain one.
    /// </remarks>
    public bool Fades { get; }

    /// <summary>A line that fades from one color to another.</summary>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="from">The color at the start, linear RGBA.</param>
    /// <param name="to">The color at the end. Transparent makes a line run out.</param>
    public static GizmoSegment Fading(
        Vec3 start,
        Vec3 end,
        (float R, float G, float B, float A) from,
        (float R, float G, float B, float A) to) => new(start, end, from, to);
}

/// <summary>
/// Which of the two gizmo groups a setting applies to.
/// </summary>
/// <remarks>
/// The same split a shape's <c>inFront</c> chooses between, seen from the other end. A handle, an
/// outline or a marker is drawn about the scene and has to be reachable; a grid, a path or a
/// wireframe is drawn in it and has to be behind what is in front of it. Because the two kinds of
/// drawing already fall on opposite sides of that line, it doubles as the category a game turns
/// one kind of debug drawing off by.
/// </remarks>
public enum GizmoGroup
{
    /// <summary>Both, for a setting meant for everything.</summary>
    Both = 0,

    /// <summary>The group the scene can hide, which is where a grid or a path is drawn.</summary>
    Behind = 1,

    /// <summary>The group nothing can hide, which is where a handle or a marker is drawn.</summary>
    InFront = 2,
}

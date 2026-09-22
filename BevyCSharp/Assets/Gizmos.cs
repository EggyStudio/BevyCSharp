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
/// <b>Immediate.</b> What is drawn lasts one frame, so a shape that should stay on screen has to
/// be asked for again every frame. That is what makes a gizmo useful for a value that changes and
/// a poor choice for anything permanent, which wants an entity instead.
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
    /// centimetres and one call per piece.
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
    /// <remarks>Three circles rather than a solid, which is what makes it readable over a scene.</remarks>
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
    /// <param name="center">The centre the arc is struck about.</param>
    /// <param name="rotation">Where the arc starts and which plane it lies in.</param>
    /// <param name="radius">How far from the centre.</param>
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
    /// The shape a character controller and most colliders are, which is what makes it the one
    /// worth drawing beside a box. Its length is the straight part between the two caps, so the
    /// whole thing is that plus twice the radius.
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
    /// <param name="halfHeight">Half its height, which is what the shape is described by.</param>
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

    /// <summary>
    /// Sets how every gizmo is drawn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One setting for both groups, because the two exist to answer whether the scene may hide a
    /// shape and nothing else. A game that wants two kinds of debug drawing styled apart from each
    /// other wants what Bevy calls a config group, which is a type rather than a value and so has
    /// no bridge.
    /// </para>
    /// <para>
    /// <paramref name="enabled"/> is what a debug overlay bound to a key wants, because it stops
    /// the drawing without the systems that ask for it having to know they should stop.
    /// </para>
    /// </remarks>
    /// <param name="width">Line thickness in pixels, or 0 to leave it as it is.</param>
    /// <param name="layers">
    /// Which render layers gizmos appear on, as a bit mask. Only a camera whose layers overlap
    /// draws them. Zero keeps Bevy's own default of layer zero.
    /// </param>
    /// <param name="enabled">Whether to draw gizmos at all.</param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void Configure(float width = 0f, uint layers = 0, bool enabled = true) =>
        Native.Check(
            Native.bcs_gizmo_configure(width, layers, enabled ? 1 : 0),
            "configuring gizmos");

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
                "Drawing a gizmo failed: gizmos are drawn by a plugin that comes with the "
                + "window, so a windowless run has nothing to draw on. Guard with App.HasRenderer "
                + "and Config.Headless.");

        Native.Check(status, "drawing a gizmo");
    }
}

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
    /// Draws a line that fades from one colour to another along its length.
    /// </summary>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="from">Linear RGBA at the start.</param>
    /// <param name="to">Linear RGBA at the end. An alpha of zero is a line that runs out.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <remarks>
    /// What draws something that has no edge: a grid that thins into the distance rather than
    /// stopping at a square boundary, a trail that dies away behind what left it. Fading a line by
    /// cutting it into pieces and colouring each is the same picture with a seam every few
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
    /// Whether the scene can hide it. In front by default, because a gizmo is usually a control —
    /// a handle, an outline, a marker — and one hiding inside the thing it describes has failed at
    /// the only job it has. Pass <see langword="false"/> for a shape drawn *in* the scene rather
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

    /// <summary>
    /// Draws a set of axes, so an orientation can be read at a glance.
    /// </summary>
    /// <remarks>
    /// Coloured by Bevy: red for X, green for Y, blue for Z. Drawing these on an entity is the
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

using System.Runtime.InteropServices;
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
public static unsafe partial class Gizmos
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
    /// <param name="resolution">
    /// How many segments each circle is drawn with, or zero for Bevy's own. A sphere drawn large or
    /// close needs more to stay round, and one drawn many times a frame fewer.
    /// </param>
    public static void Sphere(
        Vec3 center,
        float radius,
        (float R, float G, float B, float A) color,
        bool inFront = true,
        uint resolution = 0) =>
        Draw(new NativeGizmoConfig
        {
            Kind = 1,
            InFront = inFront ? 1 : 0,
            StartX = center.X,
            StartY = center.Y,
            StartZ = center.Z,
            EndX = resolution,
            RotationW = 1f,
            Radius = radius,
            ColorR = color.R,
            ColorG = color.G,
            ColorB = color.B,
            ColorA = color.A,
        });

    /// <summary>Draws a line with a head on its far end.</summary>
    /// <remarks>
    /// What a line cannot say. A velocity, a normal, a direction to a target. All of them are a
    /// line plus which way along it, and a line drawn for one of those leaves the reader working
    /// out the direction from what they already believe.
    /// </remarks>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where the head is.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="tipLength">How long the head is, or zero for Bevy's own, a tenth of the arrow.</param>
    /// <param name="doubleEnd">Whether there is a head at the start as well, for a span between two points.</param>
    public static void Arrow(
        Vec3 start,
        Vec3 end,
        (float R, float G, float B, float A) color,
        bool inFront = true,
        float tipLength = 0f,
        bool doubleEnd = false) =>
        Draw(new NativeGizmoConfig
        {
            Kind = doubleEnd ? 25 : 7,
            Radius = tipLength,
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
    /// this and <see cref="Sphere"/>. A circle says where a plane is, and a sphere says where a
    /// point is and how far its influence reaches.
    /// </remarks>
    /// <param name="center">Where it sits, in world space.</param>
    /// <param name="rotation">Which way the circle faces. Unrotated is the XY plane.</param>
    /// <param name="radius">How large.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="resolution">How many segments it is drawn with, or zero for Bevy's own.</param>
    public static void Circle(
        Vec3 center,
        Quat rotation,
        float radius,
        (float R, float G, float B, float A) color,
        bool inFront = true,
        uint resolution = 0) =>
        Draw(Shape(5, center, rotation, color, inFront, radius: radius, end: new Vec3(resolution, 0f, 0f)));

    /// <summary>Draws the outline of an ellipse.</summary>
    /// <remarks>
    /// A circle stretched along its own axes, which is a circle seen at an angle, an orbit, or the
    /// reach of something that reaches further one way than the other.
    /// </remarks>
    /// <param name="center">Where it sits, in world space.</param>
    /// <param name="rotation">Which way it faces. Unrotated is the XY plane.</param>
    /// <param name="halfWidth">Half its extent along its own X axis.</param>
    /// <param name="halfHeight">Half its extent along its own Y axis.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="resolution">How many segments it is drawn with, or zero for Bevy's own.</param>
    public static void Ellipse(
        Vec3 center,
        Quat rotation,
        float halfWidth,
        float halfHeight,
        (float R, float G, float B, float A) color,
        bool inFront = true,
        uint resolution = 0) =>
        Draw(Shape(21, center, rotation, color, inFront, end: new Vec3(halfWidth, halfHeight, resolution)));

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
    /// <param name="resolution">How many segments it is drawn with, or zero for Bevy's own.</param>
    public static void Arc(
        Vec3 center,
        Quat rotation,
        float radius,
        float angle,
        (float R, float G, float B, float A) color,
        bool inFront = true,
        uint resolution = 0) =>
        Draw(Shape(6, center, rotation, color, inFront, radius: radius, end: new Vec3(angle, resolution, 0f)));

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

    /// <summary>Draws the outline of a rectangle with rounded corners.</summary>
    /// <param name="center">Where the middle of it sits, in world space.</param>
    /// <param name="rotation">Which way it faces. Unrotated is the XY plane.</param>
    /// <param name="width">How wide, along the rectangle's own X axis.</param>
    /// <param name="height">How tall, along its own Y axis.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="cornerRadius">
    /// How round the corners are, or null for Bevy's own, a tenth of the shorter side. A negative
    /// radius turns the corners inward.
    /// </param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="arcResolution">How many segments each corner is drawn with, or zero for Bevy's own.</param>
    public static void RoundedRect(
        Vec3 center,
        Quat rotation,
        float width,
        float height,
        (float R, float G, float B, float A) color,
        float? cornerRadius = null,
        bool inFront = true,
        uint arcResolution = 0) =>
        Draw(Shape(27, center, rotation, color, inFront, cornerRadius ?? float.NaN, new Vec3(width, height, 0f)) with { EndColorR = arcResolution });

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

    /// <summary>Draws the edges of a box with rounded edges and corners.</summary>
    /// <param name="center">Where the middle of it sits, in world space.</param>
    /// <param name="rotation">Which way it is turned.</param>
    /// <param name="size">How large along each of its own axes.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="edgeRadius">
    /// How round the edges are, or null for Bevy's own, a tenth of the shortest side. A negative
    /// radius turns them inward.
    /// </param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="arcResolution">How many segments each rounded edge is drawn with, or zero for Bevy's own.</param>
    public static void RoundedCuboid(
        Vec3 center,
        Quat rotation,
        Vec3 size,
        (float R, float G, float B, float A) color,
        float? edgeRadius = null,
        bool inFront = true,
        uint arcResolution = 0) =>
        Draw(Shape(24, center, rotation, color, inFront, edgeRadius ?? float.NaN, size) with { EndColorR = arcResolution });

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
    /// <param name="spacing">How large one cell is, along both axes unless <paramref name="spacingDown"/> says otherwise.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="spacingDown">How tall one cell is, or zero for as tall as it is wide.</param>
    public static void Grid(
        Vec3 center,
        Quat rotation,
        uint across,
        uint down,
        float spacing,
        (float R, float G, float B, float A) color,
        bool inFront = false,
        float spacingDown = 0f) =>
        Draw(Shape(
            8, center, rotation, color, inFront,
            radius: spacing,
            end: new Vec3(across, down, spacingDown)));

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
}

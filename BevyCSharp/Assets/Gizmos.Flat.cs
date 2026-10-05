using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Gizmos
{
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

    /// <summary>Draws the outline of a rectangle with rounded corners, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where the middle of it sits, on the XY plane.</param>
    /// <param name="width">How wide, along the rectangle's own X axis.</param>
    /// <param name="height">How tall, along its own Y axis.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="cornerRadius">
    /// How round the corners are, or null for Bevy's own, a tenth of the shorter side. A negative
    /// radius turns the corners inward.
    /// </param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="arcResolution">How many segments each corner is drawn with, or zero for Bevy's own.</param>
    public static void RoundedRect2d(
        (float X, float Y) center,
        float width,
        float height,
        (float R, float G, float B, float A) color,
        float? cornerRadius = null,
        float angle = 0f,
        bool inFront = true,
        uint arcResolution = 0) =>
        Draw(Shape(
            23, Flat(center), Turn(angle), color, inFront, cornerRadius ?? float.NaN, new Vec3(width, height, 0f)) with { EndColorR = arcResolution });

    /// <summary>Draws the outline of a circle, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where it sits, on the XY plane.</param>
    /// <param name="radius">How large.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="resolution">How many segments it is drawn with, or zero for Bevy's own.</param>
    public static void Circle2d(
        (float X, float Y) center,
        float radius,
        (float R, float G, float B, float A) color,
        bool inFront = true,
        uint resolution = 0) =>
        Draw(Shape(15, Flat(center), Quat.Identity, color, inFront, radius: radius, end: new Vec3(resolution, 0f, 0f)));

    /// <summary>Draws the outline of an ellipse, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where it sits, on the XY plane.</param>
    /// <param name="halfWidth">Half its extent along its own X axis.</param>
    /// <param name="halfHeight">Half its extent along its own Y axis.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="resolution">How many segments it is drawn with, or zero for Bevy's own.</param>
    public static void Ellipse2d(
        (float X, float Y) center,
        float halfWidth,
        float halfHeight,
        (float R, float G, float B, float A) color,
        float angle = 0f,
        bool inFront = true,
        uint resolution = 0) =>
        Draw(Shape(22, Flat(center), Turn(angle), color, inFront, end: new Vec3(halfWidth, halfHeight, resolution)));

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
    /// <param name="tipLength">How long the head is, or zero for Bevy's own, a tenth of the arrow.</param>
    /// <param name="doubleEnd">Whether there is a head at the start as well, for a span between two points.</param>
    public static void Arrow2d(
        (float X, float Y) start,
        (float X, float Y) end,
        (float R, float G, float B, float A) color,
        bool inFront = true,
        float tipLength = 0f,
        bool doubleEnd = false)
    {
        var config = Gradient(doubleEnd ? 26 : 17, start, end, color, color, inFront);
        config.Radius = tipLength;
        Draw(config);
    }

    /// <summary>Draws part of a circle, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">The center the arc is struck about.</param>
    /// <param name="radius">How far from the center.</param>
    /// <param name="angle">How much of the circle to draw, in radians.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="from">Where the arc starts, as an angle about Z in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="resolution">How many segments it is drawn with, or zero for Bevy's own.</param>
    public static void Arc2d(
        (float X, float Y) center,
        float radius,
        float angle,
        (float R, float G, float B, float A) color,
        float from = 0f,
        bool inFront = true,
        uint resolution = 0) =>
        Draw(Shape(
            18, Flat(center), Turn(from), color, inFront,
            radius: radius,
            end: new Vec3(angle, resolution, 0f)));

    /// <summary>Draws a grid of lines, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where the middle of the grid sits, on the XY plane.</param>
    /// <param name="across">How many cells wide.</param>
    /// <param name="down">How many cells tall.</param>
    /// <param name="spacing">How large one cell is, along both axes unless <paramref name="spacingDown"/> says otherwise.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <param name="spacingDown">How tall one cell is, or zero for as tall as it is wide.</param>
    public static void Grid2d(
        (float X, float Y) center,
        uint across,
        uint down,
        float spacing,
        (float R, float G, float B, float A) color,
        float angle = 0f,
        bool inFront = false,
        float spacingDown = 0f) =>
        Draw(Shape(
            19, Flat(center), Turn(angle), color, inFront,
            radius: spacing,
            end: new Vec3(across, down, spacingDown)));

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
}

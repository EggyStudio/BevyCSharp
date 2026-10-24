namespace Bevy;

public static partial class Gizmos
{
    /// <summary>
    /// Draws a plane, flat, for a 2D camera, as Bevy's <c>primitive_2d</c> draws its
    /// <c>Plane2d</c>, a line through <paramref name="point"/> and an arrow for the side it faces.
    /// </summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="point">A point the plane passes through, on the XY plane.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far its facing is turned about Z from up, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Plane2d((float X, float Y) point, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true) =>
        Draw(Shape(28, Flat(point), Turn(angle), color, inFront));

    /// <summary>
    /// Draws a plane as Bevy's <c>primitive_3d</c> draws its <c>Plane3d</c>, the arrow it faces
    /// along and lines across it, facing up before <paramref name="rotation"/> turns it.
    /// </summary>
    /// <param name="center">Where it sits.</param>
    /// <param name="rotation">Which way it faces.</param>
    /// <param name="halfWidth">Half its extent along its own X axis.</param>
    /// <param name="halfDepth">Half its extent along its own Z axis.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Plane(Vec3 center, Quat rotation, float halfWidth, float halfDepth, (float R, float G, float B, float A) color, bool inFront = false) =>
        Draw(Shape(29, center, rotation, color, inFront, end: new Vec3(halfWidth, halfDepth, 0f)));

    /// <summary>
    /// Draws a line with no ends, flat, for a 2D camera, as Bevy's <c>primitive_2d</c> draws its
    /// <c>Line2d</c>, a line far out both ways through <paramref name="point"/> and an arrow along
    /// it.
    /// </summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="point">A point it passes through, on the XY plane.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z from the X axis, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void EndlessLine2d((float X, float Y) point, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true) =>
        Draw(Shape(30, Flat(point), Turn(angle), color, inFront));

    /// <summary>
    /// Draws a line with no ends, as Bevy's <c>primitive_3d</c> draws its <c>Line3d</c>, along X
    /// before <paramref name="rotation"/> turns it.
    /// </summary>
    /// <param name="point">A point it passes through.</param>
    /// <param name="rotation">Which way it runs.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void EndlessLine(Vec3 point, Quat rotation, (float R, float G, float B, float A) color, bool inFront = false) =>
        Draw(Shape(31, point, rotation, color, inFront));

    /// <summary>Draws the outline of a capsule, flat, for a 2D camera, its length along Y.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where the middle of it sits, on the XY plane.</param>
    /// <param name="radius">The radius of its round ends and half its width.</param>
    /// <param name="halfLength">Half the length of its straight sides.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Capsule2d((float X, float Y) center, float radius, float halfLength, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true) =>
        Draw(Shape(32, Flat(center), Turn(angle), color, inFront, radius: radius, end: new Vec3(halfLength, 0f, 0f)));

    /// <summary>Draws the outline of a ring between two circles, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where the middle of it sits, on the XY plane.</param>
    /// <param name="innerRadius">The radius of its inside.</param>
    /// <param name="outerRadius">The radius of its outside.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Annulus2d((float X, float Y) center, float innerRadius, float outerRadius, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true) =>
        Draw(Shape(33, Flat(center), Turn(angle), color, inFront, radius: outerRadius, end: new Vec3(innerRadius, 0f, 0f)));

    /// <summary>
    /// Draws the outline of a slice of a circle, its arc and the two radii to it, flat, for a 2D
    /// camera, the arc centered on Y.
    /// </summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">The center of the circle it is cut from, on the XY plane.</param>
    /// <param name="radius">The radius of that circle.</param>
    /// <param name="halfAngle">Half the angle of its arc, in radians.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void CircularSector2d((float X, float Y) center, float radius, float halfAngle, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true) =>
        Draw(Shape(34, Flat(center), Turn(angle), color, inFront, radius: radius, end: new Vec3(halfAngle, 0f, 0f)));

    /// <summary>
    /// Draws the outline of the part of a circle cut off by a chord, its arc and the chord, flat,
    /// for a 2D camera, the arc centered on Y.
    /// </summary>
    /// <inheritdoc cref="CircularSector2d"/>
    public static void CircularSegment2d((float X, float Y) center, float radius, float halfAngle, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true) =>
        Draw(Shape(35, Flat(center), Turn(angle), color, inFront, radius: radius, end: new Vec3(halfAngle, 0f, 0f)));

    /// <summary>Draws the outline of a polygon of equal sides, flat, for a 2D camera, a corner up.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="center">Where the middle of it sits, on the XY plane.</param>
    /// <param name="radius">How far its corners are from its middle.</param>
    /// <param name="sides">How many sides, three or more.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="angle">How far it is turned about Z, in radians.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void RegularPolygon2d((float X, float Y) center, float radius, int sides, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true) =>
        Draw(Shape(36, Flat(center), Turn(angle), color, inFront, radius: radius, end: new Vec3(Math.Max(sides, 3), 0f, 0f)));

    /// <summary>Draws lines through points in order, flat, for a 2D camera, closed back to the first where asked.</summary>
    /// <inheritdoc cref="Rect2d" path="/remarks"/>
    /// <param name="points">The points, on the XY plane.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="closed">Whether the last point is joined to the first, as a polygon is.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    public static void Polyline2d(ReadOnlySpan<(float X, float Y)> points, (float R, float G, float B, float A) color, bool closed = false, bool inFront = true)
    {
        if (points.Length < 2) return;
        var count = closed && points.Length > 2 ? points.Length : points.Length - 1;
        for (var i = 0; i < count; i++) Line2d(points[i], points[(i + 1) % points.Length], color, inFront);
    }
}

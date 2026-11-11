namespace Bevy;

/// <summary>
/// A disc centered on the origin cut by a chord, the part between the chord and an
/// <see cref="Arc2d"/>, Bevy's <c>CircularSegment</c>.
/// </summary>
/// <param name="Arc">The arc its round edge is.</param>
/// <remarks>
/// Symmetric about the Y axis as its arc is, its chord below the arc's midpoint.
/// <see cref="MeshShape.CircularSegment"/> builds its mesh from the same two measures.
/// </remarks>
public readonly record struct CircularSegment(Arc2d Arc) : IBounded2d
{
    /// <summary>A segment of a radius and a half angle in radians, Bevy's <c>new</c>.</summary>
    public CircularSegment(float radius, float halfAngle) : this(new Arc2d(radius, halfAngle))
    {
    }

    /// <summary>A segment whose arc spans <paramref name="angle"/> radians in all.</summary>
    public static CircularSegment FromRadians(float radius, float angle) => new(Arc2d.FromRadians(radius, angle));

    /// <summary>A segment whose arc spans <paramref name="angle"/> degrees in all.</summary>
    public static CircularSegment FromDegrees(float radius, float angle) => new(Arc2d.FromDegrees(radius, angle));

    /// <summary>A segment whose arc spans <paramref name="fraction"/> of a whole turn, half a turn a half disc.</summary>
    public static CircularSegment FromTurns(float radius, float fraction) => new(Arc2d.FromTurns(radius, fraction));

    /// <summary>The radius of the disc it is cut from.</summary>
    public float Radius => Arc.Radius;

    /// <summary>Half the angle its arc spans, in radians.</summary>
    public float HalfAngle => Arc.HalfAngle;

    /// <summary>The angle its arc spans in all, in radians.</summary>
    public float Angle => Arc.Angle;

    /// <summary>How long its round edge is.</summary>
    public float ArcLength => Arc.Length;

    /// <inheritdoc/>
    /// <remarks>Bevy's, the arc's box, which holds the chord, since the chord runs between the arc's ends.</remarks>
    public Aabb2d AabbAt(Isometry2d isometry) => Arc.AabbAt(isometry);

    /// <inheritdoc/>
    /// <remarks>Bevy's, the arc's circle.</remarks>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry) => Arc.BoundingCircleAt(isometry);
}

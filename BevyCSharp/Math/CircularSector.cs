namespace Bevy;

/// <summary>
/// A slice of a disc centered on the origin, its point at the center and its round edge an
/// <see cref="Arc2d"/>, Bevy's <c>CircularSector</c>.
/// </summary>
/// <param name="Arc">The arc its round edge is.</param>
/// <remarks>
/// Symmetric about the Y axis as its arc is, so a slice turned to start at the top and run clockwise,
/// as Bevy's <c>mesh2d_arcs</c> turns its slices, is turned by minus its half angle.
/// <see cref="MeshShape.CircularSector"/> builds its mesh from the same two measures.
/// </remarks>
public readonly record struct CircularSector(Arc2d Arc) : IBounded2d
{
    /// <summary>A sector of a radius and a half angle in radians, Bevy's <c>new</c>, which takes the half angle.</summary>
    public CircularSector(float radius, float halfAngle) : this(new Arc2d(radius, halfAngle))
    {
    }

    /// <summary>A sector spanning <paramref name="angle"/> radians in all.</summary>
    public static CircularSector FromRadians(float radius, float angle) => new(Arc2d.FromRadians(radius, angle));

    /// <summary>A sector spanning <paramref name="angle"/> degrees in all.</summary>
    public static CircularSector FromDegrees(float radius, float angle) => new(Arc2d.FromDegrees(radius, angle));

    /// <summary>A sector spanning <paramref name="fraction"/> of a whole turn, half a turn a half disc.</summary>
    public static CircularSector FromTurns(float radius, float fraction) => new(Arc2d.FromTurns(radius, fraction));

    /// <summary>The radius of the disc it is cut from.</summary>
    public float Radius => Arc.Radius;

    /// <summary>Half the angle it spans, in radians.</summary>
    public float HalfAngle => Arc.HalfAngle;

    /// <summary>The angle it spans in all, in radians.</summary>
    public float Angle => Arc.Angle;

    /// <summary>How long its round edge is.</summary>
    public float ArcLength => Arc.Length;

    /// <inheritdoc/>
    /// <remarks>Bevy's. The box of its arc with the disc's center taken in, and the disc's box for a whole turn or more.</remarks>
    public Aabb2d AabbAt(Isometry2d isometry) =>
        HalfAngle >= MathF.PI
            ? new Circle(Radius).AabbAt(isometry)
            : Aabb2d.FromPointCloud(new Isometry2d(isometry.Translation, Rot2.Identity), Arc.BoundingPoints(isometry.Rotation, withCenter: true));

    /// <inheritdoc/>
    /// <remarks>
    /// Bevy's. The disc's own circle for a sector of more than a half disc, and otherwise the circle
    /// about its point and its two ends, which curves more tightly than the disc and so holds all of
    /// it.
    /// </remarks>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry) =>
        Arc.IsMajor
            ? new BoundingCircle(isometry.Translation, Radius)
            : new Triangle2d(Vec2.Zero, Arc.LeftEndpoint, Arc.RightEndpoint).BoundingCircleAt(isometry);
}

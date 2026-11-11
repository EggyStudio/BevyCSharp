namespace Bevy;

/// <summary>
/// An arc of a circle centered on the origin, Bevy's <c>Arc2d</c>, symmetric about the Y axis and
/// opening down from its midpoint at the top.
/// </summary>
/// <param name="Radius">The radius of the circle it is part of.</param>
/// <param name="HalfAngle">Half the angle it spans, in radians, from its midpoint to either end.</param>
/// <remarks>
/// The arc a <see cref="CircularSector"/> and a <see cref="CircularSegment"/> are cut along. An arc
/// of a half angle of a quarter turn is the top half of the circle, and one of a half turn the
/// whole of it.
/// </remarks>
public readonly record struct Arc2d(float Radius, float HalfAngle) : IBounded2d
{
    /// <summary>An arc spanning <paramref name="angle"/> radians in all, Bevy's <c>from_radians</c>.</summary>
    public static Arc2d FromRadians(float radius, float angle) => new(radius, angle / 2f);

    /// <summary>An arc spanning <paramref name="angle"/> degrees in all, Bevy's <c>from_degrees</c>.</summary>
    public static Arc2d FromDegrees(float radius, float angle) => new(radius, angle * MathF.PI / 180f / 2f);

    /// <summary>An arc spanning <paramref name="fraction"/> of a whole turn, half a turn a semicircle, Bevy's <c>from_turns</c>.</summary>
    public static Arc2d FromTurns(float radius, float fraction) => new(radius, fraction * MathF.PI);

    /// <summary>The angle it spans in all, in radians.</summary>
    public float Angle => HalfAngle * 2f;

    /// <summary>How long it is along the circle.</summary>
    public float Length => Angle * Radius;

    /// <summary>Its end on the right, clockwise of its midpoint.</summary>
    public Vec2 RightEndpoint => Vec2.FromAngle(MathF.PI / 2f - HalfAngle) * Radius;

    /// <summary>Its end on the left, counterclockwise of its midpoint.</summary>
    public Vec2 LeftEndpoint => Vec2.FromAngle(MathF.PI / 2f + HalfAngle) * Radius;

    /// <summary>Its midpoint, at the top of the circle.</summary>
    public Vec2 Midpoint => new(0f, Radius);

    /// <summary>Half the length of the chord between its ends.</summary>
    public float HalfChordLength => Radius * MathF.Sin(HalfAngle);

    /// <summary>The length of the chord between its ends.</summary>
    public float ChordLength => 2f * HalfChordLength;

    /// <summary>The middle of the chord between its ends.</summary>
    public Vec2 ChordMidpoint => new(0f, Apothem);

    /// <summary>
    /// How far the chord is from the circle's center, below it for a major arc, as Bevy reads the
    /// apothem as a distance with a sign.
    /// </summary>
    public float Apothem => (IsMinor ? 1f : -1f) * MathF.Sqrt(Radius * Radius - HalfChordLength * HalfChordLength);

    /// <summary>How far the arc's midpoint is from its chord.</summary>
    public float Sagitta => Radius - Apothem;

    /// <summary>Whether it spans a half circle or less.</summary>
    public bool IsMinor => HalfAngle <= MathF.PI / 2f;

    /// <summary>Whether it spans a half circle or more.</summary>
    public bool IsMajor => HalfAngle >= MathF.PI / 2f;

    /// <inheritdoc/>
    /// <remarks>
    /// Bevy's. The box about the arc's ends and those of the circle's four extremes that the arc
    /// passes through where it is turned, and the circle's box for an arc of a whole turn or more.
    /// </remarks>
    public Aabb2d AabbAt(Isometry2d isometry) =>
        HalfAngle >= MathF.PI
            ? new Circle(Radius).AabbAt(isometry)
            : Aabb2d.FromPointCloud(new Isometry2d(isometry.Translation, Rot2.Identity), BoundingPoints(isometry.Rotation, withCenter: false));

    /// <inheritdoc/>
    /// <remarks>
    /// Bevy's. The circle the arc is part of for a major arc, and the circle on its chord for a
    /// minor one, which the arc never leaves.
    /// </remarks>
    public BoundingCircle BoundingCircleAt(Isometry2d isometry) =>
        IsMajor
            ? new BoundingCircle(isometry.Translation, Radius)
            : new BoundingCircle(isometry.Rotation * ChordMidpoint + isometry.Translation, HalfChordLength);

    /// <summary>
    /// The points the arc's box is found from where it is turned by <paramref name="rotation"/>, its
    /// two ends and each of the circle's extremes along the axes that the turned arc reaches, and the
    /// circle's center where asked, as a sector's box takes it.
    /// </summary>
    internal Vec2[] BoundingPoints(Rot2 rotation, bool withCenter)
    {
        var points = new List<Vec2>(7) { rotation * LeftEndpoint, rotation * RightEndpoint };

        // Measured from a quarter turn, Vec2.Y's angle, with the turn taken in, and kept in one turn.
        var left = Wrap(MathF.PI / 2f + HalfAngle + rotation.AsRadians);
        var right = Wrap(MathF.PI / 2f - HalfAngle + rotation.AsRadians);

        // Where the left end's angle has wrapped below the right one's, the arc covers the angles
        // outside the two rather than between them.
        var inverted = left < right;
        foreach (var extreme in (ReadOnlySpan<Vec2>)[new(1f, 0f), new(0f, 1f), new(-1f, 0f), new(0f, -1f)])
        {
            var angle = Wrap(extreme.ToAngle());
            var within = inverted ? angle >= right || angle <= left : angle >= right && angle <= left;
            if (within) points.Add(extreme * Radius);
        }

        if (withCenter) points.Add(Vec2.Zero);
        return [.. points];
    }

    private static float Wrap(float angle)
    {
        var turn = 2f * MathF.PI;
        var wrapped = angle % turn;
        return wrapped < 0f ? wrapped + turn : wrapped;
    }
}

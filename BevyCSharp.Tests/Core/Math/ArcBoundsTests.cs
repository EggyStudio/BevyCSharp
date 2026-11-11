using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the bounds of an arc, a disc's segment and a disc's sector, with Bevy's own cases from its
/// bounding tests at v0.19.1.
/// </summary>
/// <remarks>
/// An arc and the segment along it share their bounds, since the chord runs between the arc's ends,
/// and a sector adds the disc's center to the arc's points. The cases turn arcs so their ends and
/// the circle's extremes fall on the axes, where the box is decided by which extremes the arc
/// passes through.
/// </remarks>
public sealed class ArcBoundsTests
{
    private static readonly float Apothem = MathF.Sqrt(3f) / 2f;
    private static readonly float InverseRootThree = 1f / MathF.Sqrt(3f);

    // name, the arc's radius and angle in all, the turn, the translation, the box and the circle.
    public static TheoryData<string, float, float, float, Vec2, Vec2, Vec2, Vec2, float> ArcsAndSegments => new()
    {
        { "a sixth untransformed", 1f, MathF.PI / 3f, 0f, Vec2.Zero, new(-0.5f, Apothem), new(0.5f, 1f), new(0f, Apothem), 0.5f },
        { "a sixth of radius two", 2f, MathF.PI / 3f, 0f, Vec2.Zero, new(-1f, 2f * Apothem), new(1f, 2f), new(0f, 2f * Apothem), 1f },
        { "a sixth translated", 1f, MathF.PI / 3f, 0f, new(2f, 3f), new(1.5f, 3f + Apothem), new(2.5f, 4f), new(2f, 3f + Apothem), 0.5f },
        { "a sixth turned", 1f, MathF.PI / 3f, MathF.PI / 6f, Vec2.Zero, new(-Apothem, 0.5f), new(0f, 1f), new(-Apothem / 2f, Apothem * Apothem), 0.5f },
        { "a quarter turned onto the axes", 1f, MathF.PI / 2f, -MathF.PI / 4f, Vec2.Zero, Vec2.Zero, new(1f, 1f), new(0.5f, 0.5f), MathF.Sqrt(2f) / 2f },
        { "five sixths untransformed", 1f, 5f * MathF.PI / 3f, 0f, Vec2.Zero, new(-1f, -Apothem), new(1f, 1f), Vec2.Zero, 1f },
        { "five sixths turned", 1f, 5f * MathF.PI / 3f, MathF.PI / 6f, Vec2.Zero, new(-1f, -1f), new(1f, 1f), Vec2.Zero, 1f },
    };

    public static TheoryData<string, float, float, float, Vec2, Vec2, Vec2, Vec2, float> Sectors => new()
    {
        { "a third", 1f, 2f * MathF.PI / 3f, 0f, Vec2.Zero, new(-Apothem, 0f), new(Apothem, 1f), new(0f, 0.5f), Apothem },
        { "a sixth untransformed", 1f, MathF.PI / 3f, 0f, Vec2.Zero, new(-0.5f, 0f), new(0.5f, 1f), new(0f, InverseRootThree), InverseRootThree },
        { "a sixth translated", 1f, MathF.PI / 3f, 0f, new(2f, 3f), new(1.5f, 3f), new(2.5f, 4f), new(2f, 3f + InverseRootThree), InverseRootThree },
        { "a sixth turned", 1f, MathF.PI / 3f, MathF.PI / 6f, Vec2.Zero, new(-Apothem, 0f), new(0f, 1f), new(-InverseRootThree / 2f, 0.5f), InverseRootThree },
        { "a quarter turned onto the axes", 1f, MathF.PI / 2f, -MathF.PI / 4f, Vec2.Zero, Vec2.Zero, new(1f, 1f), new(0.5f, 0.5f), MathF.Sqrt(2f) / 2f },
        { "five sixths turned", 1f, 5f * MathF.PI / 3f, MathF.PI / 6f, Vec2.Zero, new(-1f, -1f), new(1f, 1f), Vec2.Zero, 1f },
    };

    /// <summary>An arc and the segment along it have the same box and circle as Bevy gives them.</summary>
    [Theory]
    [MemberData(nameof(ArcsAndSegments))]
    public void AnArcAndItsSegmentAreBoundedAsBevyBoundsThem(string name, float radius, float angle, float turn, Vec2 at, Vec2 min, Vec2 max, Vec2 center, float circleRadius)
    {
        var arc = Arc2d.FromRadians(radius, angle);
        var isometry = new Isometry2d(at, Rot2.Radians(turn));

        foreach (IBounded2d shape in new IBounded2d[] { arc, new CircularSegment(arc) })
        {
            var box = shape.AabbAt(isometry);
            Near(min, box.Min, name);
            Near(max, box.Max, name);

            var circle = shape.BoundingCircleAt(isometry);
            Near(center, circle.Center, name);
            Assert.Equal(circleRadius, circle.Radius, 4);
        }
    }

    /// <summary>A sector's box takes in the disc's center, and its circle is the one about its point and ends for less than a half disc.</summary>
    [Theory]
    [MemberData(nameof(Sectors))]
    public void ASectorIsBoundedAsBevyBoundsIt(string name, float radius, float angle, float turn, Vec2 at, Vec2 min, Vec2 max, Vec2 center, float circleRadius)
    {
        var sector = new CircularSector(Arc2d.FromRadians(radius, angle));
        var isometry = new Isometry2d(at, Rot2.Radians(turn));

        var box = sector.AabbAt(isometry);
        Near(min, box.Min, name);
        Near(max, box.Max, name);

        var circle = sector.BoundingCircleAt(isometry);
        Near(center, circle.Center, name);
        Assert.Equal(circleRadius, circle.Radius, 4);
    }

    /// <summary>A sector made from a fraction of a turn spans that fraction, its ends on either side of the top.</summary>
    [Fact]
    public void ASectorFromTurnsSpansThatFractionAboutTheTop()
    {
        var half = CircularSector.FromTurns(40f, 0.5f);
        Assert.Equal(MathF.PI / 2f, half.HalfAngle, 5);
        Assert.Equal(MathF.PI, half.Angle, 5);
        Near(new Vec2(40f, 0f), half.Arc.RightEndpoint, "right end");
        Near(new Vec2(-40f, 0f), half.Arc.LeftEndpoint, "left end");
        Assert.True(half.Arc.IsMinor && half.Arc.IsMajor);
    }

    private static void Near(Vec2 expected, Vec2 actual, string name)
    {
        Assert.True(MathF.Abs(expected.X - actual.X) < 1e-4f && MathF.Abs(expected.Y - actual.Y) < 1e-4f,
            $"{name}: expected ({expected.X}, {expected.Y}), got ({actual.X}, {actual.Y})");
    }
}

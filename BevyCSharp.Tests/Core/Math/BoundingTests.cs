using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers bounding volumes in the plane, their overlaps, a ray and a volume swept along one, and each shape's bounds, as Bevy's bounding math has them.</summary>
public sealed class BoundingTests
{
    private static readonly Aabb2d Unit = new(new Vec2(-1f, -1f), new Vec2(1f, 1f));

    private static RayCast2d Cast(float x, float y, float dx, float dy, float max = 90f) =>
        new(Ray2d.Toward(new Vec2(x, y), new Vec2(dx, dy)), max);

    /// <summary>A ray meets a circle where it first touches it, at once from inside, and not where it points away, passes beside or stops short.</summary>
    [Fact]
    public void ARayMeetsACircleWhereItFirstTouchesIt()
    {
        var circle = new BoundingCircle(Vec2.Zero, 1f);
        Assert.Equal(4f, Cast(0f, -5f, 0f, 1f).CircleIntersectionAt(circle)!.Value, 4);
        Assert.Equal(0f, Cast(0f, 0.5f, 0f, 1f).CircleIntersectionAt(circle));
        Assert.Null(Cast(0f, -5f, 0f, -1f).CircleIntersectionAt(circle));
        Assert.Null(Cast(2f, -5f, 0f, 1f).CircleIntersectionAt(circle));
        Assert.Null(Cast(0f, -5f, 0f, 1f, max: 3f).CircleIntersectionAt(circle));
    }

    /// <summary>A ray meets a box at its nearest side, at a corner along a diagonal, at once from inside, and not alongside it.</summary>
    [Fact]
    public void ARayMeetsABoxAtItsNearestSide()
    {
        Assert.Equal(4f, Cast(-5f, 0f, 1f, 0f).AabbIntersectionAt(Unit)!.Value, 4);
        Assert.Equal(MathF.Sqrt(32f), Cast(-5f, -5f, 1f, 1f).AabbIntersectionAt(Unit)!.Value, 3);
        Assert.Equal(0f, Cast(0f, 0f, 0f, 1f).AabbIntersectionAt(Unit));
        Assert.Null(Cast(-5f, 2f, 1f, 0f).AabbIntersectionAt(Unit));
        Assert.Null(Cast(5f, 0f, 1f, 0f).AabbIntersectionAt(Unit));
    }

    /// <summary>A box or a circle swept along a ray touches another of its kind when their edges meet, not their centers.</summary>
    [Fact]
    public void ASweptVolumeTouchesAnotherWhereTheirEdgesMeet()
    {
        var ray = Cast(-10f, 0f, 1f, 0f);
        Assert.Equal(13f, new AabbCast2d(Unit, ray).AabbCollisionAt(Aabb2d.FromCenter(new Vec2(5f, 0f), Vec2.One))!.Value, 4);
        Assert.Equal(13f, new BoundingCircleCast(new BoundingCircle(Vec2.Zero, 1f), ray).CircleCollisionAt(new BoundingCircle(new Vec2(5f, 0f), 1f))!.Value, 4);
        Assert.Null(new AabbCast2d(Unit, ray).AabbCollisionAt(Aabb2d.FromCenter(new Vec2(5f, 3f), Vec2.One)));
    }

    /// <summary>Boxes and circles overlap where they share a point, touching counting, a box's corner nearest a circle's middle deciding.</summary>
    [Fact]
    public void VolumesOverlapWhereTheyShareAPoint()
    {
        var box = new Aabb2d(Vec2.Zero, Vec2.One);
        Assert.True(box.Intersects(new Aabb2d(Vec2.One, new Vec2(2f))));
        Assert.False(box.Intersects(new Aabb2d(new Vec2(1.01f, 0f), new Vec2(2f))));
        Assert.False(box.Intersects(new BoundingCircle(new Vec2(2f), 1.41f)));
        Assert.True(box.Intersects(new BoundingCircle(new Vec2(2f), 1.42f)));
        Assert.True(new BoundingCircle(Vec2.Zero, 1f).Intersects(new BoundingCircle(new Vec2(2f, 0f), 1f)));
        Assert.False(new BoundingCircle(Vec2.Zero, 1f).Intersects(new BoundingCircle(new Vec2(2.01f, 0f), 1f)));
        Assert.Equal(new Vec2(1f, 0.5f), box.ClosestPoint(new Vec2(3f, 0.5f)));
    }

    /// <summary>Each shape's box and circle where it is placed, turned a quarter and moved as Bevy's bounds have them.</summary>
    [Fact]
    public void EachShapesBoundsAreTakenWhereItIsPlaced()
    {
        var turned = new Isometry2d(new Vec2(1f, 1f), Rot2.Radians(MathF.PI / 2f));

        var rectangle = Rectangle.FromSize(4f, 2f);
        Near(Aabb2d.FromCenter(new Vec2(1f, 1f), new Vec2(1f, 2f)), rectangle.AabbAt(turned));
        Assert.Equal(MathF.Sqrt(5f), rectangle.BoundingCircleAt(turned).Radius, 4);

        var capsule = new Capsule2d(1f, 2f);
        Near(new Aabb2d(new Vec2(-2f, 0f), new Vec2(4f, 2f)), capsule.AabbAt(turned));
        Assert.Equal(3f, capsule.BoundingCircleAt(turned).Radius);

        var segment = Segment2d.FromDirectionAndLength(new Vec2(1f, 0f), 4f);
        Near(new Aabb2d(new Vec2(1f, -1f), new Vec2(1f, 3f)), segment.AabbAt(turned));
        Assert.Equal(new BoundingCircle(new Vec2(1f, 1f), 2f), segment.BoundingCircleAt(turned));

        // A square's corners point along the axes, and a hexagon's reach across is its width at a corner.
        Near(Unit, new RegularPolygon(1f, 4).AabbAt(new Isometry2d(Vec2.Zero, Rot2.Identity)));
        Near(new Aabb2d(new Vec2(-MathF.Sqrt(3f) / 2f, -1f), new Vec2(MathF.Sqrt(3f) / 2f, 1f)), new RegularPolygon(1f, 6).AabbAt(new Isometry2d(Vec2.Zero, Rot2.Identity)));

        Assert.Equal(new BoundingCircle(new Vec2(2f, 3f), 5f), new Circle(5f).BoundingCircleAt(new Isometry2d(new Vec2(2f, 3f), Rot2.Identity)));
    }

    /// <summary>A triangle with a wide angle is held by the circle on the side across it, and one with none by the circle through its corners.</summary>
    [Fact]
    public void ATrianglesCircleIsTheSmallestThatHoldsIt()
    {
        var still = new Isometry2d(Vec2.Zero, Rot2.Identity);

        var wide = new Triangle2d(Vec2.Zero, new Vec2(4f, 0f), new Vec2(1f, 1f)).BoundingCircleAt(still);
        Near(new Vec2(2f, 0f), wide.Center);
        Assert.Equal(2f, wide.Radius, 4);

        var even = new Triangle2d(new Vec2(-1f, 0f), new Vec2(1f, 0f), new Vec2(0f, MathF.Sqrt(3f))).BoundingCircleAt(still);
        Near(new Vec2(0f, 1f / MathF.Sqrt(3f)), even.Center);
        Assert.Equal(2f / MathF.Sqrt(3f), even.Radius, 4);
    }

    /// <summary>A transform turned about Z places a shape turned the same, as Bevy's examples take it.</summary>
    [Fact]
    public void ATransformTurnedAboutZPlacesAShapeTurnedTheSame()
    {
        var placed = Isometry2d.FromTransform(new Transform(new Vec3(3f, 4f, 9f), Quat.FromRotationZ(0.5f), Vec3.One));
        Assert.Equal(new Vec2(3f, 4f), placed.Translation);
        Assert.Equal(0.5f, placed.Rotation.AsRadians, 4);
    }

    private static void Near(Vec2 expected, Vec2 actual)
    {
        Assert.Equal(expected.X, actual.X, 4);
        Assert.Equal(expected.Y, actual.Y, 4);
    }

    private static void Near(Aabb2d expected, Aabb2d actual)
    {
        Near(expected.Min, actual.Min);
        Near(expected.Max, actual.Max);
    }
}

using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's cubic splines made into curves and sampled along, as Bevy's <c>cubic_splines</c> has them.</summary>
public sealed class CubicSplineTests
{
    private static readonly Vec2[] Points = [new(-500f, -200f), new(-250f, 250f), new(250f, 250f), new(500f, -200f)];
    private static readonly Vec2[] Tangents = [new(0f, 200f), new(200f, 0f), new(0f, -200f), new(-200f, 0f)];

    /// <summary>A Hermite curve passes through each point at each whole <c>t</c>, along the tangent given there, and loops back where asked.</summary>
    [Fact]
    public void AHermiteCurvePassesThroughEachPointAlongItsTangent()
    {
        var spline = new CubicHermite<Vec2>(Points, Tangents);
        var curve = spline.ToCurve()!;
        Assert.Equal(3, curve.Segments.Count);
        for (var i = 0; i < Points.Length; i++)
        {
            Near(Points[i], curve.Position(i));
            Near(Tangents[i], curve.Velocity(i));
        }

        var loop = spline.ToCurveCyclic()!;
        Assert.Equal(4, loop.Segments.Count);
        Near(Points[0], loop.Position(4f));
        Assert.Null(new CubicHermite<Vec2>([Vec2.Zero], [Vec2.Zero]).ToCurve());
    }

    /// <summary>A Catmull-Rom curve passes through each point, leaving the first toward the second, and its loop starts at the first point.</summary>
    [Fact]
    public void ACatmullRomCurvePassesThroughEachPoint()
    {
        var spline = CubicCardinalSpline<Vec2>.CatmullRom(Points);
        var curve = spline.ToCurve()!;
        Assert.Equal(3, curve.Segments.Count);
        for (var i = 0; i < Points.Length; i++) Near(Points[i], curve.Position(i));

        // The end mirrored, so the curve leaves its first point along the line to the second.
        var start = curve.Velocity(0f);
        var toSecond = Points[1] - Points[0];
        Assert.Equal(0f, Vec2.PerpDot(start, toSecond), 1);

        var loop = spline.ToCurveCyclic()!;
        Assert.Equal(4, loop.Segments.Count);
        Near(Points[0], loop.Position(0f));
        Near(Points[1], loop.Position(1f));
        Near(Points[0], loop.Position(4f));
    }

    /// <summary>A B-spline is drawn toward its points without passing through them, from the mix of each three in a row.</summary>
    [Fact]
    public void ABSplineStartsAtTheMixOfItsFirstThreePoints()
    {
        var curve = new CubicBSpline<Vec2>(Points).ToCurve()!;
        Assert.Single(curve.Segments);

        // A uniform cubic B-spline starts at (P0 + 4 P1 + P2) / 6 and ends at (P1 + 4 P2 + P3) / 6.
        Near((Points[0] + Points[1] * 4f + Points[2]) * (1f / 6f), curve.Position(0f));
        Near((Points[1] + Points[2] * 4f + Points[3]) * (1f / 6f), curve.Position(1f));
        Assert.Null(new CubicBSpline<Vec2>(Points[..3]).ToCurve());
        Assert.Equal(4, new CubicBSpline<Vec2>(Points).ToCurveCyclic()!.Segments.Count);
    }

    /// <summary>A Bézier segment starts at its first point and ends at its fourth, and a curve is sampled at even steps from end to end.</summary>
    [Fact]
    public void ABezierEndsAtItsOuterPointsAndACurveIsSampledEvenly()
    {
        var curve = new CubicBezier<Vec3>([(Vec3.Zero, new Vec3(1f, 2f, 0f), new Vec3(3f, 2f, 0f), new Vec3(4f, 0f, 0f))]).ToCurve()!;
        Assert.Equal(Vec3.Zero, curve.Position(0f));
        Assert.Equal(new Vec3(4f, 0f, 0f), curve.Position(1f));
        Assert.Equal(new Vec3(2f, 1.5f, 0f), curve.Position(0.5f));

        var samples = new CubicHermite<Vec2>(Points, Tangents).ToCurve()!.IterPositions(300).ToList();
        Assert.Equal(301, samples.Count);
        Near(Points[0], samples[0]);
        Near(Points[3], samples[^1]);
        Near(Points[1], samples[100]);
    }

    private static void Near(Vec2 expected, Vec2 actual)
    {
        Assert.Equal(expected.X, actual.X, 2);
        Assert.Equal(expected.Y, actual.Y, 2);
    }
}

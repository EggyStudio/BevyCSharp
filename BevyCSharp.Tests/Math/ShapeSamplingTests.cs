using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers points sampled inside and on the surface of a box and a ball, as Bevy's <c>ShapeSample</c> takes them.</summary>
/// <remarks>
/// Each draws ten thousand points from a seeded source, enough that a share a face or a shell
/// should hold comes within a few hundredths of it every run, and the same seed draws them again.
/// </remarks>
public sealed class ShapeSamplingTests
{
    private const int Count = 10_000;

    /// <summary>A box's points inside it stay inside and spread across it, and its surface's land on a face as often as the face's area says.</summary>
    [Fact]
    public void ABoxsPointsStayInsideAndLandOnEachFaceByItsArea()
    {
        var box = Cuboid.FromSize(new Vec3(4f, 2f, 2f));
        var random = new Random(7);

        var inside = Enumerable.Range(0, Count).Select(_ => box.SampleInterior(random)).ToList();
        Assert.All(inside, p => Assert.True(MathF.Abs(p.X) <= 2f && MathF.Abs(p.Y) <= 1f && MathF.Abs(p.Z) <= 1f, $"{p} is outside"));
        Assert.InRange(inside.Average(p => p.X), -0.1, 0.1);
        Assert.InRange(inside.Count(p => p.X > 1f) / (double)Count, 0.22, 0.28);

        // The faces across X are 2 by 2, and those across Y and Z 4 by 2, a fifth and two fifths each.
        var surface = Enumerable.Range(0, Count).Select(_ => box.SampleBoundary(random)).ToList();
        int On(Func<Vec3, bool> face) => surface.Count(face);
        var (x, y, z) = (On(p => MathF.Abs(MathF.Abs(p.X) - 2f) < 1e-5f), On(p => MathF.Abs(MathF.Abs(p.Y) - 1f) < 1e-5f), On(p => MathF.Abs(MathF.Abs(p.Z) - 1f) < 1e-5f));
        Assert.Equal(Count, x + y + z);
        Assert.InRange(x / (double)Count, 0.17, 0.23);
        Assert.InRange(y / (double)Count, 0.37, 0.43);
        Assert.InRange(z / (double)Count, 0.37, 0.43);
    }

    /// <summary>A ball's points inside it fill it evenly, an eighth within half its radius, and its surface's are all at its radius.</summary>
    [Fact]
    public void ABallsPointsFillItEvenlyAndItsSurfacesAreAtItsRadius()
    {
        var ball = new Sphere(3f);
        var random = new Random(11);

        var inside = Enumerable.Range(0, Count).Select(_ => ball.SampleInterior(random)).ToList();
        Assert.All(inside, p => Assert.True(p.Length <= 3f + 1e-4f, $"{p} is outside"));
        Assert.InRange(inside.Count(p => p.Length <= 1.5f) / (double)Count, 0.10, 0.15);

        var surface = Enumerable.Range(0, Count).Select(_ => ball.SampleBoundary(random)).ToList();
        Assert.All(surface, p => Assert.Equal(3f, p.Length, 3));
        Assert.InRange(surface.Average(p => p.Z), -0.1, 0.1);
        Assert.InRange(surface.Count(p => p.Z > 0f) / (double)Count, 0.47, 0.53);
    }

    /// <summary>The same seed draws the same points.</summary>
    [Fact]
    public void TheSameSeedDrawsTheSamePoints()
    {
        var box = Cuboid.FromLength(2.9f);
        var (first, second) = (new Random(19878367), new Random(19878367));
        for (var i = 0; i < 50; i++) Assert.Equal(box.SampleBoundary(first), box.SampleBoundary(second));
    }
}

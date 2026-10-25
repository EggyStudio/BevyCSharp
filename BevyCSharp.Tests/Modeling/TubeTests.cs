using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// <see cref="EditMesh.Tube"/>, rings along a chain of points, square to it and untwisted, closed
/// and facing out, and the line of the recipe that makes it again.
/// </summary>
public sealed class TubeTests
{
    [Fact]
    public void AStraightTubeIsAPrismOfItsRings()
    {
        var mesh = new EditMesh().Tube([Vec3.Zero, Vec3.UnitY, new Vec3(0f, 2f, 0f)], [0.5f, 0.5f, 0.5f], 8);
        Assert.Equal(24, mesh.VertexCount);
        Assert.Equal(2 * 8 + 2, mesh.FaceCount);
        Assert.True(mesh.IsClosed);

        // Two meters of an octagon of radius a half, whose area is twice the square root of two
        // times the radius squared.
        Assert.Equal(2f * 2f * MathF.Sqrt(2f) * 0.25f, mesh.Volume, 4);
        for (var vertex = 0; vertex < mesh.VertexCount; vertex++)
        {
            var at = mesh.Position(vertex);
            Assert.Equal(0.5f, MathF.Sqrt(at.X * at.X + at.Z * at.Z), 5);
        }

        // Its sides are round, its ends flat, and the texture runs once round it.
        Assert.Equal(180f, mesh.SmoothOf(0));
        Assert.Equal(0f, mesh.SmoothOf(mesh.FaceCount - 1));
        Assert.Equal(1f, mesh.UvsOf(7)[1].X);
    }

    [Fact]
    public void ABentTubeKeepsItsRingsSquareAndUntwisted()
    {
        // A quarter of a circle of two meters in the XY plane, bending from up to along X.
        var points = Enumerable.Range(0, 9).Select(i =>
        {
            var angle = MathF.PI / 2f * i / 8f;
            return new Vec3(2f - 2f * MathF.Cos(angle), 2f * MathF.Sin(angle), 0f);
        }).ToArray();
        const int Sides = 6;
        var mesh = new EditMesh().Tube(points, Enumerable.Repeat(0.25f, points.Length).ToArray(), Sides);
        Assert.True(mesh.IsClosed);
        Assert.True(mesh.Volume > 0f, "the tube faces in");

        float? lean = null;
        for (var ring = 0; ring < points.Length; ring++)
        {
            // The ends square to their chords, and a point between two to the way midway between
            // its chords, which on a circle cut evenly is the way from the point before to the next.
            var way = (points[Math.Min(ring + 1, points.Length - 1)] - points[Math.Max(ring - 1, 0)]).Normalized;
            for (var side = 0; side < Sides; side++)
            {
                var offset = mesh.Position(ring * Sides + side) - points[ring];
                Assert.Equal(0.25f, offset.Length, 4);
                Assert.True(MathF.Abs(Vec3.Dot(offset, way)) < 1e-3f, $"ring {ring} leans {Vec3.Dot(offset, way)} along the chain");
            }

            // A bend in a plane turns the rings about the plane's normal alone, so each ring's first
            // corner stands as far out of the plane as the first ring's.
            var outOfPlane = (mesh.Position(ring * Sides) - points[ring]).Z;
            lean ??= outOfPlane;
            Assert.Equal(lean.Value, outOfPlane, 3);
        }
    }

    [Fact]
    public void ATubeIsMadeAgainFromItsLine()
    {
        var mesh = new EditMesh().Tube([new Vec3(0.1f, 0f, 0.3f), new Vec3(0.2f, 1.3f, 0.1f), new Vec3(1f / 3f, 2f, 0.7f)], [0.3f, 0.2f, 0.1f], 5);
        var line = Assert.Single(mesh.Recipe);
        Assert.StartsWith("tube 5 0.1 0 0.3 0.3 ", line);

        var again = new EditMesh().Run(line);
        Assert.Equal(mesh.VertexCount, again.VertexCount);
        for (var vertex = 0; vertex < mesh.VertexCount; vertex++) Assert.Equal(mesh.Position(vertex), again.Position(vertex));
    }

    [Fact]
    public void ATubeWithNoShapeIsRefused()
    {
        Assert.Throws<ArgumentException>(() => new EditMesh().Tube([Vec3.Zero], [1f], 4));
        Assert.Throws<ArgumentException>(() => new EditMesh().Tube([Vec3.Zero, Vec3.UnitY], [1f], 4));
        Assert.Throws<ArgumentException>(() => new EditMesh().Tube([Vec3.Zero, Vec3.Zero, Vec3.UnitY], [1f, 1f, 1f], 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EditMesh().Tube([Vec3.Zero, Vec3.UnitY], [1f, 0f], 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EditMesh().Tube([Vec3.Zero, Vec3.UnitY], [1f, 1f], 2));
        Assert.Throws<FormatException>(() => new EditMesh().Run("tube 4 0 0 0 1 0 1 0"));
    }
}

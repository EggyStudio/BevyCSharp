using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the operations that cut along edges, the bevel, the loop cut and the subdivision, on the
/// exact counts a box gives, whether every edge is shared by two faces running along it in opposite
/// directions after, and the volume each leaves.
/// </summary>
/// <remarks>
/// A cube of two meters with its twelve edges beveled holds less than the cube and more than the
/// chamfered cube inside the bevel, whose corners lie on the round profile too, and less than the
/// cube rounded by a true quarter circle, whose volume is the inner box, the six slabs, the twelve
/// quarter cylinders and the eight eighths of a sphere. A loop cut and a subdivision change the
/// faces and leave the shape, so the volume they leave is the one they found.
/// </remarks>
public sealed class BevelTests
{
    private const float Close = 1e-4f;

    private static EditMesh Cube() => new EditMesh().Box(new Vec3(2f));

    private static EditMesh OneEdgeOf(EditMesh mesh) => mesh.SelectEdges([mesh.Edges().First()]);

    /// <summary>The volume of a cube of a side rounded along every edge by a quarter circle of a radius.</summary>
    private static float Rounded(float side, float radius)
    {
        var inner = side - 2f * radius;
        return inner * inner * inner + 6f * radius * inner * inner + 3f * MathF.PI * radius * radius * inner + 4f / 3f * MathF.PI * radius * radius * radius;
    }

    [Fact]
    public void ACubesTwelveEdgesBeveledFlatAreACubeOfTwentySixFaces()
    {
        var mesh = Cube().SelectAll().Bevel(0.25f);

        // Six smaller sides, a strip for each edge and a triangle at each corner.
        Assert.Equal((24, 48, 26), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);

        // The cube less a prism of half a square of the width along each edge's middle, and at each
        // corner a cube of the width less the sixth of it beyond the corner's triangle.
        var w = 0.25f;
        Assert.Equal(8f - 12f * (w * w / 2f) * (2f - 2f * w) - 8f * (5f / 6f) * w * w * w, mesh.Volume, Close);

        // The strips and the corners are selected, and a side is a square half a meter smaller.
        Assert.Equal(20, mesh.SelectedFaceCount);
        var side = Enumerable.Range(0, mesh.FaceCount).Single(face => (mesh.Normal(face) - Vec3.UnitY).Length < Close);
        var corners = mesh.Corners(side).ToArray().Select(mesh.Position).ToArray();
        Assert.Equal(1.5f, corners.Max(point => point.X) - corners.Min(point => point.X), Close);
    }

    [Fact]
    public void ACubesTwelveEdgesBeveledInTwoSegmentsAreWatertightAndBetweenTheCubeAndItsChamfer()
    {
        var mesh = Cube().SelectAll().Bevel(0.25f, 2);

        // A point more across each strip's end, a strip of two faces an edge, and three four sided
        // faces round a point in the middle of each corner.
        Assert.Equal((56, 108, 54), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);

        var chamfer = Cube().SelectAll().Bevel(0.25f).Volume;
        Assert.InRange(mesh.Volume, chamfer + 0.001f, Rounded(2f, 0.25f) - 0.001f);
        Assert.True(Rounded(2f, 0.25f) < 8f);

        // Every new point lies on the rounded cube, a quarter meter from the box a quarter meter in.
        for (var vertex = 0; vertex < mesh.VertexCount; vertex++)
        {
            var point = mesh.Position(vertex);
            var nearest = new Vec3(Math.Clamp(point.X, 0.25f, 1.75f), Math.Clamp(point.Y, 0.25f, 1.75f), Math.Clamp(point.Z, 0.25f, 1.75f));
            Assert.Equal(0.25f, (point - nearest).Length, 1e-3f);
        }

        // More segments come nearer the rounded cube and stay closed.
        var fine = Cube().SelectAll().Bevel(0.25f, 8);
        Assert.True(fine.IsClosed);
        Assert.InRange(fine.Volume, mesh.Volume, Rounded(2f, 0.25f));
    }

    [Fact]
    public void OneEdgeBeveledCutsTheCornersOfTheTwoFacesAtItsEnds()
    {
        // The edge from vertex 0 to vertex 4 stands up the cube's first corner.
        var mesh = Cube().SelectEdges([(0, 4)]).Bevel(0.5f);

        Assert.Equal((10, 15, 7), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(8f - 0.5f * 0.5f / 2f * 2f, mesh.Volume, Close);

        // The top and the bottom are five sided, their corner cut.
        Assert.Equal(2, Enumerable.Range(0, mesh.FaceCount).Count(face => mesh.Corners(face).Length == 5));

        // Round, the two end faces take the profile's points and stay flat.
        var round = Cube().SelectEdges([(0, 4)]).Bevel(0.5f, 4);
        Assert.Equal((16, 24, 10), (round.VertexCount, round.EdgeCount, round.FaceCount));
        Assert.True(round.IsClosed);
        // The corner's square less the four triangles from the profile's center to its segments.
        Assert.Equal(8f - (0.25f - 4f * 0.5f * 0.25f * MathF.Sin(MathF.PI / 8f)) * 2f, round.Volume, Close);
    }

    [Fact]
    public void TheTopsFourEdgesBeveledTurnItsCornersWithNoPatch()
    {
        var mesh = Cube().SelectFacing(Vec3.UnitY).Bevel(0.25f);

        // Each corner of the top two points, one in from the top's edges and one down the side.
        Assert.Equal((12, 20, 10), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        // Four prisms of half a square of the width along the top's edges, two meeting at each
        // corner over a third of a cube of the width, counted once.
        Assert.Equal(8f - 4f * (0.25f * 0.25f / 2f) * 2f + 4f * 0.25f * 0.25f * 0.25f / 3f, mesh.Volume, Close);

        var round = Cube().SelectFacing(Vec3.UnitY).Bevel(0.25f, 2);
        Assert.Equal((16, 28, 14), (round.VertexCount, round.EdgeCount, round.FaceCount));
        Assert.True(round.IsClosed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EverySolidPrimitiveBeveledWholeOrInPartStaysClosedAndHoldsLess(int segments)
    {
        // Faces of many corners, concave corners, faces facing in, corners of four edges and runs
        // of edges that turn, each beveled by five centimeters.
        var shapes = new Func<EditMesh>[]
        {
            () => new EditMesh().Cylinder(1f, 2f, 8).SelectAll(),
            () => new EditMesh().Cylinder(1f, 2f, 8).SelectFacing(Vec3.UnitY),
            () => new EditMesh().Cone(1f, 2f, 6).SelectAll(),
            () => new EditMesh().Wedge(new Vec3(1f)).SelectAll(),
            () => new EditMesh().Stairs(3, 0.2f, 0.3f, 1f).SelectAll(),
            () => new EditMesh().Arch(1f, 0.2f, 0.4f, 6).SelectAll(),
            () => new EditMesh().Room(new Vec3(4f, 3f, 4f), 0.2f).SelectAll(),
            () => Cube().SelectAll().Subdivide(),
            () => Cube().SelectAll().Subdivide().SelectFacing(Vec3.UnitY),
            () => OneEdgeOf(Cube().SelectAll().Subdivide()),
            () => Cube().SelectEdges([(0, 4), (4, 5), (5, 6)]),
            () => Cube().SelectFacing(Vec3.UnitY).Inset(0.4f).Extrude(0.5f).SelectAll(),
        };

        foreach (var make in shapes)
        {
            var mesh = make();
            var volume = mesh.Volume;
            mesh.Bevel(0.05f, segments);

            Assert.True(mesh.IsClosed, mesh.RecipeText);
            Assert.InRange(mesh.Volume, volume * 0.9f, volume - 1e-4f);
        }
    }

    [Fact]
    public void ABevelOfNothingThatCanBeBeveledIsRefusedAndAMeasureOutOfRangeToo()
    {
        Assert.Throws<InvalidOperationException>(() => Cube().SelectNone().Bevel(0.1f));
        Assert.Throws<InvalidOperationException>(() => new EditMesh().Plane(2f, 2f).SelectAll().Bevel(0.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().SelectAll().Bevel(0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().SelectAll().Bevel(0.1f, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().SelectAll().Bevel(0.1f, EditMesh.MostSegments + 1));
        Assert.Throws<ArgumentException>(() => Cube().SelectEdges([(0, 6)]));
    }

    [Fact]
    public void ALoopCutAcrossAnUprightEdgeOfACubeRunsRoundItsFourSides()
    {
        var mesh = Cube().SelectEdges([(0, 4)]).LoopCut(0.25f);

        Assert.Equal((12, 20, 10), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(8f, mesh.Volume, Close);

        // The loop's four points are selected, a quarter of the way up from the lower end of the
        // selected edge, and with them the loop's four edges.
        Assert.Equal((4, 0, 4), (mesh.SelectedVertexCount, mesh.SelectedFaceCount, mesh.SelectedEdgeCount));
        for (var vertex = 0; vertex < mesh.VertexCount; vertex++)
        {
            if (mesh.IsVertexSelected(vertex)) Assert.Equal(0.5f, mesh.Position(vertex).Y, Close);
        }

        // Two edges of one ring are cut by one loop, and two of different rings are refused.
        Assert.Equal(12, Cube().SelectEdges([(0, 4), (2, 6)]).LoopCut(0.5f).VertexCount);
        Assert.Throws<InvalidOperationException>(() => Cube().SelectEdges([(0, 4), (0, 1)]).LoopCut(0.5f));
        Assert.Throws<InvalidOperationException>(() => Cube().SelectNone().LoopCut(0.5f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().SelectEdges([(0, 4)]).LoopCut(1f));
    }

    [Fact]
    public void ALoopCutAcrossAFlightsWidthRunsRoundEveryTreadAndRiser()
    {
        // The ring across an edge along the flight's width runs up each riser and along each
        // tread, down the back and along the floor, and comes round.
        var stairs = new EditMesh().Stairs(2, 0.5f, 0.5f, 1f);
        var (before, volume) = (stairs.FaceCount, stairs.Volume);
        var width = stairs.Edges().First(edge => MathF.Abs((stairs.Position(edge.A) - stairs.Position(edge.B)).X) > 0.5f);
        stairs.SelectEdges([width]).LoopCut(0.5f);

        // The six faces round the outline are cut along the flight's middle, and the two sides
        // keep their outlines.
        Assert.True(stairs.IsClosed);
        Assert.Equal(before + 6, stairs.FaceCount);
        Assert.Equal(volume, stairs.Volume, Close);

        // A ring that meets a face of another count stops there, and the face takes the point. A
        // wedge's ring across the edge its slope rises from runs over the slope, the back and the
        // floor, and its three sided ends take a corner each.
        var wedge = new EditMesh().Wedge(new Vec3(1f));
        var foot = wedge.Edges().First(edge => MathF.Abs((wedge.Position(edge.A) - wedge.Position(edge.B)).X) > 0.5f);
        wedge.SelectEdges([foot]).LoopCut(0.5f);
        Assert.True(wedge.IsClosed);
        Assert.Equal(0.5f, wedge.Volume, Close);
    }

    [Fact]
    public void ACubeSubdividedOnceIsTwentyFourFacesOnTwentySixVertices()
    {
        var once = Cube().SelectAll().Subdivide();
        Assert.Equal((26, 48, 24), (once.VertexCount, once.EdgeCount, once.FaceCount));
        Assert.True(once.IsClosed);
        Assert.Equal(8f, once.Volume, Close);
        Assert.Equal(24, once.SelectedFaceCount);

        var twice = once.Subdivide();
        Assert.Equal((98, 192, 96), (twice.VertexCount, twice.EdgeCount, twice.FaceCount));
        Assert.True(twice.IsClosed);

        var twoCuts = Cube().SelectAll().Subdivide(2);
        Assert.Equal((56, 108, 54), (twoCuts.VertexCount, twoCuts.EdgeCount, twoCuts.FaceCount));
        Assert.True(twoCuts.IsClosed);
    }

    [Fact]
    public void AFaceSubdividedAloneLeavesItsNeighborsJoinedToIt()
    {
        // The top in a grid of three a side, each side taking the top edge's two points.
        var mesh = Cube().SelectFacing(Vec3.UnitY).Subdivide(2);

        Assert.Equal((8 + 8 + 4, 5 + 9), (mesh.VertexCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(8f, mesh.Volume, Close);
        Assert.Equal(4, Enumerable.Range(0, mesh.FaceCount).Count(face => mesh.Corners(face).Length == 6));
    }

    [Fact]
    public void TrianglesAndFacesOfManyCornersSubdivideClosed()
    {
        // A cone's base of eight corners becomes eight faces round its middle, and its triangles
        // four each.
        var cone = new EditMesh().Cone(1f, 2f, 8).SelectAll();
        var volume = cone.Volume;
        cone.Subdivide();
        Assert.True(cone.IsClosed);
        Assert.Equal(8 + 8 * 4, cone.FaceCount);
        Assert.Equal(volume, cone.Volume, Close);

        var thrice = new EditMesh().Cone(1f, 2f, 5).SelectAll().Subdivide(3);
        Assert.True(thrice.IsClosed);
        Assert.Equal(5 + 5 * 16, thrice.FaceCount);
    }

    [Fact]
    public void TheOperationsWriteTheirLinesAndAreMadeAgainFromThem()
    {
        var mesh = Cube().SelectEdges([(0, 4)]).LoopCut(0.5f).SelectAll().Bevel(0.1f, 3).SelectFacing(Vec3.UnitY).Subdivide(2);

        Assert.Equal(new[] { "box 2 2 2", "select edge 0 4", "loopcut 0.5", "select all", "bevel 0.1 3", "select +y", "subdivide 2" }, mesh.Recipe);
        Assert.Equal("bevel 0.1", Cube().SelectAll().Bevel(0.1f).Recipe[^1]);
        Assert.Equal("subdivide", Cube().SelectAll().Subdivide().Recipe[^1]);

        var again = EditMesh.FromRecipe(mesh.RecipeText);
        Assert.Equal((mesh.VertexCount, mesh.FaceCount), (again.VertexCount, again.FaceCount));
        Assert.True(again.IsClosed);
        Assert.Equal(mesh.Volume, again.Volume, Close);

        Assert.True(mesh.Undo());
        Assert.Equal(("select +y", 0), (mesh.Recipe[^1], mesh.OpenEdges));
    }

    [Fact]
    public void AFacesOwnCoordinatesFollowItThroughACut()
    {
        var mesh = Cube().SelectAll().UvBox().SelectEdges([(0, 4)]).LoopCut(0.5f).SelectAll().Subdivide();

        for (var face = 0; face < mesh.FaceCount; face++)
        {
            Assert.True(mesh.HasOwnUvs(face));
            var uvs = mesh.UvsOf(face);
            var corners = mesh.Corners(face).ToArray();

            // A box projection's coordinates are the corners' places in meters along two axes, so
            // each corner's are as far apart as the corners are.
            for (var i = 0; i < corners.Length; i++)
            {
                var j = (i + 1) % corners.Length;
                Assert.Equal((mesh.Position(corners[i]) - mesh.Position(corners[j])).Length, (uvs[i] - uvs[j]).Length, Close);
            }
        }
    }
}

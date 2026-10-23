using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the face and edge operations that join, close, cut across a plane, copy across one, sweep
/// round an axis and ease vertices, the bridge, fill, dissolve, knife, mirror, spin and smooth, on
/// exact counts, whether the mesh is closed after and the volume it holds.
/// </summary>
/// <remarks>
/// A box's faces are numbered as its primitive makes them, the bottom first, the top second and the
/// four sides after, and its vertices the bottom's four and then the top's, so a face or an edge is
/// named by its index here as a recipe names it.
/// </remarks>
public sealed class FillAndMirrorTests
{
    private const float Close = 1e-4f;

    private static EditMesh Cube() => new EditMesh().Box(new Vec3(2f));

    private static int Facing(EditMesh mesh, Vec3 way) => Enumerable.Range(0, mesh.FaceCount).Single(face => (mesh.Normal(face) - way).Length < Close);

    [Fact]
    public void TwoBoxesBridgedTopToBottomAreOneSolidThreeTimesAsTall()
    {
        // The first box's top is face 1, and the second's bottom its first face, face 6.
        var mesh = Cube().Box(new Vec3(2f), new Vec3(0f, 4f, 0f)).SelectFaces([1, 6]).Bridge();

        Assert.Equal((16, 28, 14), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(24f, mesh.Volume, Close);
        Assert.Equal((new Vec3(0f), new Vec3(2f, 6f, 2f)), mesh.Bounds);

        // The band of four faces between is selected, each a wall two meters square.
        Assert.Equal(4, mesh.SelectedFaceCount);
        for (var face = 0; face < mesh.FaceCount; face++)
        {
            if (mesh.IsFaceSelected(face)) Assert.Equal(8f, mesh.Area(face).Length, Close);
        }
    }

    [Fact]
    public void ABridgePairsTheNearestCornersWhateverTheSecondRimStartsAt()
    {
        // The upper box turned a quarter about its middle, so its corners are numbered from
        // another corner than the lower's, and the band still stands straight.
        var mesh = Cube().Box(new Vec3(2f), new Vec3(0f, 4f, 0f)).Rotate(Vec3.UnitY, 90f).SelectFaces([1, 6]).Bridge();

        Assert.True(mesh.IsClosed);
        Assert.Equal(24f, mesh.Volume, Close);
    }

    [Fact]
    public void ABridgeOfOneRegionOrOfRimsOfDifferentCountsIsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => Cube().SelectFaces([1]).Bridge());
        Assert.Throws<InvalidOperationException>(() => Cube().Cylinder(1f, 1f, 8, new Vec3(0f, 4f, 0f)).SelectFaces([1, 6]).Bridge());
    }

    [Fact]
    public void ATopTakenAwayIsFilledAgainByOneFaceFacingUp()
    {
        var mesh = Cube().SelectFacing(Vec3.UnitY).Delete().SelectAll().Fill();

        Assert.Equal((8, 12, 6), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(8f, mesh.Volume, Close);
        Assert.Equal(Vec3.UnitY, mesh.Normal(Enumerable.Range(0, mesh.FaceCount).Single(mesh.IsFaceSelected)));

        Assert.Throws<InvalidOperationException>(() => Cube().SelectAll().Fill());
    }

    [Fact]
    public void ATopSubdividedAndDissolvedIsOneFaceOfEightCorners()
    {
        var mesh = Cube().SelectAll().Subdivide().SelectFacing(Vec3.UnitY).DissolveFaces();

        Assert.Equal((25, 44, 21), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(8f, mesh.Volume, Close);
        Assert.Equal(8, mesh.Corners(Facing(mesh, Vec3.UnitY)).Length);

        // A whole closed box has no rim to make one face of, and is left.
        Assert.Equal(6, Cube().SelectAll().DissolveFaces().FaceCount);
    }

    [Fact]
    public void ALoopCutsLoopDissolvedPutsTheBoxBack()
    {
        var mesh = Cube().SelectEdges([(0, 4)]).LoopCut(0.5f).DissolveEdges();

        Assert.Equal((8, 12, 6), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(8f, mesh.Volume, Close);
    }

    [Fact]
    public void AKnifeAcrossABoxCutsTheFacesItCrossesAndLeavesItClosed()
    {
        // Through the middle along Y, the four sides cut in two round a loop.
        var level = Cube().SelectAll().Knife(Vec3.UnitY);
        Assert.Equal((12, 20, 10), (level.VertexCount, level.EdgeCount, level.FaceCount));
        Assert.True(level.IsClosed);
        Assert.Equal(8f, level.Volume, Close);
        Assert.Equal(10, level.SelectedFaceCount);

        // A plane through two opposite upright edges cuts the top and the bottom corner to corner
        // with no new vertex, and leaves the sides it only touches whole.
        var diagonal = Cube().SelectAll().Knife(new Vec3(1f, 0f, 1f));
        Assert.Equal((8, 14, 8), (diagonal.VertexCount, diagonal.EdgeCount, diagonal.FaceCount));
        Assert.True(diagonal.IsClosed);

        // At a point given, a quarter of the way up.
        var low = Cube().SelectAll().Knife(Vec3.UnitY, new Vec3(0f, 0.5f, 0f));
        Assert.Equal(4, Enumerable.Range(0, low.VertexCount).Count(vertex => MathF.Abs(low.Position(vertex).Y - 0.5f) < Close));

        // Only the selected faces are cut, and a side beside a cut top takes the cut's point.
        var top = Cube().SelectFacing(Vec3.UnitY).Knife(Vec3.UnitX);
        Assert.Equal((10, 7), (top.VertexCount, top.FaceCount));
        Assert.True(top.IsClosed);
    }

    [Fact]
    public void HalfABoxMirroredIsAWholeOneWithNoVertexTwice()
    {
        // The half from the plane at X of zero, open where it meets it.
        var open = new EditMesh().Box(new Vec3(1f, 2f, 2f)).SelectFacing(-Vec3.UnitX).Delete().SelectAll().Mirror(Vec3.UnitX);
        Assert.Equal((12, 20, 10), (open.VertexCount, open.EdgeCount, open.FaceCount));
        Assert.True(open.IsClosed);
        Assert.Equal(8f, open.Volume, Close);
        Assert.Equal((new Vec3(-1f, 0f, 0f), new Vec3(1f, 2f, 2f)), open.Bounds);
        Assert.Equal(open.VertexCount, Enumerable.Range(0, open.VertexCount).Select(open.Position).Distinct().Count());
        Assert.Equal(10, open.SelectedFaceCount);

        // Closed, its face on the plane would lie inside the whole and goes.
        var closed = new EditMesh().Box(new Vec3(1f, 2f, 2f)).SelectAll().Mirror(Vec3.UnitX);
        Assert.Equal((12, 10), (closed.VertexCount, closed.FaceCount));
        Assert.True(closed.IsClosed);
        Assert.Equal(8f, closed.Volume, Close);

        // A vertex within a millimeter of the plane is put on it and shared.
        var near = new EditMesh().Box(new Vec3(1f, 2f, 2f), new Vec3(0.0005f, 0f, 0f)).SelectFacing(-Vec3.UnitX).Delete().SelectAll().Mirror(Vec3.UnitX);
        Assert.True(near.IsClosed);
        Assert.Equal(12, near.VertexCount);

        // Across a plane apart from it, a second box.
        var apart = Cube().Mirror(Vec3.UnitX, new Vec3(-1f, 0f, 0f));
        Assert.Equal((16, 12), (apart.VertexCount, apart.FaceCount));
        Assert.True(apart.IsClosed);
        Assert.Equal(16f, apart.Volume, Close);
    }

    [Fact]
    public void ATopsEdgesSpunRoundAnAxisSweepAFaceAStepAnEdge()
    {
        var quarter = new EditMesh().Box(new Vec3(2f), new Vec3(3f, 0f, 0f)).SelectFacing(Vec3.UnitY).Spin(Vec3.UnitY, 90f, 4);
        Assert.Equal((24, 22), (quarter.VertexCount, quarter.FaceCount));
        Assert.Equal(16, quarter.SelectedFaceCount);

        // A whole turn joins the last step to the edges themselves.
        var whole = new EditMesh().Box(new Vec3(2f), new Vec3(3f, 0f, 0f)).SelectFacing(Vec3.UnitY).Spin(Vec3.UnitY, 360f, 8);
        Assert.Equal((8 + 7 * 4, 6 + 8 * 4), (whole.VertexCount, whole.FaceCount));

        Assert.Throws<InvalidOperationException>(() => Cube().SelectNone().Spin(Vec3.UnitY, 90f, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().Spin(Vec3.UnitY, 0f, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().Spin(Vec3.UnitY, 90f, 0));
    }

    [Fact]
    public void ASquareStandingBesideTheAxisSpunAWholeTurnIsACylinder()
    {
        // A meter square standing in the XY plane with an edge on the Y axis, swept round it in
        // sixteen steps, its corners on the axis staying put, and the square taken away after.
        var mesh = new EditMesh().Plane(1f, 1f).SelectAll().Rotate(Vec3.UnitX, -90f, Vec3.Zero).Spin(Vec3.UnitY, 360f, 16).SelectFaces([0]).Delete();

        Assert.True(mesh.IsClosed);
        Assert.Equal(4 + 15 * 2, mesh.VertexCount);
        Assert.Equal(16 * 3, mesh.FaceCount);

        // The square faces toward -Z, the way its far edge turns first, so the sweep's faces run
        // as a solid whose start is the square would and face in, and flipped they face out.
        var prism = 16f * 0.5f * MathF.Sin(MathF.Tau / 16f);
        Assert.Equal(-prism, mesh.Volume, Close);
        Assert.Equal(prism, mesh.SelectAll().Flip().Volume, Close);
    }

    [Fact]
    public void ABoxsCornersSmoothedByAHalfMoveASixthOfItsSideIn()
    {
        var mesh = Cube().SelectAll().Smooth(0.5f);

        Assert.Equal((8, 6), (mesh.VertexCount, mesh.FaceCount));
        Assert.Equal(1f / 3f, mesh.Bounds.Low.X, Close);
        Assert.Equal(5f / 3f, mesh.Bounds.High.Z, Close);

        // Twice, from where the first pass left them, and only the selected vertices move.
        Assert.Equal(5f / 9f, Cube().SelectAll().Smooth(0.5f, 2).Bounds.Low.Y, Close);
        Assert.Equal(new Vec3(2f), Cube().SelectFacing(-Vec3.UnitY).Smooth(1f).Bounds.High);
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().Smooth(0f));
    }

    [Fact]
    public void TheOperationsWriteTheirLinesAndAreMadeAgainFromThem()
    {
        var mesh = new EditMesh()
            .Box(new Vec3(1f, 2f, 2f))
            .SelectAll()
            .Mirror(Vec3.UnitX)
            .Knife(Vec3.UnitY, new Vec3(0f, 1f, 0f))
            .SelectFacing(Vec3.UnitY)
            .DissolveFaces()
            .Delete()
            .SelectAll()
            .Fill()
            .SelectEdges([(0, 1)])
            .DissolveEdges()
            .SelectAll()
            .Smooth(0.25f, 3)
            .Box(new Vec3(1f), new Vec3(0f, 5f, 0f))
            .SelectFaces([1, 10])
            .Bridge()
            .SelectFacing(Vec3.UnitX)
            .Spin(new Vec3(0f, 1f, 1f), 45f, 3, new Vec3(1f, 0f, 0f));

        Assert.Equal(
            new[]
            {
                "box 1 2 2", "select all", "mirror x", "knife y at 0 1 0", "select +y", "dissolve", "delete", "select all", "fill",
                "select edge 0 1", "dissolve edges", "select all", "smooth 0.25 3", "box 1 1 1 at 0 5 0", "select face 1 10", "bridge",
                "select +x", "spin 0 1 1 45 3 at 1 0 0",
            },
            mesh.Recipe);

        var again = EditMesh.FromRecipe(mesh.RecipeText);
        Assert.Equal((mesh.VertexCount, mesh.FaceCount, mesh.OpenEdges), (again.VertexCount, again.FaceCount, again.OpenEdges));
        Assert.Equal(mesh.Volume, again.Volume, Close);
    }
}

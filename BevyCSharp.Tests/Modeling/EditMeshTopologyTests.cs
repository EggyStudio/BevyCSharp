using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the operations that change which vertices and faces an edit mesh has, extrude, inset,
/// delete, merge, flip, triangulate and duplicate, on exact counts, the volume a closed mesh holds
/// and whether every edge is shared by two faces running along it in opposite directions.
/// </summary>
/// <remarks>
/// A box of two by three by four holds 24 cubic meters and its top is two by four, so pushing the
/// top up a meter adds eight and an inset of the top changes nothing it holds. A mesh left closed
/// with its faces facing out after an operation is one the drawing and the collider take as a
/// solid.
/// </remarks>
public sealed class EditMeshTopologyTests
{
    private const float Close = 1e-4f;

    private static EditMesh Box() => new EditMesh().Box(new Vec3(2f, 3f, 4f));

    private static int FaceFacing(EditMesh mesh, Vec3 way) => Enumerable.Range(0, mesh.FaceCount).Single(face => (mesh.Normal(face) - way).Length < Close);

    [Fact]
    public void TheTopOfABoxExtrudedAMeterIsATallerBoxOfTwelveVerticesAndTenFaces()
    {
        var mesh = Box().SelectFacing(Vec3.UnitY).Extrude(1f);

        Assert.Equal((12, 20, 10), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(32f, mesh.Volume, Close);
        Assert.Equal(new Vec3(2f, 4f, 4f), mesh.Bounds.High);

        // The top stays selected, so a second extrude pushes it further.
        Assert.Equal((4, 1), (mesh.SelectedVertexCount, mesh.SelectedFaceCount));
        Assert.Equal(40f, mesh.Extrude(1f).Volume, Close);

        // By a vector, the same.
        Assert.Equal(32f, Box().SelectFacing(Vec3.UnitY).Extrude(new Vec3(0f, 1f, 0f)).Volume, Close);
    }

    [Fact]
    public void TwoFacesExtrudedTogetherMoveEachTheWholeDistanceAlongItsOwnNormal()
    {
        // The top and the front of a box of two pushed out a meter as one region stand a meter out
        // each, the corner they share moved along both, and the edge between them stays joined.
        var mesh = new EditMesh().Box(new Vec3(2f)).SelectFacing(Vec3.UnitY).SelectFacing(Vec3.UnitZ, SelectionMode.Add).Extrude(1f);

        Assert.Equal(new Vec3(2f, 3f, 3f), mesh.Bounds.High);
        Assert.True(mesh.IsClosed);

        // Six corners for the region's six, and a side for each of its six rim edges.
        Assert.Equal((14, 12), (mesh.VertexCount, mesh.FaceCount));
        Assert.Equal(8f + 4f + 4f + 2f, mesh.Volume, Close);
    }

    [Fact]
    public void APlanePushedUpIsABoxOpenUnderneath()
    {
        var mesh = new EditMesh().Plane(2f, 2f).Extrude(1f);

        Assert.Equal((8, 12, 5), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.Equal(4, mesh.OpenEdges);
        Assert.Equal(1f, mesh.Bounds.High.Y - mesh.Bounds.Low.Y, Close);
    }

    [Fact]
    public void AnInsetTopIsASmallerFaceInsideABandAndHoldsAsMuch()
    {
        var mesh = Box().SelectFacing(Vec3.UnitY).Inset(0.5f);

        Assert.Equal((12, 20, 10), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(24f, mesh.Volume, Close);

        // The smaller face is selected, a meter by three, half a meter in from each edge.
        Assert.Equal((4, 1), (mesh.SelectedVertexCount, mesh.SelectedFaceCount));
        var inner = Enumerable.Range(0, mesh.FaceCount).Single(mesh.IsFaceSelected);
        var corners = mesh.Corners(inner).ToArray().Select(mesh.Position).ToArray();
        Assert.Equal(1f, corners.Max(point => point.X) - corners.Min(point => point.X), Close);
        Assert.Equal(3f, corners.Max(point => point.Z) - corners.Min(point => point.Z), Close);

        // Pushed in, it is a recess a meter deep.
        Assert.Equal(24f - 3f, mesh.Extrude(-1f).Volume, Close);
    }

    [Fact]
    public void ATopDeletedLeavesAHoleAndFilledClosesIt()
    {
        var open = Box().SelectFacing(Vec3.UnitY).Delete();
        Assert.Equal((8, 12, 5), (open.VertexCount, open.EdgeCount, open.FaceCount));
        Assert.Equal(4, open.OpenEdges);
        Assert.Equal(0, open.SelectedFaceCount);

        var filled = Box().SelectFacing(Vec3.UnitY).Delete(fill: true);
        Assert.Equal((8, 12, 6), (filled.VertexCount, filled.EdgeCount, filled.FaceCount));
        Assert.True(filled.IsClosed);
        Assert.Equal(24f, filled.Volume, Close);
        Assert.Equal(Vec3.UnitY, filled.Normal(Enumerable.Range(0, 6).Single(filled.IsFaceSelected)));
    }

    [Fact]
    public void AVertexDeletedTakesTheFacesUsingItWithIt()
    {
        var mesh = Box().SelectInside(new Vec3(-0.1f), new Vec3(0.1f)).Delete();

        Assert.Equal((7, 3), (mesh.VertexCount, mesh.FaceCount));
        Assert.Equal(6, mesh.OpenEdges);
    }

    [Fact]
    public void ATopMovedOntoTheBottomMergesIntoTwoFacesOfFourCorners()
    {
        var mesh = new EditMesh().Box(new Vec3(1f)).SelectFacing(Vec3.UnitY).Move(new Vec3(0f, -1f, 0f)).SelectAll().Merge(0.001f);

        // The four sides have two corners each left and are gone, and the top and the bottom face
        // each other on the same four vertices.
        Assert.Equal((4, 4, 2), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.True(mesh.IsClosed);
        Assert.Equal(0f, mesh.Volume, Close);
        Assert.Equal(4, mesh.SelectedVertexCount);

        // Nothing near enough is nothing joined.
        Assert.Equal(16, new EditMesh().Box(new Vec3(1f)).Box(new Vec3(1f), new Vec3(3f, 0f, 0f)).SelectAll().Merge(0.5f).VertexCount);
    }

    [Fact]
    public void FlippedFacesFaceInAndHoldANegativeVolume()
    {
        var mesh = Box().SelectAll().Flip();

        Assert.True(mesh.IsClosed);
        Assert.Equal(-24f, mesh.Volume, Close);
        Assert.Equal(-Vec3.UnitY, mesh.Normal(FaceFacing(mesh, -Vec3.UnitY)));
    }

    [Fact]
    public void TriangulatingCutsEachFaceOfNCornersIntoNLessTwoAndKeepsTheSolid()
    {
        var box = Box().SelectAll().Triangulate();
        Assert.Equal((8, 18, 12), (box.VertexCount, box.EdgeCount, box.FaceCount));
        Assert.True(box.IsClosed);
        Assert.Equal(24f, box.Volume, Close);
        Assert.Equal(12, box.SelectedFaceCount);

        // A flight's sides are concave, ten corners each for four steps, and are cut inside their
        // outlines, so the solid holds what it did, its ten faces of four corners two triangles
        // each and its sides eight.
        var stairs = new EditMesh().Stairs(4, 0.25f, 0.5f, 2f).SelectAll().Triangulate();
        Assert.Equal(2 * 10 + 2 * 8, stairs.FaceCount);
        Assert.True(stairs.IsClosed);
        Assert.Equal(2.5f, stairs.Volume, Close);
        Assert.All(Enumerable.Range(0, stairs.FaceCount), face => Assert.Equal(3, stairs.Corners(face).Length));
    }

    [Fact]
    public void ADuplicateSharesNoVertexAndMovesAlone()
    {
        var mesh = Box().SelectAll().Duplicate();

        Assert.Equal((16, 12), (mesh.VertexCount, mesh.FaceCount));
        Assert.Equal((8, 6), (mesh.SelectedVertexCount, mesh.SelectedFaceCount));

        mesh.Move(new Vec3(5f, 0f, 0f));
        Assert.Equal(48f, mesh.Volume, Close);
        Assert.True(mesh.IsClosed);
        Assert.Equal((Vec3.Zero, new Vec3(7f, 3f, 4f)), mesh.Bounds);
    }

    [Fact]
    public void ShadingSetsTheSelectedFacesAngleAndFlatSetsNone()
    {
        var mesh = new EditMesh().Cylinder(1f, 2f, 8).SelectAll().Shade(true, 45f);
        Assert.All(Enumerable.Range(0, mesh.FaceCount), face => Assert.Equal(45f, mesh.SmoothOf(face)));

        mesh.SelectFacing(Vec3.UnitY).Shade(false);
        Assert.Equal(0f, mesh.SmoothOf(FaceFacing(mesh, Vec3.UnitY)));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Shade(true, 0f));
    }
}

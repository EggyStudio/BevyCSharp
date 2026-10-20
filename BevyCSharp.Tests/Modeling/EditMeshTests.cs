using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the edit mesh's primitives, its selection and what moves it, on exact counts of vertices,
/// edges and faces, the volume a closed mesh holds, and whether every edge is shared by two faces
/// running along it in opposite directions.
/// </summary>
/// <remarks>
/// A closed mesh whose faces all face out holds a positive volume, so the volume tells a primitive
/// turned inside out from one that is right, and the edges tell one with a hole or a face turned
/// the wrong way. The counts of a solid pushed out from an outline of n corners are 2n vertices,
/// 3n edges and n + 2 faces, which the primitives built that way are held to.
/// </remarks>
public sealed class EditMeshTests
{
    private const float Close = 1e-4f;

    public static TheoryData<string, int, int, int, float> Primitives => new()
    {
        // name, vertices, edges, faces, volume
        { "box", 8, 12, 6, 24f },
        { "cylinder", 16, 24, 10, 8f / 2f * MathF.Sin(MathF.Tau / 8f) * 2f },
        { "square cylinder", 8, 12, 6, 4f },
        { "cone", 7, 12, 7, 6f / 2f * MathF.Sin(MathF.Tau / 6f) * 3f / 3f },
        { "wedge", 6, 9, 5, 12f },
        { "stairs", 20, 30, 12, 2f * 0.25f * 0.5f * (1 + 2 + 3 + 4) },
        { "arch", 36, 54, 20, 8f / 2f * MathF.Sin(MathF.PI / 8f) * (4f - 2.25f) * 1f },
    };

    private static EditMesh Make(string name) => name switch
    {
        "box" => new EditMesh().Box(new Vec3(2f, 3f, 4f)),
        "cylinder" => new EditMesh().Cylinder(1f, 2f, 8),
        "square cylinder" => new EditMesh().Cylinder(1f, 2f, 4),
        "cone" => new EditMesh().Cone(1f, 3f, 6),
        "wedge" => new EditMesh().Wedge(new Vec3(2f, 3f, 4f)),
        "stairs" => new EditMesh().Stairs(4, 0.25f, 0.5f, 2f),
        "arch" => new EditMesh().Arch(2f, 0.5f, 1f, 8),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Theory]
    [MemberData(nameof(Primitives))]
    public void EachSolidPrimitiveHasItsCountsIsClosedAndFacesOut(string name, int vertices, int edges, int faces, float volume)
    {
        var mesh = Make(name);

        Assert.Equal((vertices, edges, faces), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.Equal(0, mesh.OpenEdges);
        Assert.True(mesh.IsClosed);
        Assert.Equal(volume, mesh.Volume, Close);

        // Selected whole, so the operation after it acts on it.
        Assert.Equal((vertices, edges, faces), (mesh.SelectedVertexCount, mesh.SelectedEdgeCount, mesh.SelectedFaceCount));

        // Placed by its lowest corner, at the origin where nothing says otherwise.
        Assert.Equal(Vec3.Zero, mesh.Bounds.Low);
    }

    [Fact]
    public void APlaneIsOneFaceFacingUpWithFourOpenEdges()
    {
        var mesh = new EditMesh().Plane(2f, 3f);

        Assert.Equal((4, 4, 1), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.Equal(4, mesh.OpenEdges);
        Assert.False(mesh.IsClosed);
        Assert.Equal(Vec3.UnitY, mesh.Normal(0));
        Assert.Equal((Vec3.Zero, new Vec3(2f, 0f, 3f)), mesh.Bounds);
    }

    [Fact]
    public void APrimitiveIsPlacedByItsLowestCornerOrByItsMiddle()
    {
        var cornered = new EditMesh().Box(new Vec3(2f, 3f, 4f), new Vec3(1f, 0f, -2f));
        Assert.Equal((new Vec3(1f, 0f, -2f), new Vec3(3f, 3f, 2f)), cornered.Bounds);

        var centered = new EditMesh().Box(new Vec3(2f, 3f, 4f), new Vec3(0f, 1.5f, 0f), centered: true);
        Assert.Equal((new Vec3(-1f, 0f, -2f), new Vec3(1f, 3f, 2f)), centered.Bounds);

        // A cylinder of eight sides stands its corners half a side round from the axes, so it is
        // as wide along X as along Z, and placed by its corner it starts at the origin all the same.
        var round = new EditMesh().Cylinder(1f, 2f, 8);
        var (low, high) = round.Bounds;
        Assert.Equal(Vec3.Zero, low);
        Assert.Equal(high.X, high.Z, Close);
        Assert.Equal(2f, high.Y);
    }

    [Fact]
    public void StairsRiseAlongZFromTheirFirstStepToABackAsTallAsTheFlight()
    {
        var mesh = new EditMesh().Stairs(4, 0.25f, 0.5f, 2f);

        Assert.Equal((Vec3.Zero, new Vec3(2f, 1f, 2f)), mesh.Bounds);

        // Four treads face up, one a step, each a step higher and a run further along.
        mesh.SelectFacing(Vec3.UnitY);
        Assert.Equal(4, mesh.SelectedFaceCount);

        var treads = Enumerable.Range(0, mesh.FaceCount).Where(mesh.IsFaceSelected).Select(mesh.Center).OrderBy(center => center.Z).ToArray();
        for (var step = 0; step < 4; step++)
        {
            Assert.Equal((step + 1) * 0.25f, treads[step].Y, Close);
            Assert.Equal((step + 0.5f) * 0.5f, treads[step].Z, Close);
        }

        // Four risers face toward the start, and the back faces the other way, as tall as the flight.
        Assert.Equal(4, mesh.SelectFacing(-Vec3.UnitZ).SelectedFaceCount);
        Assert.Equal(1, mesh.SelectFacing(Vec3.UnitZ).SelectedFaceCount);
    }

    [Fact]
    public void AWedgeRisesAlongZAndItsSlopeAtFortyFiveDegreesIsAFloor()
    {
        var mesh = new EditMesh().Wedge(new Vec3(2f, 3f, 4f));

        // Its top edge is at its far end, full height.
        foreach (var vertex in Enumerable.Range(0, mesh.VertexCount).Where(vertex => mesh.Position(vertex).Y > 0f))
        {
            Assert.Equal(4f, mesh.Position(vertex).Z);
        }

        // A slope that rises as far as it runs is taken as facing up, where a character walks it.
        var even = new EditMesh().Wedge(new Vec3(2f, 2f, 2f));
        Assert.Equal(1, even.SelectFacing(Vec3.UnitY).SelectedFaceCount);
        Assert.Equal(0, even.SelectFacing(-Vec3.UnitZ).SelectedFaceCount);
    }

    [Fact]
    public void TwoPrimitivesInOneMeshAreCountedTogetherAndTheSecondIsWhatIsSelected()
    {
        var mesh = new EditMesh().Box(new Vec3(1f)).Box(new Vec3(1f), new Vec3(3f, 0f, 0f));

        Assert.Equal((16, 24, 12), (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount));
        Assert.Equal(2f, mesh.Volume, Close);
        Assert.True(mesh.IsClosed);
        Assert.Equal((8, 6), (mesh.SelectedVertexCount, mesh.SelectedFaceCount));
        Assert.All(Enumerable.Range(0, 8), vertex => Assert.False(mesh.IsVertexSelected(vertex)));
    }

    [Fact]
    public void TheTopOfABoxSelectedAndMovedUpMakesItTaller()
    {
        var mesh = new EditMesh().Box(new Vec3(2f, 3f, 4f)).SelectFacing(Vec3.UnitY);

        Assert.Equal((4, 4, 1), (mesh.SelectedVertexCount, mesh.SelectedEdgeCount, mesh.SelectedFaceCount));

        mesh.Move(new Vec3(0f, 1f, 0f));

        Assert.Equal(32f, mesh.Volume, Close);
        Assert.Equal(new Vec3(2f, 4f, 4f), mesh.Bounds.High);
        Assert.True(mesh.IsClosed);
    }

    [Fact]
    public void InvertingTheTopSelectsTheOtherFiveFacesAndEveryCorner()
    {
        var mesh = new EditMesh().Box(new Vec3(1f)).SelectFacing(Vec3.UnitY).SelectInvert();

        Assert.Equal((8, 12, 5), (mesh.SelectedVertexCount, mesh.SelectedEdgeCount, mesh.SelectedFaceCount));
        Assert.False(mesh.IsFaceSelected(Enumerable.Range(0, 6).Single(face => mesh.Normal(face) == Vec3.UnitY)));

        Assert.Equal((0, 0), (mesh.SelectAll().SelectInvert().SelectedVertexCount, mesh.SelectedFaceCount));
    }

    [Fact]
    public void FacesAreAddedAndTakenAwayWithTheCornersNoOtherSelectedFaceHas()
    {
        var mesh = new EditMesh().Box(new Vec3(1f));
        var top = Enumerable.Range(0, 6).Single(face => mesh.Normal(face) == Vec3.UnitY);
        var front = Enumerable.Range(0, 6).Single(face => mesh.Normal(face) == Vec3.UnitZ);

        mesh.SelectFaces([top]).SelectFaces([front], SelectionMode.Add);
        Assert.Equal((6, 2), (mesh.SelectedVertexCount, mesh.SelectedFaceCount));

        // The two corners the top and the front share stay with the front.
        mesh.SelectFaces([top], SelectionMode.Remove);
        Assert.Equal((4, 1), (mesh.SelectedVertexCount, mesh.SelectedFaceCount));
        Assert.True(mesh.IsFaceSelected(front));

        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.SelectFaces([6]));
    }

    [Fact]
    public void ABoxDrawnRoundHalfAMeshPicksItsVerticesAndTheFacesWhollyInside()
    {
        var mesh = new EditMesh().Box(new Vec3(2f, 1f, 1f)).SelectInside(new Vec3(1f, -1f, -1f), new Vec3(3f, 2f, 2f));

        // The far half's four corners, on the box's side, and the one face all of whose corners are there.
        Assert.Equal((4, 4, 1), (mesh.SelectedVertexCount, mesh.SelectedEdgeCount, mesh.SelectedFaceCount));
        Assert.Equal(Vec3.UnitX, mesh.Normal(Enumerable.Range(0, 6).Single(mesh.IsFaceSelected)));

        // Taking away the corners at the top takes the face with them.
        mesh.SelectInside(new Vec3(-1f, 0.5f, -1f), new Vec3(3f, 2f, 2f), SelectionMode.Remove);
        Assert.Equal((2, 0), (mesh.SelectedVertexCount, mesh.SelectedFaceCount));
    }

    [Fact]
    public void FacesPutInASlotAreSelectedByIt()
    {
        var mesh = new EditMesh().Box(new Vec3(4f, 3f, 4f)).SelectFacing(Vec3.UnitY).Material("ceiling")
            .SelectFacing(-Vec3.UnitY).Material("floor");

        Assert.Equal(new[] { EditMesh.DefaultSlot, "ceiling", "floor" }, mesh.Slots);
        Assert.Equal(1, mesh.SelectSlot("floor").SelectedFaceCount);
        Assert.Equal(-Vec3.UnitY, mesh.Normal(Enumerable.Range(0, 6).Single(mesh.IsFaceSelected)));
        Assert.Equal(4, mesh.SelectSlot(EditMesh.DefaultSlot).SelectedFaceCount);
        Assert.Equal(5, mesh.SelectSlot("ceiling", SelectionMode.Add).SelectedFaceCount);
        Assert.Equal(0, mesh.SelectSlot("roof").SelectedFaceCount);

        Assert.Throws<ArgumentException>(() => mesh.Material("two words"));
    }

    [Fact]
    public void AQuarterTurnAboutYTurnsABoxInPlace()
    {
        var mesh = new EditMesh().Box(new Vec3(2f, 3f, 4f)).Rotate(Vec3.UnitY, 90f);
        var (low, high) = mesh.Bounds;

        // The middle of the box holding it stays, at (1, 1.5, 2), and its width and depth trade.
        Assert.Equal(4f, high.X - low.X, Close);
        Assert.Equal(2f, high.Z - low.Z, Close);
        Assert.Equal(1f, (low.X + high.X) / 2f, Close);
        Assert.Equal(2f, (low.Z + high.Z) / 2f, Close);
        Assert.Equal(24f, mesh.Volume, Close);
        Assert.True(mesh.IsClosed);

        // About a point given, the corner at the origin, a quarter turn about Y takes +X to -Z.
        var pivoted = new EditMesh().Box(new Vec3(2f, 1f, 1f)).Rotate(Vec3.UnitY, 90f, Vec3.Zero);
        Assert.Equal(-2f, pivoted.Bounds.Low.Z, Close);
        Assert.Equal(1f, pivoted.Bounds.High.X, Close);
    }

    [Fact]
    public void ScalingStretchesAboutTheMiddleAndAMirrorStillFacesOut()
    {
        var mesh = new EditMesh().Box(new Vec3(2f)).Scale(new Vec3(2f, 1f, 1f));

        Assert.Equal((new Vec3(-1f, 0f, 0f), new Vec3(3f, 2f, 2f)), mesh.Bounds);
        Assert.Equal(16f, mesh.Volume, Close);

        mesh.Scale(new Vec3(-1f, 1f, 1f));
        Assert.Equal(16f, mesh.Volume, Close);
        Assert.True(mesh.IsClosed);

        // About a point given, which stays where it is.
        var pivoted = new EditMesh().Box(new Vec3(1f)).Scale(3f, Vec3.Zero);
        Assert.Equal((Vec3.Zero, new Vec3(3f)), pivoted.Bounds);
    }

    [Fact]
    public void TheGridIsAPowerOfTwoOfAMeterAndSnapPutsTheSelectionOnIt()
    {
        var mesh = new EditMesh().Box(new Vec3(1f), new Vec3(0.3f, 0.6f, -0.2f));

        Assert.Equal(1f, mesh.GridStep);
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Grid(0.3f));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Grid(1f / 32f));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Grid(128f));

        mesh.Grid(0.5f).Snap();

        Assert.Equal(0.5f, mesh.GridStep);
        Assert.Equal((new Vec3(0.5f, 0.5f, 0f), new Vec3(1.5f, 1.5f, 1f)), mesh.Bounds);

        // A typed measure is taken as given, off the grid.
        mesh.Move(new Vec3(0.2f, 0f, 0f));
        Assert.Equal(0.7f, mesh.Bounds.Low.X, Close);
    }

    [Fact]
    public void WhatIsNotSelectedStaysWhereItIs()
    {
        var mesh = new EditMesh().Box(new Vec3(1f)).Box(new Vec3(1f), new Vec3(3f, 0f, 0f)).Move(new Vec3(0f, 5f, 0f));

        Assert.Equal((Vec3.Zero, new Vec3(4f, 6f, 1f)), mesh.Bounds);
        Assert.Equal(1f, Enumerable.Range(0, 8).Max(vertex => mesh.Position(vertex).Y), Close);

        Assert.Equal(0, mesh.SelectNone().Move(Vec3.One).SelectedVertexCount);
        Assert.Equal((Vec3.Zero, new Vec3(4f, 6f, 1f)), mesh.Bounds);
    }

    [Fact]
    public void AMeasureOfAPrimitiveIsAboveZero()
    {
        var mesh = new EditMesh();

        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Box(new Vec3(1f, 0f, 1f)));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Cylinder(1f, 1f, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Arch(1f, 1f, 1f, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Stairs(0, 1f, 1f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Plane(float.NaN, 1f));
        Assert.Equal(0, mesh.VertexCount);
    }
}

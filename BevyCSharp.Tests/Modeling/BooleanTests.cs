using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the edit mesh's booleans, a box cut through a wall, two boxes joined, two kept where they
/// overlap, a flight of stairs joined by its primitive's line, and the room, the window, the door
/// and the ramp made with them, each closed with every edge shared by two faces, holding the volume
/// the operation leaves, and joined back into few faces.
/// </summary>
/// <remarks>
/// The partition leaves convex pieces, which are joined back where they lie in one plane and one
/// slot, so a cut through a wall leaves the wall's two faces in a few pieces round the hole, the
/// hole's four sides, and the wall's other faces as they were.
/// </remarks>
public sealed class BooleanTests : IDisposable
{
    private const float Close = 1e-3f;

    private readonly TestFolder _folder = new("bcs-boolean-");

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void ABoxCutThroughAWallLeavesItClosedAndHoldingTheWallLessTheHole()
    {
        var wall = new EditMesh().Box(new Vec3(4f, 3f, 0.5f)).Cut("box 1 1 1 at 1.5 1 -0.25");

        Assert.True(wall.IsClosed);
        Assert.Equal(6f - 0.5f, wall.Volume, Close);

        // The hole's four sides are selected. The wall's front and back are two faces each
        // round the hole, which one loop cannot go round, and its top, bottom and ends one each,
        // an end with a corner where the front's two faces meet it, twelve faces in all, with a
        // hole through, so vertices less edges plus faces is none.
        Assert.Equal(4, wall.SelectedFaceCount);
        Assert.Equal((20, 32, 12), (wall.VertexCount, wall.EdgeCount, wall.FaceCount));
        Assert.Equal(new[] { "box 4 3 0.5", "cut box 1 1 1 at 1.5 1 -0.25" }, wall.Recipe);
    }

    [Fact]
    public void TwoBoxesSharingAFaceUniteIntoOneBox()
    {
        var other = new EditMesh().Box(new Vec3(1f)).Save(_folder.File("cube.model"));
        var joined = new EditMesh().Box(new Vec3(1f)).Union(other, new Vec3(1f, 0f, 0f));

        Assert.Equal((8, 12, 6), (joined.VertexCount, joined.EdgeCount, joined.FaceCount));
        Assert.True(joined.IsClosed);
        Assert.Equal(2f, joined.Volume, Close);
        Assert.Equal((Vec3.Zero, new Vec3(2f, 1f, 1f)), joined.Bounds);
    }

    [Fact]
    public void TwoOverlappingBoxesIntersectInTheirOverlapAndSubtractToTheRest()
    {
        var other = new EditMesh().Box(new Vec3(2f)).Save(_folder.File("block.model"));

        var both = new EditMesh().Box(new Vec3(2f)).Intersect(other, new Vec3(1f));
        Assert.Equal((8, 6), (both.VertexCount, both.FaceCount));
        Assert.True(both.IsClosed);
        Assert.Equal(1f, both.Volume, Close);

        var rest = new EditMesh().Box(new Vec3(2f)).Subtract(other, new Vec3(1f));
        Assert.True(rest.IsClosed);
        Assert.Equal(7f, rest.Volume, Close);
        Assert.Equal(new[] { "box 2 2 2", $"subtract {other} at 1 1 1" }, rest.Recipe);
    }

    [Fact]
    public void ARoomIsTwoClosedShellsHoldingItsWalls()
    {
        var room = new EditMesh().Room(new Vec3(6f, 3f, 5f), 0.2f);

        Assert.Equal((16, 24, 12), (room.VertexCount, room.EdgeCount, room.FaceCount));
        Assert.True(room.IsClosed);
        Assert.Equal(6f * 3f * 5f - 5.6f * 2.6f * 4.6f, room.Volume, Close);
    }

    [Fact]
    public void AWindowGoesThroughTheWallSelectedAndNotTheWallAcross()
    {
        var room = new EditMesh().Room(new Vec3(6f, 3f, 5f), 0.2f);
        var before = room.Volume;

        // The front wall's outside, the first face facing out along +Z, the inside of the back
        // wall facing that way too, after it.
        room.SelectFacing(Vec3.UnitZ).Window(1f, 1f, 3f, 1f);

        Assert.True(room.IsClosed);
        Assert.Equal(before - 1f * 1f * 0.2f, room.Volume, Close);
        Assert.Equal(4, room.SelectedFaceCount);
        Assert.All(Enumerable.Range(0, room.FaceCount).Where(room.IsFaceSelected), face =>
        {
            var center = room.Center(face);
            Assert.InRange(center.Z, 4.8f - Close, 5f + Close);
            Assert.InRange(center.X, 2.5f - Close, 3.5f + Close);
        });

        Assert.Throws<InvalidOperationException>(() => room.SelectFacing(Vec3.UnitY).Window(1f, 1f, 1f, 1f));
        Assert.Throws<InvalidOperationException>(() => room.SelectNone().Window(1f, 1f, 1f, 1f));
    }

    [Fact]
    public void ADoorStandsOnTheWallsBottomAndASecondOpeningIsMeasuredOnTheWholeWall()
    {
        var wall = new EditMesh().Box(new Vec3(6f, 3f, 0.2f)).SelectFacing(Vec3.UnitZ).Window(1f, 1f, 4f, 1f);
        var front = new Vec3(-1f, -1f, 0.2f - Close);

        // Every piece the window left of the front, so the door is measured from the wall's left.
        wall.SelectInside(front, new Vec3(7f, 4f, 0.2f + Close)).Door(1f, 2.1f, 1.5f);

        Assert.True(wall.IsClosed);
        Assert.Equal(6f * 3f * 0.2f - 1f * 0.2f - 1f * 2.1f * 0.2f, wall.Volume, Close);

        // The door's sides from its foot at the wall's bottom up to its head, between one and two along X.
        var sides = Enumerable.Range(0, wall.FaceCount).Where(wall.IsFaceSelected).Select(wall.Center).ToArray();
        Assert.Equal(3, sides.Length);
        Assert.All(sides, center => Assert.InRange(center.X, 1f - Close, 2f + Close));
        Assert.Equal(new[] { "box 6 3 0.2", "select +z", "cut window 1 1 4 1", "select box -1 -1 0.199 7 4 0.201", "cut door 1 2.1 1.5" }, wall.Recipe);
    }

    [Fact]
    public void AFlightJoinedByItsLineIsOneSolidWithTheFloorAndTheWallItStandsOn()
    {
        // A hall's floor and wall the flight stands on and against, as Blockout's stairs do.
        static EditMesh Hall() => new EditMesh().Box(new Vec3(6f, 3f, 6f)).Cut("box 5.6 2.6 5.6 at 0.2 0.2 0.2");
        var shell = 6f * 3f * 6f - 5.6f * 2.6f * 5.6f;

        var hall = Hall().Union("stairs 8 0.2 0.3 1 at 0.2 0.2 1");
        Assert.True(hall.IsClosed);
        Assert.Equal("union stairs 8 0.2 0.3 1 at 0.2 0.2 1", hall.Recipe[^1]);
        Assert.Equal(shell + 0.3f * 1f * 0.2f * 36f, hall.Volume, Close);

        // A window cut after it leaves every edge shared by two faces, where a flight added as a
        // second solid touching the floor and the wall has its edges welded to theirs by the cut,
        // four faces along each, and every one of them counted open.
        static EditMesh Window(EditMesh mesh) => mesh.SelectInside(new Vec3(5.8f, 0.2f, 0.2f), new Vec3(5.8f, 2.8f, 5.8f)).Window(1f, 1f, 2.8f, 1f);
        Assert.Equal(0, Window(hall).OpenEdges);
        Assert.True(Window(Hall().Stairs(8, 0.2f, 0.3f, 1f, new Vec3(0.2f, 0.2f, 1f))).OpenEdges > 0);
    }

    [Fact]
    public void APrimitivesLineInABooleanTakesTheMeshsFirstSlotAndReadsBack()
    {
        var mesh = new EditMesh().Box(new Vec3(2f)).SlotRename("default", "stone");
        mesh.Union("box 1 1 1 at 2 0 0");
        mesh.Subtract("box 0.5 0.5 0.5");
        mesh.Intersect("box 3 3 3 at -0.5 -0.5 -0.5");

        // The primitives' faces in the stone the mesh's first slot was renamed to, and no slot
        // called default made for them.
        Assert.Equal(new[] { "stone" }, mesh.Slots);
        Assert.True(mesh.IsClosed);
        Assert.Equal(
            new[] { "box 2 2 2", "slot rename default stone", "union box 1 1 1 at 2 0 0", "subtract box 0.5 0.5 0.5", "intersect box 3 3 3 at -0.5 -0.5 -0.5" },
            mesh.Recipe);
        Assert.Equal(mesh.Recipe, EditMesh.FromRecipe(mesh.RecipeText).Recipe);

        // A primitive's line places it with its own at, which a boolean's move of a file's model is not.
        Assert.Throws<ArgumentException>(() => mesh.Union("box 1 1 1", Vec3.One));
    }

    [Fact]
    public void ARampIsAWedgeInTheStairsWordsAndTheLinesReadBack()
    {
        var ramp = new EditMesh().Ramp(1f, 3f, 2f);
        Assert.Equal((6, 5), (ramp.VertexCount, ramp.FaceCount));
        Assert.Equal(3f, ramp.Volume, Close);
        Assert.Equal((Vec3.Zero, new Vec3(2f, 1f, 3f)), ramp.Bounds);

        var made = new EditMesh().Epsilon(1e-4f).Room(new Vec3(6f, 3f, 5f), 0.2f).SelectFacing(Vec3.UnitZ).Window(1f, 1f, 3f, 1f)
            .Cut("cylinder 0.2 4 8 at 1 -0.5 1").Ramp(0.5f, 2f, 1f, new Vec3(1f, 0.2f, 1f));
        var again = EditMesh.FromRecipe(made.RecipeText);

        Assert.Equal(made.Recipe, again.Recipe);
        Assert.Equal((made.VertexCount, made.FaceCount), (again.VertexCount, again.FaceCount));
        Assert.Equal(made.Volume, again.Volume, Close);
        Assert.Equal(1e-4f, again.BooleanEpsilon);

        Assert.Throws<ArgumentException>(() => new EditMesh().Box(new Vec3(1f)).Cut("plane 1 1"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EditMesh().Room(new Vec3(1f), 0.5f));
    }
}

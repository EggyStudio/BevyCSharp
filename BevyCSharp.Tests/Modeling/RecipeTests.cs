using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers an edit mesh's recipe, the lines its operations write, the mesh made again from them,
/// undo and redo by making it again, the file it is saved as and loaded from, a model joined in by
/// its file, and the errors a line that is not an operation is named by.
/// </summary>
/// <remarks>
/// The lines are the words 3DEngine's model takes too, so a recipe written in one engine builds the
/// same mesh in the other, and each line written is the line the console's model command takes.
/// </remarks>
public sealed class RecipeTests : IDisposable
{
    private const float Close = 1e-4f;

    private readonly TestFolder _folder = new("bcs-recipe-");

    public void Dispose() => _folder.Dispose();

    private static EditMesh Room() => new EditMesh()
        .Box(new Vec3(8f, 3f, 6f))
        .SelectFacing(Vec3.UnitY)
        .Material("ceiling")
        .Inset(1f)
        .Extrude(0.5f)
        .SelectAll()
        .Rotate(Vec3.UnitY, 90f, Vec3.Zero)
        .Grid(0.5f)
        .Snap();

    [Fact]
    public void EachOperationWritesItsLineInTheWordsTheModelCommandsTake()
    {
        Assert.Equal(
            new[]
            {
                "box 8 3 6", "select +y", "material ceiling", "inset 1", "extrude 0.5", "select all",
                "rotate y 90 at 0 0 0", "grid 0.5", "snap",
            },
            Room().Recipe);

        var mesh = new EditMesh()
            .Stairs(4, 0.25f, 0.3f, 2f, new Vec3(1f, 0f, -2f))
            .Cylinder(0.4f, 3f, 8, new Vec3(0f, 1.5f, 0f), centered: true)
            .SelectFaces([0, 2], SelectionMode.Add)
            .SelectFacing(-Vec3.UnitX, SelectionMode.Remove)
            .SelectInside(Vec3.Zero, Vec3.One)
            .SelectSlot("default", SelectionMode.Add)
            .Scale(new Vec3(1f, 2f, 1f))
            .Rotate(new Vec3(1f, 0f, 1f), 45f)
            .Shade(true)
            .Delete(fill: true)
            .SelectAll()
            .Merge(0.01f)
            .Flip()
            .Triangulate()
            .Duplicate()
            .Extrude(new Vec3(0f, 1f, 0f))
            .Clear();

        Assert.Equal(
            new[]
            {
                "stairs 4 0.25 0.3 2 at 1 0 -2", "cylinder 0.4 3 8 at 0 1.5 0 center", "select add face 0 2", "select remove -x",
                "select box 0 0 0 1 1 1", "select add slot default", "scale 1 2 1", "rotate 1 0 1 45", "shade smooth 30", "delete fill",
                "select all", "merge 0.01", "flip", "triangulate", "duplicate", "extrude 0 1 0", "clear",
            },
            mesh.Recipe);
    }

    [Fact]
    public void ARecipeRunFromNothingMakesTheSameMesh()
    {
        var made = Room();
        var again = EditMesh.FromRecipe(made.RecipeText);

        Assert.Equal(made.Recipe, again.Recipe);
        Assert.Equal((made.VertexCount, made.EdgeCount, made.FaceCount), (again.VertexCount, again.EdgeCount, again.FaceCount));
        Assert.Equal(made.Slots, again.Slots);
        Assert.Equal(made.Volume, again.Volume, Close);
        Assert.All(Enumerable.Range(0, made.VertexCount), vertex => Assert.Equal(made.Position(vertex), again.Position(vertex)));
        Assert.True(again.IsClosed);
    }

    [Fact]
    public void UndoMakesTheMeshAgainWithoutTheLastLineAndRedoPutsItBack()
    {
        var mesh = new EditMesh().Box(new Vec3(2f, 3f, 4f)).SelectFacing(Vec3.UnitY).Move(new Vec3(0f, 1f, 0f));
        Assert.Equal(32f, mesh.Volume, Close);

        Assert.True(mesh.Undo());
        Assert.Equal(24f, mesh.Volume, Close);
        Assert.Equal(new[] { "box 2 3 4", "select +y" }, mesh.Recipe);

        // The selection is the one the lines before left, the top.
        Assert.Equal(1, mesh.SelectedFaceCount);

        Assert.True(mesh.CanRedo);
        Assert.True(mesh.Redo());
        Assert.Equal(32f, mesh.Volume, Close);
        Assert.False(mesh.CanRedo);

        // An operation after an undo forgets what was taken back.
        mesh.Undo();
        mesh.Move(new Vec3(0f, 2f, 0f));
        Assert.False(mesh.Redo());
        Assert.Equal(40f, mesh.Volume, Close);

        while (mesh.Undo()) { }
        Assert.Equal((0, 0), (mesh.VertexCount, mesh.Recipe.Count));
        Assert.False(mesh.CanUndo);
    }

    [Fact]
    public void ASavedRecipeLoadsBackAsTheSameMesh()
    {
        var made = Room();
        var path = made.Save(_folder.File("models/room.model"));

        Assert.StartsWith(EditMesh.Header + "\n", File.ReadAllText(path));
        Assert.False(File.Exists(path + ".part"));

        var loaded = EditMesh.Load(path);
        Assert.Equal(made.Recipe, loaded.Recipe);
        Assert.Equal((made.VertexCount, made.FaceCount), (loaded.VertexCount, loaded.FaceCount));
        Assert.True(loaded.CanUndo);

        Assert.Throws<FileNotFoundException>(() => EditMesh.Load(_folder.File("models/none.model")));
    }

    [Fact]
    public void AJoinedModelIsAddedByItsFileWithItsSlotsByName()
    {
        var pillar = new EditMesh().Cylinder(0.5f, 3f, 8).Material("stone");
        var pillarPath = pillar.Save(_folder.File("pillar.model"));

        var hall = new EditMesh().Box(new Vec3(10f, 3f, 10f)).Join(pillarPath).Move(new Vec3(5f, 0f, 5f));

        Assert.Equal((8 + 16, 6 + 10), (hall.VertexCount, hall.FaceCount));
        Assert.Equal(new[] { EditMesh.DefaultSlot, "stone" }, hall.Slots);
        Assert.Equal((16, 10), (hall.SelectedVertexCount, hall.SelectedFaceCount));
        Assert.Equal($"join {pillarPath}", hall.Recipe[1]);

        // A model that joins itself is refused rather than read until the stack runs out.
        var loop = _folder.File("loop.model");
        File.WriteAllText(loop, $"{EditMesh.Header}\nbox 1 1 1\njoin {loop}\n");
        Assert.Throws<FormatException>(() => EditMesh.Load(loop));
    }

    [Fact]
    public void AReadRecipeSkipsCommentsAndBlankLinesAndSaysWhichLineIsWrong()
    {
        var mesh = EditMesh.FromRecipe("model 1\n# a room\n\nbox 8 3 6   # the walls\nselect +y\nextrude 1\n");
        Assert.Equal(new[] { "box 8 3 6", "select +y", "extrude 1" }, mesh.Recipe);
        Assert.Equal(8f * 4f * 6f, mesh.Volume, Close);

        var unknown = Assert.Throws<FormatException>(() => EditMesh.FromRecipe("model 1\nbox 1 1 1\nboxx 1 1 1\n", "hall.model"));
        Assert.StartsWith("Line 3 of hall.model, boxx 1 1 1: boxx is not an operation", unknown.Message);

        Assert.Contains("needs another number", Assert.Throws<FormatException>(() => EditMesh.FromRecipe("box 1 1")).Message);
        Assert.Contains("is not a number", Assert.Throws<FormatException>(() => EditMesh.FromRecipe("move 1 up 0")).Message);
        Assert.Contains("words after", Assert.Throws<FormatException>(() => EditMesh.FromRecipe("flip twice")).Message);
        Assert.Contains("this reads model 1", Assert.Throws<FormatException>(() => EditMesh.FromRecipe("model 2\nbox 1 1 1")).Message);
        Assert.Contains("above zero", Assert.Throws<FormatException>(() => EditMesh.FromRecipe("box 1 0 1")).Message);
    }

    [Theory]
    [InlineData("rotate y 90 at 1 0 1")]
    [InlineData("rotate 0 -1 0 45")]
    [InlineData("scale 2")]
    [InlineData("scale 1 2 1 at 0 0 0")]
    [InlineData("select add face 0 1")]
    [InlineData("select remove +y")]
    [InlineData("select box 0 0 0 1 1 1")]
    [InlineData("select slot default")]
    [InlineData("extrude 1")]
    [InlineData("extrude 0 1 0")]
    [InlineData("inset 0.25")]
    [InlineData("delete fill")]
    [InlineData("shade smooth 45")]
    [InlineData("shade flat")]
    [InlineData("wedge 1 2 3 center")]
    [InlineData("arch 2 0.5 1 8 at 0 0 4")]
    public void ALineRunIsWrittenBackAsItWasTyped(string line)
    {
        var mesh = new EditMesh().Box(new Vec3(2f)).SelectAll().Run(line);

        Assert.Equal(line, mesh.Recipe[^1]);
    }
}

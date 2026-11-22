using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the modeling commands through the console, the guide's room with a window made a line at
/// a time and its counts read from the answers, a save loaded back as the same model, undo and redo,
/// the refusals, the texture coordinates and the slots' looks, and a model shown in a running app
/// following the commands' edits, its material among them.
/// </summary>
/// <remarks>
/// The models outlive a test where no app runs, so each test names its own and forgets them after.
/// </remarks>
[Collection("engine")]
public sealed class ModelCommandTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-model-commands-");

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void TheGuidesRoomIsMadeALineAtATimeAndSavedAndLoadedAsTheSame()
    {
        Assert.Equal("hall1: 16 vertices, 24 edges, 12 faces, 12 faces and 16 vertices selected", ConsoleHost.Run("model.room hall1 6 3 5 0.2").Text);
        Assert.StartsWith("hall1: 16 vertices, 24 edges, 12 faces, 2 faces", ConsoleHost.Run("model.select hall1 -z").Text);

        // The window's four sides selected, a corner more than their eight where the wall's pieces
        // meet one of their edges.
        var cut = ConsoleHost.Run("model.cut hall1 window 1 1.2 2 0.9").Text;
        Assert.NotNull(cut);
        Assert.EndsWith("4 faces and 9 vertices selected", cut);

        var mesh = Modeling.Find("hall1")!;
        Assert.True(mesh.IsClosed);
        Assert.Equal(new[] { "room 6 3 5 0.2", "select -z", "cut window 1 1.2 2 0.9" }, mesh.Recipe);

        var info = ConsoleHost.Run("model.info hall1").Text;
        Assert.Contains("closed", info);
        Assert.Contains("slots default", info);
        Assert.Contains("3 lines of recipe", info);

        var path = _folder.File("hall.model");
        Assert.Equal($"wrote hall1 to {path}", ConsoleHost.Run($"model.save hall1 {path}").Text);
        Assert.Equal(cut.Replace("hall1", "hall2"), ConsoleHost.Run($"model.load hall2 {path}").Text);
        Assert.Equal(mesh.Recipe, Modeling.Find("hall2")!.Recipe);

        Assert.Contains("hall1: ", ConsoleHost.Run("model.list").Text);
        Assert.Contains("hall2: ", ConsoleHost.Run("model.list").Text);
        Assert.StartsWith(EditMesh.Header + "\nroom 6 3 5 0.2", ConsoleHost.Run("model.recipe hall1").Text);

        Assert.StartsWith("hall1: 16 vertices", ConsoleHost.Run("model.undo hall1").Text);
        Assert.Equal(cut, ConsoleHost.Run("model.redo hall1").Text);

        Assert.Equal("forgot hall1", ConsoleHost.Run("model.free hall1").Text);
        Assert.Equal("forgot hall2", ConsoleHost.Run("model.free hall2").Text);
        Assert.DoesNotContain("hall1", Modeling.Names);
    }

    [Fact]
    public void AnOperationOnANameNoModelHasIsRefusedAndAPrimitiveMakesOne()
    {
        Assert.StartsWith("There is no model called absent3", ConsoleHost.Run("model.select absent3 +y").Text);
        Assert.Null(Modeling.Find("absent3"));

        Assert.StartsWith("plane3: 4 vertices", ConsoleHost.Run("model.plane plane3 2 2").Text);
        Assert.Contains("is not a number", ConsoleHost.Run("model.move plane3 1 up 0").Text);
        Assert.Contains("needs another number", ConsoleHost.Run("model.extrude plane3 0 1").Text);
        Assert.StartsWith("plane3: 8 vertices", ConsoleHost.Run("model.extrude plane3 1").Text);
        Assert.StartsWith("plane3 has no operation", ConsoleHost.Run("model.redo plane3").Text);

        Assert.Equal("forgot plane3", ConsoleHost.Run("model.free plane3").Text);
    }

    [Fact]
    public void AnEdgeIsSelectedByItsEndsAndBeveledLoopCutAndSubdividedByTheCommands()
    {
        Assert.StartsWith("cube6: 8 vertices", ConsoleHost.Run("model.box cube6 2 2 2").Text);
        Assert.Equal("cube6: 8 vertices, 12 edges, 6 faces, 0 faces and 2 vertices selected", ConsoleHost.Run("model.select cube6 edge 0 4").Text);
        Assert.Equal("cube6: 12 vertices, 20 edges, 10 faces, 0 faces and 4 vertices selected", ConsoleHost.Run("model.loopcut cube6 0.5").Text);
        Assert.StartsWith("cube6: 12 vertices", ConsoleHost.Run("model.select cube6 +y").Text);
        Assert.Equal("cube6: 24 vertices, 40 edges, 18 faces, 9 faces and 16 vertices selected", ConsoleHost.Run("model.subdivide cube6 2").Text);
        Assert.StartsWith("cube6: ", ConsoleHost.Run("model.select cube6 all").Text);
        Assert.StartsWith("cube6: ", ConsoleHost.Run("model.bevel cube6 0.1 2").Text);
        Assert.True(Modeling.Find("cube6")!.IsClosed);

        Assert.Contains("No face runs from vertex 0 to vertex 99", ConsoleHost.Run("model.select cube6 edge 0 99").Text);
        Assert.Contains("is not a whole number", ConsoleHost.Run("model.bevel cube6 0.1 two").Text);
        Assert.EndsWith("select edge 0 4\nloopcut 0.5\nselect +y\nsubdivide 2\nselect all\nbevel 0.1 2", ConsoleHost.Run("model.recipe cube6").Text);
        Assert.Equal("forgot cube6", ConsoleHost.Run("model.free cube6").Text);
    }

    [Fact]
    public void AHalfIsMirroredCutFilledAndSmoothedByTheCommands()
    {
        Assert.StartsWith("half7: 8 vertices", ConsoleHost.Run("model.box half7 1 2 2").Text);
        Assert.Equal("half7: 12 vertices, 20 edges, 10 faces, 10 faces and 12 vertices selected", ConsoleHost.Run("model.mirror half7 x").Text);

        // The front, the back and both ends crossed at a meter up, six faces cut and six points made.
        Assert.Equal("half7: 18 vertices, 32 edges, 16 faces, 16 faces and 18 vertices selected", ConsoleHost.Run("model.knife half7 y at 0 1 0").Text);
        Assert.StartsWith("half7: 18 vertices, 32 edges, 16 faces, 2 faces", ConsoleHost.Run("model.select half7 +y").Text);
        Assert.StartsWith("half7: 18 vertices, 31 edges, 15 faces, 1 faces", ConsoleHost.Run("model.dissolve half7").Text);
        Assert.StartsWith("half7: 18 vertices, 31 edges, 14 faces, 0 faces", ConsoleHost.Run("model.delete half7").Text);
        Assert.StartsWith("half7: ", ConsoleHost.Run("model.select half7 all").Text);
        Assert.StartsWith("half7: 18 vertices, 31 edges, 15 faces, 1 faces", ConsoleHost.Run("model.fill half7").Text);
        Assert.StartsWith("half7: 18 vertices, 31 edges, 15 faces", ConsoleHost.Run("model.smooth half7 0.1 2").Text);
        Assert.True(Modeling.Find("half7")!.IsClosed);

        Assert.StartsWith("half7: ", ConsoleHost.Run("model.select half7 none").Text);
        Assert.Contains("no edge is selected", ConsoleHost.Run("model.spin half7 y 90 4").Text);
        Assert.Contains("two regions", ConsoleHost.Run("model.bridge half7").Text);
        Assert.EndsWith("mirror x\nknife y at 0 1 0\nselect +y\ndissolve\ndelete\nselect all\nfill\nsmooth 0.1 2\nselect none", ConsoleHost.Run("model.recipe half7").Text);
        Assert.Equal("forgot half7", ConsoleHost.Run("model.free half7").Text);
    }

    [Fact]
    public void TheModifierStackIsChangedByTheCommandAndInfoSaysWhatItBuilds()
    {
        Assert.StartsWith("stack9: 8 vertices", ConsoleHost.Run("model.box stack9 2 2 2").Text);

        // A modifier leaves the model's own counts, which each command answers, and info says
        // the stack and what it builds.
        Assert.StartsWith("stack9: 8 vertices, 12 edges, 6 faces", ConsoleHost.Run("model.modifier stack9 add subdivision 1").Text);
        Assert.StartsWith("stack9: 8 vertices", ConsoleHost.Run("model.modifier stack9 add array 2 3 0 0").Text);
        Assert.Contains("modifiers subdivision 1, array 2 3 0 0, built as 52 vertices and 48 faces", ConsoleHost.Run("model.info stack9").Text);

        Assert.Contains("is no modifier", ConsoleHost.Run("model.modifier stack9 add twist 90").Text);
        Assert.Contains("There is no model at models/none.model", ConsoleHost.Run("model.modifier stack9 add boolean union models/none.model").Text);

        Assert.StartsWith("stack9: 8 vertices", ConsoleHost.Run("model.modifier stack9 remove 1").Text);
        Assert.StartsWith("stack9: 26 vertices, 48 edges, 24 faces", ConsoleHost.Run("model.modifier stack9 apply 0").Text);
        Assert.Contains("no modifiers", ConsoleHost.Run("model.info stack9").Text);
        Assert.EndsWith("modifier add subdivision 1\nmodifier add array 2 3 0 0\nmodifier remove 1\nmodifier apply 0", ConsoleHost.Run("model.recipe stack9").Text);
        Assert.Equal("forgot stack9", ConsoleHost.Run("model.free stack9").Text);
    }

    [Fact]
    public void ADensityTypedWithOneNumberTakesTheWidthOfItsSlotsTextureAndInfoSaysTheLooks()
    {
        Assert.StartsWith("uvs5: 8 vertices", ConsoleHost.Run("model.box uvs5 2 2 2").Text);
        ConsoleHost.Run("model.select uvs5 +z");

        // A slot with no texture shows the grid, a meter of 1024 pixels.
        Assert.StartsWith("uvs5: ", ConsoleHost.Run("model.uv uvs5 density 512").Text);
        Assert.Equal("uv density 512 1024", Modeling.Find("uvs5")!.Recipe[^1]);

        // A texture's width is read once it has loaded, which no app runs to do here, so the
        // density is refused with how to give the width, and given it the line runs.
        Assert.StartsWith("uvs5: ", ConsoleHost.Run("model.slot uvs5 texture default textures/checker.png").Text);
        Assert.Contains("Give it after the density", ConsoleHost.Run("model.uv uvs5 density 512").Text);
        Assert.StartsWith("uvs5: ", ConsoleHost.Run("model.uv uvs5 density 512 256").Text);
        Assert.StartsWith("uvs5: ", ConsoleHost.Run("model.slot uvs5 roughness default 0.9").Text);
        Assert.Contains("is none of", ConsoleHost.Run("model.uv uvs5 wrap").Text);

        Assert.Contains("slots default (texture textures/checker.png, roughness 0.9)", ConsoleHost.Run("model.info uvs5").Text);
        Assert.EndsWith("uv density 512 256\nslot roughness default 0.9", ConsoleHost.Run("model.recipe uvs5").Text);
        Assert.Equal("forgot uvs5", ConsoleHost.Run("model.free uvs5").Text);
    }

    [SkippableFact]
    public void AShownModelIsDrawnAndFollowsTheCommandsEdits()
    {
        Needs.Renderer();
        var (before, after) = (0f, 0f);
        var (prototype, given, expected) = (AssetHandle.None, AssetHandle.None, AssetHandle.None);

        var run = new PictureRun { Width = 32, Height = 32 };
        string? freed = null;
        run.Do("make and show", world =>
            {
                using (ConsoleHost.Lend(world))
                {
                    ConsoleHost.Run("model.box shown4 1 1 1");
                    ConsoleHost.Run("model.show shown4 2 0 0");
                }
            })
            .Wait(2)
            .Do("read and move", world =>
            {
                before = Highest(world);
                prototype = Render.MaterialOf(world.Resource<EcsWorld>(), Modeling.Entry("shown4")!.Parts[0]);
                using (ConsoleHost.Lend(world))
                {
                    ConsoleHost.Run("model.select shown4 +y");
                    ConsoleHost.Run("model.move shown4 0 1 0");
                    ConsoleHost.Run("model.slot shown4 color default 1 0 0");
                }
            })
            .Wait(2)
            .Do("read again and forget", world =>
            {
                after = Highest(world);
                given = Render.MaterialOf(world.Resource<EcsWorld>(), Modeling.Entry("shown4")!.Parts[0]);
                expected = Modeling.Materials(Modeling.Find("shown4")!)[0];
                using (ConsoleHost.Lend(world)) freed = ConsoleHost.Run("model.free shown4").Text;
            })
            .Go();

        // The box's top a meter up in the mesh the part draws with, then two once the command moved
        // it, written in place by the frame's tick.
        Assert.Equal((1f, 2f), (before, after));
        Assert.Equal("forgot shown4", freed);

        // Drawn in its slot's prototype, then in the material of the color the slot was given.
        Assert.NotEqual(AssetHandle.None, prototype);
        Assert.NotEqual(prototype, given);
        Assert.Equal(expected, given);

        static float Highest(World world)
        {
            var part = Modeling.Entry("shown4")!.Parts[0];
            Assert.True(Render.TryReadMesh(Render.MeshOf(world.Resource<EcsWorld>(), part), out var mesh));
            return mesh!.Positions.Max(point => point.Y);
        }
    }
}

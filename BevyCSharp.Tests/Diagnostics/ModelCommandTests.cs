using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the modeling commands through the console, the guide's room with a window made a line at
/// a time and its counts read from the answers, a save loaded back as the same model, undo and redo,
/// the refusals, and a model shown in a running app following the commands' edits.
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
        Assert.Equal("hall1: 16 vertices, 24 edges, 12 faces, 12 faces and 16 vertices selected", ConsoleCommands.Run("model.room hall1 6 3 5 0.2"));
        Assert.StartsWith("hall1: 16 vertices, 24 edges, 12 faces, 2 faces", ConsoleCommands.Run("model.select hall1 -z"));

        // The window's four sides selected, a corner more than their eight where the wall's pieces
        // meet one of their edges.
        var cut = ConsoleCommands.Run("model.cut hall1 window 1 1.2 2 0.9");
        Assert.NotNull(cut);
        Assert.EndsWith("4 faces and 9 vertices selected", cut);

        var mesh = Modeling.Find("hall1")!;
        Assert.True(mesh.IsClosed);
        Assert.Equal(new[] { "room 6 3 5 0.2", "select -z", "cut window 1 1.2 2 0.9" }, mesh.Recipe);

        var info = ConsoleCommands.Run("model.info hall1");
        Assert.Contains("closed", info);
        Assert.Contains("slots default", info);
        Assert.Contains("3 lines of recipe", info);

        var path = _folder.File("hall.model");
        Assert.Equal($"wrote hall1 to {path}", ConsoleCommands.Run($"model.save hall1 {path}"));
        Assert.Equal(cut.Replace("hall1", "hall2"), ConsoleCommands.Run($"model.load hall2 {path}"));
        Assert.Equal(mesh.Recipe, Modeling.Find("hall2")!.Recipe);

        Assert.Contains("hall1: ", ConsoleCommands.Run("model.list"));
        Assert.Contains("hall2: ", ConsoleCommands.Run("model.list"));
        Assert.StartsWith(EditMesh.Header + "\nroom 6 3 5 0.2", ConsoleCommands.Run("model.recipe hall1"));

        Assert.StartsWith("hall1: 16 vertices", ConsoleCommands.Run("model.undo hall1"));
        Assert.Equal(cut, ConsoleCommands.Run("model.redo hall1"));

        Assert.Equal("forgot hall1", ConsoleCommands.Run("model.free hall1"));
        Assert.Equal("forgot hall2", ConsoleCommands.Run("model.free hall2"));
        Assert.DoesNotContain("hall1", Modeling.Names);
    }

    [Fact]
    public void AnOperationOnANameNoModelHasIsRefusedAndAPrimitiveMakesOne()
    {
        Assert.StartsWith("There is no model called absent3", ConsoleCommands.Run("model.select absent3 +y"));
        Assert.Null(Modeling.Find("absent3"));

        Assert.StartsWith("plane3: 4 vertices", ConsoleCommands.Run("model.plane plane3 2 2"));
        Assert.Contains("is not a number", ConsoleCommands.Run("model.move plane3 1 up 0"));
        Assert.Contains("needs another number", ConsoleCommands.Run("model.extrude plane3 0 1"));
        Assert.StartsWith("plane3: 8 vertices", ConsoleCommands.Run("model.extrude plane3 1"));
        Assert.StartsWith("plane3 has no operation", ConsoleCommands.Run("model.redo plane3"));

        Assert.Equal("forgot plane3", ConsoleCommands.Run("model.free plane3"));
    }

    [SkippableFact]
    public void AShownModelIsDrawnAndFollowsTheCommandsEdits()
    {
        Needs.Renderer();
        var (before, after) = (0f, 0f);

        var run = new PictureRun { Width = 32, Height = 32 };
        string? freed = null;
        run.Do("make and show", world =>
            {
                using (ConsoleHost.Lend(world))
                {
                    ConsoleCommands.Run("model.box shown4 1 1 1");
                    ConsoleCommands.Run("model.show shown4 2 0 0");
                }
            })
            .Wait(2)
            .Do("read and move", world =>
            {
                before = Highest(world);
                using (ConsoleHost.Lend(world))
                {
                    ConsoleCommands.Run("model.select shown4 +y");
                    ConsoleCommands.Run("model.move shown4 0 1 0");
                }
            })
            .Wait(2)
            .Do("read again and forget", world =>
            {
                after = Highest(world);
                using (ConsoleHost.Lend(world)) freed = ConsoleCommands.Run("model.free shown4");
            })
            .Go();

        // The box's top a meter up in the mesh the part draws with, then two once the command moved
        // it, written in place by the frame's tick.
        Assert.Equal((1f, 2f), (before, after));
        Assert.Equal("forgot shown4", freed);

        static float Highest(World world)
        {
            var part = Modeling.Entry("shown4")!.Parts[0];
            Assert.True(Render.TryReadMesh(Render.MeshOf(world.Resource<EcsWorld>(), part), out var mesh));
            return mesh!.Positions.Max(point => point.Y);
        }
    }
}

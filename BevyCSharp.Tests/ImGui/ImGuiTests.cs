using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What the interface does when there is no interface, and what an app's interface leaves to the
/// next app in the process. The entry points exist either way, so the managed side links against
/// one library and finds out at the call rather than at load.
/// </summary>
/// <remarks>
/// In the engine collection, because it runs a real app and two of those at once is not something
/// the native side allows. The component registry belongs to whichever app is current, so a test
/// building one while another is being torn down fails on a registration that has nowhere to go.
/// </remarks>
[Collection("engine")]
public sealed class ImGuiTests
{
    [SkippableFact]
    public void ABuildWithoutTheInterfaceDrawsNothingRatherThanCrashing()
    {
        Needs.NoRenderer();

        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, _ =>
        {
            // Starting it says so and stops, rather than creating a context nothing can draw.
            ImGuiRuntime.Start();
            Assert.False(ImGuiRuntime.IsRunning);

            // And the things that read from it answer for a context that was never made.
            Assert.False(ImGuiRuntime.WantsMouse);
            Assert.False(ImGuiRuntime.WantsKeyboard);
        });

        harness.Run();
    }

    [Fact]
    public void APictureAskedForWithNoInterfaceIsNoPictureAndLoadsNothing()
    {
        // Frames enough for Bevy's reader to have said a missing file failed, had it been asked to
        // read one, which the hook of N 3.7 would fail the test for.
        using var harness = new EngineHarness(frames: 30, fps: 240);

        harness.OnContext(Stage.Startup, _ =>
        {
            // Zero is "no picture", and with no interface running there is nothing to keep one
            // in, in any build, so the path is not read at all and asked again answers the same.
            Assert.Equal(0uL, ImGuiTextures.Load("nowhere.png"));
            Assert.Equal(0uL, ImGuiTextures.Load("nowhere.png"));
        });

        harness.Run();
    }

    /// <summary>
    /// An app that leaves the interface out has none, after an app in the same process that asked
    /// for it as well, so starting the interface there says what the app left out.
    /// </summary>
    [SkippableFact]
    public void AnAppThatLeavesTheInterfaceOutHasNoneAfterOneThatAskedForIt()
    {
        Needs.Renderer();

        bool? asked = null, left = null;
        new PictureRun
        {
            Width = 64,
            Height = 64,
            Configure = config => config.Gui = true,
            Scene = _ => asked = App.HasInterface,
        }.Wait(2).Go();

        using (var harness = new EngineHarness(frames: 2))
        {
            harness.OnContext(Stage.Startup, _ => left = App.HasInterface);
            harness.Run();
        }

        Assert.True(asked);
        Assert.False(left);
    }

    /// <summary>
    /// A picture an earlier app in the process loaded is loaded again for the next, so its path
    /// draws what it holds, where the earlier app's number named another picture in the new table.
    /// </summary>
    [SkippableFact]
    public void APictureIsLoadedAgainForEachApp()
    {
        Needs.Renderer();

        Assert.Equal((2u, 2u), SizeOfTheLast("textures/checker.png"));
        Assert.Equal((2u, 2u), SizeOfTheLast("textures/cubemap.png", "textures/checker.png"));
    }

    /// <summary>
    /// The size of the last of <paramref name="paths"/>, loaded in that order by an app with the
    /// interface running, once it has arrived.
    /// </summary>
    private static (uint Width, uint Height) SizeOfTheLast(params string[] paths)
    {
        var (pictures, size) = (new ulong[paths.Length], (0u, 0u));
        new PictureRun
        {
            Width = 64,
            Height = 64,
            Configure = config => config.Gui = true,
            Scene = _ =>
            {
                ImGuiRuntime.Start();
                for (var i = 0; i < paths.Length; i++) pictures[i] = ImGuiTextures.Load(paths[i]);
            },
            EachFrame = world =>
            {
                ImGuiRuntime.Begin(new BehaviorContext(world));
                ImGuiRuntime.End();
            },
        }.Until("the pictures arriving", _ => (size = ImGuiTextures.SizeOf(pictures[^1])) != (0u, 0u)).Go();

        return size;
    }
}

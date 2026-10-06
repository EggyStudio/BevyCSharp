using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What the interface does when there is no interface. The entry points exist either way, so the
/// managed side links against one library and finds out at the call rather than at load.
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
        Needs.NoEditor();

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
}

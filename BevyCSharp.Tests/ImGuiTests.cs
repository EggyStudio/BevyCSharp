using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What the interface does when there is no interface: the entry points exist either way, so the
/// managed side links against one library and finds out at the call rather than at load.
/// </summary>
public sealed class ImGuiTests
{
    [Fact]
    public void ABuildWithoutTheInterfaceDrawsNothingRatherThanCrashing()
    {
        using var harness = new EngineHarness(frames: 3);
        if (App.HasEditor) return;

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
    public void APictureFromNowhereIsNoPicture()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, _ =>
        {
            // Zero is what the interface is told to mean "no picture", so a path that names
            // nothing has to answer it rather than a name pointing at nothing.
            Assert.Equal(0uL, ImGuiTextures.Load(string.Empty.Length == 0 ? "nowhere.png" : "x"));
        });

        harness.Run();
    }
}

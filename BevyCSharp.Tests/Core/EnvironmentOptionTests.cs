using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers what the environment asks of an app that its config does not say.</summary>
/// <remarks>In the engine collection, since it runs an app, and the environment is the process's.</remarks>
[Collection("engine")]
public sealed class EnvironmentOptionTests
{
    [Fact]
    public void ARunWithNoWindowEndsAfterTheFramesTheEnvironmentNames()
    {
        var frames = 0;
        Environment.SetEnvironmentVariable(Config.FramesVariable, "5");

        try
        {
            // Headless with no frame count of its own, which would otherwise run until stopped.
            using var app = new App(new Config { Headless = true });
            app.AddSystem(Stage.Update, new SystemDescriptor(_ => frames++, "Test.Count"));
            Assert.Equal(0, app.Run());
        }
        finally
        {
            Environment.SetEnvironmentVariable(Config.FramesVariable, null);
        }

        Assert.InRange(frames, 4, 6);
    }

    [Fact]
    public void TheEnvironmentSetsTheClockWhereTheConfigLeftTheMachines()
    {
        var read = 0.0;
        Environment.SetEnvironmentVariable(Config.FrameTimeVariable, "0.02");

        try
        {
            using (var app = new App(new Config { Headless = true, HeadlessFrames = 2 }))
            {
                app.AddSystem(Stage.Update, new SystemDescriptor(world => read = world.Resource<Time>().FrameSeconds, "Test.Read"));
                Assert.Equal(0, app.Run());
                Assert.Equal(0.02, app.Config.FrameSeconds);
            }

            // A config that set its own keeps it.
            using var own = new App(new Config { Headless = true, HeadlessFrames = 1, FrameSeconds = 0.5 });
            Assert.Equal(0.5, own.Config.FrameSeconds);
        }
        finally
        {
            Environment.SetEnvironmentVariable(Config.FrameTimeVariable, null);
        }

        Assert.Equal(0.02, read, 9);
    }
}

using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers wireframes drawn only in an app that asked for them, whose config adds the plugins that
/// draw them.
/// </summary>
/// <remarks>
/// Bevy's wireframe plugins look at every mesh each frame whether or not one is drawn as edges,
/// so an app that never draws one leaves them out, and a wireframe turned on there would draw
/// nothing and say nothing. It is refused instead, naming what to ask for.
/// </remarks>
[Collection("engine")]
public sealed class WireframeTests
{
    [SkippableFact]
    public void AWireframeIsRefusedWhereTheConfigDidNotAskForOne()
    {
        Needs.Renderer();

        var refused = TurnOn(new Config { Offscreen = true, Width = 64, Height = 64, HeadlessFrames = 4 });

        Assert.IsType<InvalidOperationException>(refused);
        Assert.Contains("Config.Wireframes", refused!.Message);
    }

    [SkippableFact]
    public void AWireframeIsDrawnWhereTheConfigAskedForOne()
    {
        Needs.Renderer();

        var refused = TurnOn(new Config { Offscreen = true, Width = 64, Height = 64, HeadlessFrames = 4, Wireframes = true });

        Assert.Null(refused);
    }

    /// <summary>Turns a cube's wireframe on at startup, and answers what that threw, if anything.</summary>
    private static Exception? TurnOn(Config config)
    {
        Exception? thrown = null;
        using var app = new App(config);
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();
                var cube = ecs.Spawn();
                ecs.Add(cube, Transform.Identity);
                Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
                thrown = Record.Exception(() => Render.SetWireframe(cube, true, (1f, 1f, 1f, 1f)));
            },
            "Test.Wireframe"));

        Assert.Equal(0, app.Run());
        return thrown;
    }
}

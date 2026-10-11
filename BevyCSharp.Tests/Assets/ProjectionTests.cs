using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a camera's projection of the game's own, Bevy's perspective with entries of its matrix
/// written over it.
/// </summary>
[Collection("engine")]
public sealed class ProjectionTests
{
    /// <summary>
    /// A camera takes the adjusted perspective as Bevy's custom projection, an entity that is no
    /// camera is refused, and so is an entry past the matrix's fourth column before it reaches the
    /// bridge.
    /// </summary>
    [SkippableFact]
    public void ACameraTakesAnAdjustedPerspectiveAndANonCameraIsRefused()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        string? variant = null;
        int refused = NativeStatus.Ok;
        ArgumentOutOfRangeException? outside = null;

        harness.OnContext(Stage.Update, ctx =>
        {
            var ecs = ctx.Ecs;
            if (variant is not null) return;

            var camera = Render.SpawnCamera3d();
            Render.SetAdjustedPerspective(camera, [(2, 0, 0.2f), (2, 1, 0.6f)]);
            variant = ecs.GetVariant(camera, ProjectionRef.TypePath, "");

            refused = Assert.Throws<BevyNativeException>(() => Render.SetAdjustedPerspective(ecs.Spawn(), [(2, 0, 0.2f)])).Status;
            outside = Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetAdjustedPerspective(camera, [(4, 0, 1f)]));
        });

        harness.Run();

        Assert.Equal("Custom", variant);
        Assert.Equal(NativeStatus.NoComponent, refused);
        Assert.NotNull(outside);
    }
}

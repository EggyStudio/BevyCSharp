using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Bevy's forward decals, an entity drawing its material onto the surfaces inside its box.
/// </summary>
[Collection("engine")]
public sealed class DecalTests
{
    /// <summary>
    /// An entity made a forward decal with a decal material is given Bevy's box mesh to draw it on,
    /// and a standard material is refused, since it would draw the box rather than what is behind it.
    /// </summary>
    [SkippableFact]
    public void AForwardDecalIsGivenItsBoxAndAStandardMaterialIsRefused()
    {
        Needs.Renderer();

        var boxed = false;
        int refused = NativeStatus.Ok;

        // A run that draws, as a headless one has no decals to draw.
        using var app = new App(Config.OffscreenFor(64, 64, frames: 2));
        app.AddPlugin(new EnginePlugin());
        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            if (boxed) return;

            var material = Render.CreateDecalMaterial(new MaterialSettings { BaseColor = (0.3f, 0.5f, 0.8f, 1f) }, depthFadeFactor: 0.5f, depthBias: 2f);
            var decal = ecs.Spawn();
            ecs.Add(decal, new Transform(Vec3.Zero, Quat.Identity, new Vec3(4f)));
            Render.SetForwardDecal(decal, material);
            boxed = ecs.Get<Mesh3dRef>(decal) is not null;

            var plain = ecs.Spawn();
            refused = Assert.Throws<BevyNativeException>(() => Render.SetForwardDecal(plain, Render.CreateMaterial((1f, 1f, 1f, 1f)))).Status;
        }, "Test.Decal"));

        Assert.Equal(0, app.Run());
        Assert.True(boxed, "the decal was not given Bevy's box mesh");
        Assert.Equal(NativeStatus.NoComponent, refused);
    }
}

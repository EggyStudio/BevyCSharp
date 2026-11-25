using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Bevy's materials stay deferred while a camera reads the G-buffer, whatever
/// <see cref="Render.SetDeferredRendering"/> is asked meanwhile, and go forward once none does.
/// </summary>
/// <remarks>
/// Materials drawn forward leave a G-buffer empty, so a camera asking for one, as global
/// illumination's does to take Bevy's light back and add its own, would read nothing, and the
/// light it adds would go out with nothing said, which a program turning deferred off as its
/// reflections went did. A camera with a prepass of depth alone is given the G-buffer while
/// materials are deferred and has it taken off once they are forward, which is how the test reads
/// which they are.
/// </remarks>
[Collection("engine")]
public sealed class DeferredHoldTests
{
    [SkippableFact]
    public void ForwardWaitsWhileACameraReadsTheGBuffer()
    {
        Needs.Renderer();

        var (reader, other) = (Entity.None, Entity.None);
        var (held, released) = (false, true);
        new PictureRun
        {
            Scene = ecs =>
            {
                reader = PictureRun.Camera(ecs);
                Render.SetPostProcessing(reader, new PostSettings { Msaa = 1 });
                other = Render.SpawnCamera3d(new CameraSettings { Order = 1 });
                Render.SetPostProcessing(other, new PostSettings { Msaa = 1 });
            },
        }
            .Wait(ShaderMaterialTests.Settled)
            .Do("asking", _ =>
            {
                Shaders.SetPrepass(other, depth: true);
                Shaders.SetPrepass(reader, depth: true, deferred: true);
            })
            .Wait(5)
            .Do("forward asked", _ => Render.SetDeferredRendering(false))
            .Wait(5)
            .Do("held", world =>
            {
                held = world.Resource<EcsWorld>().Get<DeferredPrepassRef>(other) is not null;
                Shaders.SetPrepass(reader, depth: true);
            })
            .Wait(5)
            .Do("released", world => released = world.Resource<EcsWorld>().Get<DeferredPrepassRef>(other) is not null)
            .Go();

        Assert.True(held, "materials went forward under a camera reading the G-buffer");
        Assert.False(released, "materials stayed deferred after the last camera reading the G-buffer stopped asking");
    }
}

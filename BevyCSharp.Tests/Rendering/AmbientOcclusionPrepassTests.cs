using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A camera's ambient occlusion keeps the depth and normal prepasses it reads while it is on,
/// whatever else asks the camera for a prepass or takes the G-buffer off it.
/// </summary>
/// <remarks>
/// Bevy's occlusion brings both prepasses when it comes, and takes the normals unchecked as it
/// builds its bind group. A prepass asked for without normals would take them off the camera under
/// it and end the app at its next frame, which global illumination, asking for depth and the
/// G-buffer, would do to a camera with occlusion on. Materials going forward would take the depth
/// the G-buffer brought, which the occlusion finds there and so brings none of its own, and the
/// occlusion would go out with nothing said.
/// </remarks>
[Collection("engine")]
public sealed class AmbientOcclusionPrepassTests
{
    [SkippableFact]
    public void APrepassAskedForWithoutNormalsLeavesTheOcclusionItsNormals()
    {
        Needs.Renderer();

        var view = Entity.None;
        var (depth, normals) = (false, false);
        new PictureRun
        {
            Scene = ecs =>
            {
                view = PictureRun.Camera(ecs);
                Render.SetPostProcessing(view, new PostSettings { Msaa = 1 });
                Render.SetAmbientOcclusion(view, AmbientOcclusionQuality.High);
            },
        }
            .Wait(ShaderMaterialTests.Settled)
            .Do("asking for depth alone", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                Shaders.SetPrepass(view, depth: true);
                (depth, normals) = (ecs.Get<DepthPrepassRef>(view) is not null, ecs.Get<NormalPrepassRef>(view) is not null);

                // Bevy ends the app at its next frame without the normals, so the occlusion goes
                // at once where they went, and the run goes on to say so.
                if (!normals) Render.SetAmbientOcclusion(view, null);
            })
            .Wait(10)
            .Go();

        Assert.True(depth && normals, $"a prepass of depth alone left the occlusion with depth {depth} and normals {normals}");
    }

    [SkippableFact]
    public void MaterialsGoingForwardLeaveTheOcclusionTheDepthItFound()
    {
        Needs.Renderer();

        var view = Entity.None;
        var (deferredDepth, depth, normals) = (false, false, false);
        new PictureRun
        {
            Scene = ecs =>
            {
                view = PictureRun.Camera(ecs);
                Render.SetPostProcessing(view, new PostSettings { Msaa = 1 });
            },
        }
            .Wait(ShaderMaterialTests.Settled)
            .Do("deferred", _ => Render.SetDeferredRendering(true))
            .Wait(5)
            .Do("occluding", world =>
            {
                deferredDepth = world.Resource<EcsWorld>().Get<DepthPrepassRef>(view) is not null;
                Render.SetAmbientOcclusion(view, AmbientOcclusionQuality.High);
            })
            .Wait(5)
            .Do("forward", _ => Render.SetDeferredRendering(false))
            .Wait(5)
            .Do("reading", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                (depth, normals) = (ecs.Get<DepthPrepassRef>(view) is not null, ecs.Get<NormalPrepassRef>(view) is not null);
            })
            .Go();

        // Without the G-buffer bringing the depth, the occlusion would bring its own and the check
        // would pass whatever happened as materials went forward.
        Assert.True(deferredDepth, "the G-buffer brought no depth prepass to the camera");
        Assert.True(depth && normals, $"materials going forward left the occlusion with depth {depth} and normals {normals}");
    }
}

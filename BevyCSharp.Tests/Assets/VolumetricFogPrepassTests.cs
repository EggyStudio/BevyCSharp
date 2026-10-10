using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A camera's volumetric fog, marched through a fog volume off to one side, stays in the volume
/// with and without a depth prepass.
/// </summary>
/// <remarks>
/// On Bevy 0.19 the fog hazed the whole picture, the sky with it, once a depth prepass was on the
/// camera, as ambient occlusion brings, which the feature test's hall worked round by giving the
/// camera its fog only inside. Bevy 0.20 keeps it in its volume, which this holds it to, a picture
/// without the fog and one with it differing only where the volume is.
/// </remarks>
[Collection("engine")]
public sealed class VolumetricFogPrepassTests
{
    [SkippableTheory]
    [InlineData("none")]
    [InlineData("depth")]
    [InlineData("occlusion")]
    public void FogStaysInItsVolume(string camera)
    {
        Needs.Renderer();

        var view = Entity.None;
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                view = PictureRun.Camera(ecs);
                Render.SetPostProcessing(view, new PostSettings { Msaa = 1 });
                if (camera == "depth") Shaders.SetPrepass(view, depth: true);
                if (camera == "occlusion") Render.SetAmbientOcclusion(view, AmbientOcclusionQuality.High);

                // A volume at the right of the view, lit by a spot shining down into it.
                var volume = ecs.Spawn();
                ecs.Add(volume, new Transform(new Vec3(2f, 0f, 0f), Quat.Identity, new Vec3(1.5f, 3f, 1.5f)));
                var settings = ecs.Insert<FogVolumeRef>(volume);
                (settings.DensityFactor, settings.Scattering, settings.Absorption) = (0.5f, 0.6f, 0.05f);

                var spot = Render.SpawnLight(new LightSettings { Kind = LightKind.Spot, Intensity = 1_200_000f, Range = 12f, InnerAngle = 0.25f, OuterAngle = 0.4f, Shadows = true });
                ecs.Add(spot, Transform.LookingAt(new Vec3(2f, 3f, 0f), new Vec3(2f, 0f, 0f), Vec3.UnitX));
                ecs.Insert<VolumetricLightRef>(spot);
            },
        };

        run.Wait(ShaderMaterialTests.Settled)
            .Capture("clear")
            .Do("fog", world =>
            {
                var fog = world.Resource<EcsWorld>().Insert<VolumetricFogRef>(view);
                (fog.StepCount, fog.AmbientIntensity) = (64u, 0f);
            })
            .Wait(30)
            .Capture("fogged")
            .Go();

        var (clear, fogged) = (run.Picture("clear"), run.Picture("fogged"));
        var (left, right) = (Change(clear, fogged, 0, clear.Width / 3), Change(clear, fogged, clear.Width * 2 / 3, clear.Width));

        // Without the fog showing in its volume, the check of the rest would pass whatever it did.
        Assert.True(right > 5, $"the fog volume did not show, the right third changed by {right:0.00} with {camera}");
        Assert.True(left < 1, $"the fog hazed the left third, outside its volume, by {left:0.00} with {camera}");
    }

    /// <summary>The mean change of the channels between two pictures over columns from one to another.</summary>
    private static double Change(CapturedImage before, CapturedImage after, uint from, uint to)
    {
        long sum = 0;
        for (var y = 0u; y < before.Height; y++)
        {
            for (var x = from; x < to; x++)
            {
                var (a, b) = (before.At(x, y), after.At(x, y));
                sum += Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
            }
        }

        return sum / (3.0 * before.Height * (to - from));
    }
}

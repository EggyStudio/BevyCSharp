// Bevy's fog_volumes example, examples/3d/fog_volumes.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates fog volumes, a bunny of fog lit by the sun. Bevy's FogVolume, VolumetricLight and
// VolumetricFog are put on through reflection, with the bunny's density texture set as a reflected
// asset.
internal static class FogVolumes
{

    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render.SetAmbientLight((1f, 1f, 1f), 0f);

            var fog = ecs.Spawn();
            ecs.Add(fog, Transform.At(0f, 0.5f, 0f));
            var volume = ecs.Insert<FogVolumeRef>(fog);
            volume.DensityTexture = AssetServer.Load(AssetKind.Image, "volumes/bunny.ktx2");
            (volume.DensityFactor, volume.Scattering) = (1f, 1f);

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 32_000f, Shadows = true });
            ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 1f, -0.3f), new Vec3(0f, 0.5f, 0f), Vec3.UnitY));
            ecs.Insert<VolumetricLightRef>(sun);

            _camera = ecs.Camera(Transform.LookingAt(new Vec3(-0.75f, 1f, 2f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(_camera, new PostSettings { Hdr = true });
            var volumetric = ecs.Insert<VolumetricFogRef>(_camera);
            (volumetric.StepCount, volumetric.AmbientIntensity) = (64u, 0f);
        });

        // The camera goes round the bunny, a hundredth of a radian a frame.
        app.Update(ctx =>
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(_camera).Translation;
            ctx.Ecs.Set(_camera, Transform.LookingAt(Quat.FromRotationY(0.01f) * at, new Vec3(0f, 0.5f, 0f), Vec3.UnitY));
        }, "fog_volumes.RotateCamera");
    }
}

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates fog volumes, a bunny of fog lit by the sun. Bevy's FogVolume, VolumetricLight and
// VolumetricFog are put on through reflection, with the bunny's density texture set as a reflected
// asset.
internal static class FogVolumes
{
    private const string Volume = "bevy_light::volumetric::FogVolume";

    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render.SetAmbientLight((1f, 1f, 1f), 0f);

            var fog = ecs.Spawn();
            ecs.Add(fog, Transform.At(0f, 0.5f, 0f));
            ecs.InsertReflected(fog, Volume);
            ecs.SetReflectedAsset(fog, Volume, ".density_texture", AssetServer.Load(AssetKind.Image, "volumes/bunny.ktx2"));
            ecs.SetReflected(fog, Volume, ".density_factor", "1.0");
            ecs.SetReflected(fog, Volume, ".scattering", "1.0");

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 32_000f, Shadows = true });
            ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 1f, -0.3f), new Vec3(0f, 0.5f, 0f), Vec3.UnitY));
            ecs.InsertReflected(sun, "bevy_light::volumetric::VolumetricLight");

            _camera = ecs.Camera(Transform.LookingAt(new Vec3(-0.75f, 1f, 2f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(_camera, new PostSettings { Hdr = true });
            ecs.InsertReflected(_camera, "bevy_light::volumetric::VolumetricFog");
            ecs.SetReflected(_camera, "bevy_light::volumetric::VolumetricFog", ".step_count", "64");
            ecs.SetReflected(_camera, "bevy_light::volumetric::VolumetricFog", ".ambient_intensity", "0.0");
        });

        // The camera goes round the bunny, a hundredth of a radian a frame.
        app.Update(ctx =>
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(_camera).Translation;
            ctx.Ecs.Set(_camera, Transform.LookingAt(Quat.FromRotationY(0.01f) * at, new Vec3(0f, 0.5f, 0f), Vec3.UnitY));
        }, "fog_volumes.RotateCamera");
    }
}

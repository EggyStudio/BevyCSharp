using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Showcases a fog volume whose density texture scrolls through it, as wind would carry fog through
// beams of light. Bevy's FogVolume, VolumetricLight and VolumetricFog are put on through
// reflection, and the noise is loaded to repeat, as Bevy's example asks of its loader.
internal static class ScrollingFog
{
    private const string Volume = "bevy_light::volumetric::FogVolume";

    private static Entity _fog;
    private static float _offset;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _offset = 0f;
            Render.SetShadowMapSize(directional: 4096);

            var camera = ecs.Camera(Transform.LookingAt(new Vec3(0f, 2f, 0f), new Vec3(-5f, 3.5f, -6f), Vec3.UnitY));
            Render.SetPostProcessing(camera, new PostSettings { Msaa = 1, AntiAlias = AntiAliasPass.Temporal, Bloom = true });
            ecs.InsertReflected(camera, "bevy_light::volumetric::VolumetricFog");
            ecs.SetReflected(camera, "bevy_light::volumetric::VolumetricFog", ".ambient_intensity", "0.0");
            ecs.SetReflected(camera, "bevy_light::volumetric::VolumetricFog", ".jitter", "0.5");

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
            ecs.Add(sun, Transform.LookingAt(new Vec3(-5f, 5f, -7f), Vec3.Zero, Vec3.UnitY));
            ecs.InsertReflected(sun, "bevy_light::volumetric::VolumetricLight");

            var black = Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 0f, 0f, 1f), Roughness = 1f });
            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 64f, 1f, 64f), black, Transform.At(0f, -0.5f, 0f));
            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 2f, 9f, 2f), Scene.Material((0f, 0f, 0f, 1f)), Transform.At(-10f, 4.5f, -11f));

            _fog = ecs.Spawn();
            ecs.Add(_fog, new Transform(new Vec3(0f, 32f, 0f), Quat.Identity, new Vec3(64f)));
            ecs.InsertReflected(_fog, Volume);
            ecs.SetReflectedAsset(_fog, Volume, ".density_texture", AssetServer.LoadImage("volumes/fog_noise.ktx2", TextureSettings.Tiling));
            ecs.SetReflected(_fog, Volume, ".density_factor", "0.05");
        });

        // The texture moves through the volume along Z, four hundredths of it a second.
        app.Update(ctx =>
        {
            _offset += 0.04f * ctx.Time.Delta;
            ctx.Ecs.SetReflected(_fog, Volume, ".density_texture_offset", FormattableString.Invariant($"[0.0,0.0,{_offset}]"));
        }, "scrolling_fog.ScrollFog");
    }
}

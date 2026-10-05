// Bevy's scrolling_fog example, examples/3d/scrolling_fog.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Showcases a fog volume whose density texture scrolls through it, as wind would carry fog through
// beams of light. Bevy's FogVolume, VolumetricLight and VolumetricFog are put on through
// reflection, and the noise is loaded to repeat, as Bevy's example asks of its loader.
internal static class ScrollingFog
{

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
            var volumetric = ecs.Insert<VolumetricFogRef>(camera);
            (volumetric.AmbientIntensity, volumetric.Jitter) = (0f, 0.5f);

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
            ecs.Add(sun, Transform.LookingAt(new Vec3(-5f, 5f, -7f), Vec3.Zero, Vec3.UnitY));
            ecs.Insert<VolumetricLightRef>(sun);

            var black = Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 0f, 0f, 1f), Roughness = 1f });
            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 64f, 1f, 64f), black, Transform.At(0f, -0.5f, 0f));
            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 2f, 9f, 2f), Scene.Material((0f, 0f, 0f, 1f)), Transform.At(-10f, 4.5f, -11f));

            _fog = ecs.Spawn();
            ecs.Add(_fog, new Transform(new Vec3(0f, 32f, 0f), Quat.Identity, new Vec3(64f)));
            var volume = ecs.Insert<FogVolumeRef>(_fog);
            volume.DensityTexture = AssetServer.LoadImage("volumes/fog_noise.ktx2", TextureSettings.Tiling);
            volume.DensityFactor = 0.05f;
        });

        // The texture moves through the volume along Z, four hundredths of it a second.
        app.Update(ctx =>
        {
            _offset += 0.04f * ctx.Time.Delta;
            ctx.Ecs.Wrap<FogVolumeRef>(_fog).DensityTextureOffset = new Vec3(0f, 0f, _offset);
        }, "scrolling_fog.ScrollFog");
    }
}

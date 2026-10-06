// Bevy's update_gltf_scene example, examples/gltf/update_gltf_scene.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Gltf;

// Two flight helmets from one glTF file, one as it loads and one whose parts are moved apart and
// together, each further than the last.
internal static class UpdateGltfScene
{
    private static Entity _moved;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render.SetShadowMapSize(directional: 4096);
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
            ecs.Add(sun, Transform.LookingAt(new Vec3(4f, 25f, 8f), Vec3.Zero, Vec3.UnitY));

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-0.5f, 0.9f, 1.5f), new Vec3(-0.5f, 0.3f, 0f), Vec3.UnitY));
            Render.SetEnvironmentMap(camera,
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
                150f);

            var helmet = AssetServer.LoadGltfScene("models/FlightHelmet/FlightHelmet.gltf");
            var still = ecs.SpawnScene(helmet);
            ecs.Set(still, Transform.At(-1f, 0f, 0f));
            _moved = ecs.SpawnScene(helmet);
        }, "update_gltf_scene.Setup");

        // Every entity under the moved helmet, nearest first, half a step further along X than the
        // one before.
        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var t = ctx.Time.Elapsed;
            var offset = 0f;
            foreach (var entity in ecs.Descendants(_moved))
            {
                if (!ecs.TryGet<Transform>(entity, out var at)) continue;
                ecs.Set(entity, at with { Translation = new Vec3(offset * MathF.Sin(t) / 20f, 0f, MathF.Cos(t) / 20f) });
                offset += 0.5f;
            }
        }, "update_gltf_scene.MoveSceneEntities");
    }
}

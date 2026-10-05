using Bevy;

namespace BevyCSharp.Examples.Gltf;

// Loads the flight helmet from a glTF file and shows it lit by an environment map and a sun that
// circles it, its shadows in one cascade a little over a helmet's length deep.
internal static class LoadGltf
{
    private static Entity _sun;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render.SetShadowMapSize(directional: 4096);
            var camera = ecs.Camera(Transform.LookingAt(new Vec3(0.7f, 0.7f, 1f), new Vec3(0f, 0.3f, 0f), Vec3.UnitY));
            Render.SetEnvironmentMap(camera,
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
                250f);

            _sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
            ecs.Add(_sun, Transform.Identity);
            Render.SetShadowCascades(_sun, cascades: 1, maximum: 1.6f);

            ecs.SpawnScene(AssetServer.LoadGltfScene("models/FlightHelmet/FlightHelmet.gltf"));
        }, "load_gltf.Setup");

        // Bevy's EulerRot::ZYX of nothing, a fifth of a turn a second about Y, and down by an
        // eighth of a turn.
        app.Update(ctx => ctx.Ecs.Set(_sun, new Transform(Vec3.Zero, Quat.FromRotationY(ctx.Time.Elapsed * MathF.PI / 5f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One)),
            "load_gltf.AnimateLightDirection");
    }
}

/// <summary>A scene's entities, nearest first, as Bevy's <c>iter_descendants</c> walks them.</summary>
internal static class Descendants
{
    public static IEnumerable<Entity> Of(EcsWorld ecs, Entity root)
    {
        var queue = new Queue<Entity>(ecs.ChildrenOf(root));
        while (queue.Count > 0)
        {
            var entity = queue.Dequeue();
            yield return entity;
            foreach (var child in ecs.ChildrenOf(entity)) queue.Enqueue(child);
        }
    }
}

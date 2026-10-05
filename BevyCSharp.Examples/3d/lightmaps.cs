// Bevy's lightmaps example, examples/3d/lightmaps.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Rendering a scene with baked lightmaps, a Cornell box lit by nothing but the light stored in its
// images. Bevy's own takes --deferred and --bicubic switches, which this one leaves at their
// defaults, forward and bilinear, since the picture Bevy shows for it is taken that way.
internal static class Lightmaps
{

    // The baked values are stored at a scale of one, and the scene is metered as daylight indoors.
    private const float Exposure = 250f;

    private static Entity _box = Entity.None;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            // No light at all but what was baked, so a dark corner stays dark.
            Render.SetAmbientLight((1f, 1f, 1f), 0f);
            ctx.Ecs.Camera(Transform.At(-278f, 273f, 800f));
        });

        app.SpawnGltf("models/CornellBox/CornellBox.glb", (_, root) => _box = root);
        app.Update(AddLightmaps, "lightmaps.AddLightmaps");
    }

    // Each mesh named in the file is given its lightmap once it is spawned, as Bevy's system does
    // every frame for the meshes that have none yet.
    private static void AddLightmaps(BehaviorContext ctx)
    {
        if (_box == Entity.None) return;

        var ecs = ctx.Ecs;
        foreach (var entity in Descendants(ecs, _box))
        {
            if (ecs.Get<GltfMeshNameRef>(entity) is not { } meshName || ecs.Get<LightmapRef>(entity) is not null) continue;

            var name = meshName.Value;
            var image = name switch
            {
                "large_box" => "lightmaps/CornellBox-Large.zstd.ktx2",
                "small_box" => "lightmaps/CornellBox-Small.zstd.ktx2",
                _ when name.StartsWith("cornell_box", StringComparison.Ordinal) => "lightmaps/CornellBox-Box.zstd.ktx2",
                _ => null,
            };
            if (image is null) continue;

            var material = Render.MaterialOf(ecs, entity);
            if (Render.TryReadMaterial(material, out var settings))
            {
                settings.LightmapExposure = Exposure;
                Render.WriteMaterial(material, settings);
            }

            ecs.Insert<LightmapRef>(entity).Image = AssetServer.Load(AssetKind.Image, image);
        }
    }

    private static IEnumerable<Entity> Descendants(EcsWorld ecs, Entity root)
    {
        foreach (var child in ecs.ChildrenOf(root))
        {
            yield return child;
            foreach (var below in Descendants(ecs, child)) yield return below;
        }
    }
}

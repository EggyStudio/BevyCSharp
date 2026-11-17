// Bevy's lightmaps example, examples/3d/lightmaps.rs at v0.20.0, by Bevy's contributors under MIT
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
    private static bool _lit;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            _lit = false;

            // No light at all but what was baked, so a dark corner stays dark.
            Render.SetAmbientLight((1f, 1f, 1f), 0f);
            ctx.Ecs.SpawnCamera3d(Transform.At(-278f, 273f, 800f));
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
        foreach (var entity in ecs.Descendants(_box))
        {
            // The file's light panel glows at 100 as Bevy shows it, unexposed, and its emission is
            // read here as nits the camera's exposure takes, so it is given the 100,000 nits that
            // Bevy's exposure of EV 9.7 brings to that.
            if (!_lit && ecs.Get<GltfMaterialNameRef>(entity)?.Value == "Emitter_Light_temp"
                && Render.TryReadMaterial(Render.MaterialOf(ecs, entity), out var panel))
            {
                panel.Emissive = (100_000f, 100_000f, 100_000f, 1f);
                Render.WriteMaterial(Render.MaterialOf(ecs, entity), panel);
                _lit = true;
            }

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

}

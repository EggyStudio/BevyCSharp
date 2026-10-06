// Bevy's query_gltf_primitives example, examples/gltf/query_gltf_primitives.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Gltf;

// A model of several meshes from a glTF file, the one whose material its glTF names "Top" picked
// out by that name, its color turned around the hues and its vertices raised and lowered together.
internal static class QueryGltfPrimitives
{
    private static float? _hue;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _hue = null;
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(4f, 4f, 12f), new Vec3(0f, 0f, 0.5f), Vec3.UnitY));

            // Bevy's EulerRot::ZYX of nothing, a radian about Y and down an eighth of a turn.
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
            ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));

            ecs.SpawnScene(AssetServer.LoadGltfScene("models/GltfPrimitives/gltf_primitives.glb"));
        }, "query_gltf_primitives.Setup");

        app.Update(FindTopMaterialAndMesh, "query_gltf_primitives.FindTopMaterialAndMesh");
    }

    // The material's color starts as a bright red given in hue, saturation and lightness, and
    // after that its hue turns a hundred degrees a second. The mesh's every vertex is put at one
    // height, rising and falling about one and a half.
    private static void FindTopMaterialAndMesh(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var row in ecs.Query<Transform>(markChanged: false))
        {
            if (ecs.Get<GltfMaterialNameRef>(row.Entity)?.Value != "Top") continue;

            var material = Render.MaterialOf(ecs, row.Entity);
            if (Render.TryReadMaterial(material, out var settings) && settings is not null)
            {
                _hue = _hue is { } hue ? (hue + ctx.Time.Delta * 100f) % 360f : 0f;
                settings.BaseColor = Color.FromHsl(_hue.Value, 0.9f, 0.7f);
                Render.WriteMaterial(material, settings);
            }

            var mesh = Render.MeshOf(ecs, row.Entity);
            if (Render.TryReadMesh(mesh, out var data) && data is not null)
            {
                var height = 1.5f + 0.5f * MathF.Sin(ctx.Time.Elapsed / 2f);
                for (var i = 0; i < data.Positions.Length; i++) data.Positions[i].Y = height;
                Render.WriteMesh(mesh, data);
            }
        }
    }
}

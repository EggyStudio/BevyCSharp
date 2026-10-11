// Bevy's load_gltf_extras example, examples/gltf/load_gltf_extras.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Gltf;

// Loads a glTF file whose scene, nodes, meshes and materials carry extras, the JSON a modelling tool
// writes beside what glTF itself describes, and lists each entity that holds any, as Bevy's prints
// each of the four with Rust's Debug form.
//
// Bevy's lists the entities in the order its query meets them, and this in the order of their
// indices.
internal static class LoadGltfExtras
{
    private static Entity _display;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(2f, 2f, 2f), Vec3.Zero, Vec3.UnitY));
            Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
            ecs.SpawnScene(AssetServer.LoadGltfScene("models/extras/gltf_extras.glb"));
            _display = Ui.SpawnText("", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }, 15f);
        }, "load_gltf_extras.Setup");

        app.Update(CheckForGltfExtras, "load_gltf_extras.CheckForGltfExtras");
    }

    private static void CheckForGltfExtras(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var scene = ecs.All<GltfSceneExtrasRef>().ToDictionary(e => e.Entity, e => e.Value);
        var primitive = ecs.All<GltfExtrasRef>().ToDictionary(e => e.Entity, e => e.Value);
        var mesh = ecs.All<GltfMeshExtrasRef>().ToDictionary(e => e.Entity, e => e.Value);
        var material = ecs.All<GltfMaterialExtrasRef>().ToDictionary(e => e.Entity, e => e.Value);

        var lines = new List<string>();
        foreach (var entity in scene.Keys.Union(primitive.Keys).Union(mesh.Keys).Union(material.Keys).OrderBy(e => e.Index))
        {
            lines.Add($"Extras per entity {entity.Index}v{entity.Generation} ('Name: {ecs.NameOf(entity)}'):\n"
                + $"    - scene extras:     {Debug("GltfSceneExtras", scene, entity)}\n"
                + $"    - primitive extras: {Debug("GltfExtras", primitive, entity)}\n"
                + $"    - mesh extras:      {Debug("GltfMeshExtras", mesh, entity)}\n"
                + $"    - material extras:  {Debug("GltfMaterialExtras", material, entity)}\n"
                + "                ");
        }

        if (lines.Count > 0) Ui.SetText(_display, string.Join("\n", lines));
    }

    // An extras component as Rust's Debug writes an Option of one, its value quoted and escaped.
    private static string Debug(string type, Dictionary<Entity, string> held, Entity entity)
    {
        if (!held.TryGetValue(entity, out var value)) return "None";

        var quoted = new StringBuilder();
        foreach (var c in value)
        {
            quoted.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => c.ToString(),
            });
        }

        return $"Some({type} {{ value: \"{quoted}\" }})";
    }
}

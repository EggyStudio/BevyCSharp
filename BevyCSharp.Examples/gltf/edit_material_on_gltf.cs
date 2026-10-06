// Bevy's edit_material_on_gltf example, examples/gltf/edit_material_on_gltf.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Gltf;

// Three flight helmets from one glTF file, the outer two with the leather of their straps tinted,
// one red and one green, by copying the material their glTF gave that part and changing the copy.
//
// Bevy changes the materials as each helmet's scene finishes spawning. Here each is looked at once
// its parts carry the names of their glTF materials and those materials have loaded.
internal static class EditMaterialOnGltf
{
    private static readonly List<(Entity Root, (float R, float G, float B, float A)? Color)> Helmets = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Helmets.Clear();
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 1f, 2.5f), new Vec3(0f, 0.25f, 0f), Vec3.UnitY));
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
            ecs.Add(sun, Transform.LookingAt(new Vec3(0f, 1f, 0.25f), Vec3.Zero, Vec3.UnitY));

            var helmet = AssetServer.LoadGltfScene("models/FlightHelmet/FlightHelmet.gltf");
            Helmets.Add((ecs.SpawnScene(helmet), null));

            // Tailwind's red and green, each at 300.
            foreach (var (x, color) in new[] { (-1.25f, Color.FromSrgb8(252, 165, 165)), (1.25f, Color.FromSrgb8(134, 239, 172)) })
            {
                var root = ecs.SpawnScene(helmet);
                ecs.Set(root, Transform.At(x, 0f, 0f));
                Helmets.Add((root, color));
            }
        }, "edit_material_on_gltf.SetupScene");

        app.Update(ChangeMaterial, "edit_material_on_gltf.ChangeMaterial");
    }

    private static void ChangeMaterial(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        for (var i = Helmets.Count - 1; i >= 0; i--)
        {
            var (root, color) = Helmets[i];

            // Ready once its parts are there with the names of their materials, and every one of
            // those materials can be read.
            var named = ecs.Descendants(root)
                .Select(entity => (Entity: entity, Name: ecs.Get<GltfMaterialNameRef>(entity)?.Value))
                .Where(part => part.Name is not null)
                .ToList();
            if (named.Count == 0 || named.Any(part => !Render.TryReadMaterial(Render.MaterialOf(ecs, part.Entity), out _))) continue;

            Helmets.RemoveAt(i);
            Console.WriteLine($"processing Scene Entity: {root}");
            if (color is not { } tint)
            {
                Console.WriteLine($"{root} does not have a color override");
                continue;
            }

            foreach (var (entity, name) in named)
            {
                if (name != "LeatherPartsMat")
                {
                    Console.WriteLine($"not replacing: {name}");
                    continue;
                }

                Console.WriteLine("editing LeatherPartsMat to use ColorOverride tint");
                Render.TryReadMaterial(Render.MaterialOf(ecs, entity), out var settings);
                settings!.BaseColor = tint;
                Render.SetMaterial(ecs, entity, Render.CreateMaterial(settings));
            }
        }
    }
}

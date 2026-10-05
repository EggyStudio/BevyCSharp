// Bevy's array_texture example, examples/shader/array_texture.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Shading;

// A texture of four layers, loaded from one file of four pictures stacked, drawn on a row of cubes
// that share one material, each showing the layer its MeshTag names, which moves on every second
// and a half.
internal static class ArrayTexture
{
    private const uint TextureCount = 4;
    private static float _timer;
    private static readonly List<Entity> Cubes = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _timer = 0f;
            Cubes.Clear();
            var arrayTexture = AssetServer.LoadImage("textures/array_texture.png", new TextureSettings { Layers = TextureCount, MagFilter = TextureFilter.Linear, MinFilter = TextureFilter.Linear });

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
            ecs.Add(sun, Transform.LookingAt(new Vec3(3f, 2f, 1f), Vec3.Zero, Vec3.UnitY));
            ecs.Camera(Transform.LookingAt(new Vec3(5f, 5f, 5f), new Vec3(1.5f, 0f, 0f), Vec3.UnitY));

            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/array_texture.slang")).SetTexture("my_array_texture", arrayTexture);
            for (var x = -5; x <= 5; x++)
            {
                var entity = ecs.Mesh(cube, material, Transform.At(x + 0.5f, 0f, 0f));

                // Bevy's `x as u32 % 4`, which for a negative x wraps through the top of the
                // unsigned range first.
                ecs.Insert<MeshTagRef>(entity).Value = unchecked((uint)x) % TextureCount;
                Cubes.Add(entity);
            }
        }, "array_texture.Setup");

        app.Update(ctx =>
        {
            _timer += ctx.Time.Delta;
            if (_timer < 1.5f) return;
            _timer -= 1.5f;
            foreach (var entity in Cubes)
            {
                var tag = ctx.Ecs.Wrap<MeshTagRef>(entity);
                tag.Value = (tag.Value + 1) % TextureCount;
            }
        }, "array_texture.UpdateMeshTags");
    }
}

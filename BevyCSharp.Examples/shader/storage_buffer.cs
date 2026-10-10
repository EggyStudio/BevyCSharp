// Bevy's storage_buffer example, examples/shader/storage_buffer.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Shading;

// A grid of cubes sharing one material, each colored by the entry of a storage buffer of five
// colors that its MeshTag names, the buffer written anew every frame so the colors cycle.
internal static class StorageBuffer
{
    private static AssetHandle _colors;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Vector4[] colorData =
            [
                new(1f, 0f, 0f, 1f),
                new(0f, 1f, 0f, 1f),
                new(0f, 0f, 1f, 1f),
                new(1f, 1f, 0f, 1f),
                new(0f, 1f, 1f, 1f),
            ];
            _colors = Shaders.CreateBuffer<Vector4>(colorData);

            var program = Shaders.CreateProgram(new ShaderProgramSettings { Vertex = "shaders/storage_buffer.slang", Fragment = "shaders/storage_buffer.slang" });
            var material = Shaders.CreateMaterial(program).SetBuffer("colors", _colors);
            var cube = Render.CreateMesh(MeshShape.Cuboid, 0.3f, 0.3f, 0.3f);

            var currentColorId = 0u;
            for (var i = -6; i <= 6; i++)
            {
                for (var j = -3; j <= 3; j++)
                {
                    var entity = ecs.SpawnMesh(cube, material, Transform.At(i, j, 0f));
                    ecs.Insert<MeshTagRef>(entity).Value = currentColorId % 5;
                    currentColorId++;
                }
            }

            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 10f), Vec3.Zero, Vec3.UnitY));
        }, "storage_buffer.Setup");

        app.Update(ctx =>
        {
            var t = ctx.Time.Elapsed * 5f;
            Span<Vector4> colors = stackalloc Vector4[5];
            for (var i = 0; i < colors.Length; i++)
                colors[i] = new Vector4(MathF.Sin(t + i) / 2f + 0.5f, MathF.Sin(t + i + 2f) / 2f + 0.5f, MathF.Sin(t + i + 4f) / 2f + 0.5f, 1f);
            Shaders.WriteBuffer<Vector4>(_colors, colors);
        }, "storage_buffer.Update");
    }
}

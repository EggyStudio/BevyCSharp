// Bevy's automatic_instancing example, examples/shader/automatic_instancing.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Shading;

// A picture of 256 by 256 drawn as as many tiny cubes, one for each pixel, sharing one mesh and one
// material so Bevy draws them together. Each cube's MeshTag counts which pixel it is, the shader
// reads that pixel's color, and every cube sways toward and away from the camera on its own phase.
internal static class AutomaticInstancing
{
    private const int ImageSize = 256;

    // The pixel each cube is, by its entity, since the transforms are walked as the world holds
    // them rather than in the order they were spawned.
    private static readonly Dictionary<Entity, int> Index = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Index.Clear();
            var program = Shaders.CreateProgram(new ShaderProgramSettings { Vertex = "shaders/automatic_instancing.slang", Fragment = "shaders/automatic_instancing.slang" });
            var material = Shaders.CreateMaterial(program).SetTexture("texture", AssetServer.Load(AssetKind.Image, "branding/icon.png"));
            var cube = Render.CreateMesh(MeshShape.Cuboid, 0.01f, 0.01f, 0.01f);

            for (var index = 0; index < ImageSize * ImageSize; index++)
            {
                var (x, y) = (index % ImageSize, index / ImageSize);
                var entity = ecs.SpawnMesh(cube, material, Transform.At((x - ImageSize / 2f) / 50f, -((y - ImageSize / 2f) / 50f), 0f));
                ecs.Insert<MeshTagRef>(entity).Value = (uint)index;
                Index[entity] = index;
            }

            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "automatic_instancing.Setup");

        app.Update(ctx =>
        {
            var elapsed = ctx.Time.Elapsed;
            foreach (var row in ctx.Ecs.Query<Transform>())
            {
                if (Index.TryGetValue(row.Entity, out var index))
                    row.Component.Translation.Z = MathF.Sin(elapsed + index * 0.01f);
            }
        }, "automatic_instancing.Update");
    }
}

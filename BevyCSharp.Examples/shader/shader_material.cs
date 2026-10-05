// Bevy's shader_material example, examples/shader/shader_material.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// A material of a shader's own, a cube drawn blue through the Bevy icon, half see-through, its
// shader importing the value that halves the alpha from a module beside it.
internal static class ShaderMaterialExample
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/custom_material.slang"), AlphaMode.Blend)
                .Set("material_color", new Vector4(0f, 0f, 1f, 1f))
                .SetTexture("material_color_texture", AssetServer.Load(AssetKind.Image, "branding/icon.png"));
            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), material, Transform.At(0f, 0.5f, 0f));
            ecs.Camera(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "shader_material.Setup");
    }
}

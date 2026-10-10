// Bevy's shader_material_2d example, examples/shader/shader_material_2d.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// A shader and a material that uses it on a 2D mesh, a square drawn blue through the Bevy icon, its
// pixels below half alpha discarded, the shader importing the value that halves the alpha from a
// module beside it.
internal static class ShaderMaterial2dExample
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render2d.SpawnCamera2d();

            var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/custom_material_2d.slang" });
            var material = Shaders.CreateMaterial2d(program, AlphaMode2d.Mask, cutoff: 0.5f)
                .Set("material_color", new Vector4(0f, 0f, 1f, 1f))
                .SetTexture("base_color_texture", AssetServer.Load(AssetKind.Image, "branding/icon.png"));

            var quad = ecs.Spawn();
            ecs.Add(quad, Transform.Identity with { Scale = new Vec3(128f) });
            Render2d.SetMesh(ecs, quad, Render.CreateMesh(MeshShape.Rectangle, 1f, 1f));
            Render2d.SetMaterial(ecs, quad, material);
        }, "shader_material_2d.Setup");
    }
}

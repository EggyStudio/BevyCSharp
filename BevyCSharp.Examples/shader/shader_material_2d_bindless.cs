// Bevy's shader_material_2d_bindless example, examples/shader/shader_material_2d_bindless.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// Four quads of one shader, each a color times one of the two Bevy logos. Bevy's material is
// bindless, its materials sharing one bind group of arrays where the GPU allows it, and here each
// material binds its own, as Bevy's does on a GPU without bindless support, which draws the same
// picture.
internal static class ShaderMaterial2dBindless
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/custom_material_2d_bindless.slang" });
        var dark = AssetServer.Load(AssetKind.Image, "branding/bevy_logo_dark.png");
        var light = AssetServer.Load(AssetKind.Image, "branding/bevy_logo_light.png");
        var mesh = Render.CreateMesh(MeshShape.Rectangle, 1f, 1f);

        (Vector2 Offset, Vector4 Color, AssetHandle Image)[] quads =
        [
            (new Vector2(-1f, -1f), new Vector4(1f, 0f, 0f, 1f), dark),
            (new Vector2(1f, -1f), new Vector4(1f, 1f, 0f, 1f), light),
            (new Vector2(-1f, 1f), new Vector4(0f, 1f, 0f, 1f), dark),
            (new Vector2(1f, 1f), new Vector4(0f, 0f, 1f, 1f), light),
        ];

        foreach (var (offset, color, image) in quads)
        {
            var quad = ecs.Spawn();
            ecs.Add(quad, Transform.At(offset.X * 128f, offset.Y * 128f, 0f) with { Scale = new Vec3(128f, 32f, 1f) });
            Render2d.SetMesh(ecs, quad, mesh);
            Render2d.SetMaterial(ecs, quad, Shaders.CreateMaterial2d(program, AlphaMode2d.Mask, cutoff: 0.5f)
                .Set("material_color.base_color", color)
                .SetTexture("material_color_texture", image));
        }
    }, "shader_material_2d_bindless.Setup");
}

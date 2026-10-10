// Bevy's shader_material_bindless example, examples/shader/shader_material_bindless.rs at v0.20.0,
// by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// Two materials of one shader, a blue cube with the dark Bevy logo and a red cylinder with the
// light one. Bevy's material is bindless, its materials sharing one bind group of arrays where the
// GPU allows it, and here each material binds its own, as Bevy's does on a GPU without bindless
// support, which draws the same picture.
internal static class ShaderMaterialBindless
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var program = Shaders.CreateProgram("shaders/bindless_material.slang");

            ShaderMaterial Material(Vector4 color, string texture) => Shaders.CreateMaterial(program)
                .Set("material_color.base_color", color)
                .SetTexture("material_color_texture", AssetServer.Load(AssetKind.Image, texture));

            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Material(new Vector4(0f, 0f, 1f, 1f), "branding/bevy_logo_dark.png"), Transform.At(-2f, 0.5f, 0f));
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cylinder, 0.5f, 1f), Material(new Vector4(1f, 0f, 0f, 1f), "branding/bevy_logo_light.png"), Transform.At(2f, 0.5f, 0f));
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "shader_material_bindless.Setup");
    }
}

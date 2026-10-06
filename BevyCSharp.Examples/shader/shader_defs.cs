// Bevy's shader_defs example, examples/shader/shader_defs.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// One shader compiled two ways by a define, IS_RED, which makes one cube red whatever color its
// material holds. Bevy's material sets the define from a field of itself when its pipeline is
// specialized, and here a program is compiled with it and one without, each the material of one
// cube, which comes to the same two pipelines.
internal static class ShaderDefs
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);

            ShaderMaterial Material(Vector4 color, bool isRed)
            {
                var settings = new ShaderProgramSettings { Fragment = "shaders/shader_defs.slang" };
                if (isRed) settings.Defines["IS_RED"] = true;
                return Shaders.CreateMaterial(Shaders.CreateProgram(settings)).Set("material.color", color);
            }

            ecs.SpawnMesh(cube, Material(new Vector4(0f, 0f, 1f, 1f), isRed: false), Transform.At(-1f, 0.5f, 0f));
            ecs.SpawnMesh(cube, Material(new Vector4(0f, 1f, 0f, 1f), isRed: true), Transform.At(1f, 0.5f, 0f));
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "shader_defs.Setup");
    }
}

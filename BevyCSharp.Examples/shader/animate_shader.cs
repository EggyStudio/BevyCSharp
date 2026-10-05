// Bevy's animate_shader example, examples/shader/animate_shader.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// A shader animated by time alone, which it reads from Bevy's globals, so nothing on the C# side
// changes from frame to frame.
internal static class AnimateShader
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/animate_shader.slang"));
            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), material, Transform.At(0f, 0.5f, 0f));
            ecs.Camera(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "animate_shader.Setup");
    }
}

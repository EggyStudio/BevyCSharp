// Bevy's mesh_shader_intro example, examples/shader_advanced/mesh_shader_intro.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// Cubes written straight to the rasterizer by mesh shaders, beside a mesh cube in a light. A task
// shader runs a cube of mesh shader workgroups growing with time, each writing a cube where it is
// in the grid, colored by where that is, its triangles culled in a wave, until the count passes
// what the GPU allows and every cube disappears. A fragment shader colors them and writes its own
// depth.
//
// Bevy builds the mesh pipeline and a render system for it itself. Here the camera draws a program
// of draw task, draw mesh and draw fragment stages after its opaque geometry, counted in
// workgroups. Bevy's mesh shader writes each cube's color once a triangle, and this one once a
// corner, since Slang declares nothing a fragment shader reads per triangle, which draws the same
// cubes. Where the device has no mesh shaders the cube in the light is drawn alone.
internal static class MeshShaderIntro
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;

        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f),
            Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)),
            Transform.At(0f, 0.5f, 0f));
        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);

        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-9f, 1f, -9f), new Vec3(9f, 4f, 9f), Vec3.UnitY));
        // MSAA off, for simplicity, as Bevy's is.
        Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });

        if (!Shaders.SupportsMeshShaders)
        {
            Log.Warn("This device does not run mesh shaders, so only the cube in the light is drawn.");
            return;
        }

        var cubes = Shaders.CreateProgram(new ShaderProgramSettings
        {
            DrawTask = new ShaderStage("shaders/mesh_shader_intro.slang", "task"),
            DrawMesh = new ShaderStage("shaders/mesh_shader_intro.slang", "mesh"),
            DrawFragment = "shaders/mesh_shader_intro.slang",
        });

        // One workgroup of the task shader, which runs the mesh shader's.
        Shaders.SetViewDraws(camera, ViewDraw.Meshes(Shaders.CreateInstance(cubes), FramePoint.AfterOpaque, 1));
    }, "mesh_shader_intro.Setup");
}

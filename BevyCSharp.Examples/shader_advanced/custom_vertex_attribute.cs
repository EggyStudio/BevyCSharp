// Bevy's custom_vertex_attribute example, examples/shader_advanced/custom_vertex_attribute.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// A cube carrying a vertex stream of its own, a blend color each vertex, which a shader's own vertex
// stage reads at location 8 and hands on to its fragment stage, red times the material's white.
// Bevy's declares the attribute in Rust and places it in the material's specialize, where here the
// mesh names the stream with its location and the bridge places it.
internal static class CustomVertexAttribute
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;

        // The cube's 24 vertices, six faces of four, each with the blend color.
        Render.TryReadMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), out var cube);
        var blend = Enumerable.Repeat(new Vector4(1f, 0f, 0f, 1f), cube!.Positions.Length).ToArray();
        cube.Streams = [MeshStream.From<Vector4>("BlendColor", MeshStreamFormat.Float4, 8, blend)];

        var program = Shaders.CreateProgram(new ShaderProgramSettings
        {
            Vertex = "shaders/custom_vertex_attribute.slang",
            Fragment = "shaders/custom_vertex_attribute.slang",
        });
        var material = Shaders.CreateMaterial(program).Set("material.color", Vector4.One);

        ecs.SpawnMesh(Render.CreateMesh(cube), material, Transform.At(0f, 0.5f, 0f));
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
    }, "custom_vertex_attribute.Setup");
}

// Bevy's compute_mesh example, examples/shader_advanced/compute_mesh.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// A mesh made by a compute shader, a cube three units across drawn twice, in red and in sky blue,
// lit and casting shadows as any mesh does.
//
// Bevy's compute shader writes straight into the mesh's own vertex and index buffers, where its
// allocator keeps them. The bridge does not hand a compute shader a mesh's buffers, so here it
// writes buffers of its own, and the two cubes' material reads its vertices from them by vertex
// number, in the main pass and in the prepass that draws its shadow.
internal static class ComputeMesh
{
    // The compute shader's pipeline and the buffers it writes, Bevy's ComputePipeline resource.
    internal static ShaderInstance Pipeline;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render.SetClearColor((0f, 0f, 0f, 1f));

            var vertexData = Shaders.CreateBuffer(192 * sizeof(float));
            var indexData = Shaders.CreateBuffer(36 * sizeof(uint));
            Pipeline = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/compute_mesh.slang" }))
                .SetBuffer("vertex_data", vertexData)
                .SetBuffer("index_data", indexData);

            // Thirty-six vertices with nothing in them, one for each index the compute shader
            // writes, which the material's vertex shader fills in.
            var empty = Render.CreateMesh(new MeshData { Positions = new Vec3[36], Normals = new Vec3[36], Uvs = new float[36 * 2] });
            var program = Shaders.CreateProgram(new ShaderProgramSettings
            {
                Vertex = "shaders/compute_mesh_material.slang",
                Fragment = "shaders/compute_mesh_material.slang",
                PrepassVertex = new ShaderStage("shaders/compute_mesh_material.slang", "prepass_vertex"),
            });

            ShaderMaterial Material((float R, float G, float B, float A) color) => Shaders.CreateMaterial(program)
                .SetBuffer("vertex_data", vertexData)
                .SetBuffer("index_data", indexData)
                .Set("color", new Vector4(color.R, color.G, color.B, color.A));

            // Tailwind's red and sky, each at 400, the red one marked as the one whose mesh is made.
            ecs.Add(ecs.SpawnMesh(empty, Material(Color.FromSrgb8(248, 113, 113)), Transform.At(-2.5f, 1.5f, 0f)), new GenerateMesh());
            ecs.SpawnMesh(empty, Material(Color.FromSrgb8(56, 189, 248)), Transform.At(2.5f, 1.5f, 0f));

            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Circle, 4f), Render.CreateMaterial((1f, 1f, 1f, 1f)), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
            ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));
        }, "compute_mesh.Setup");

    }
}

/// <summary>
/// A mesh made by the compute shader, once. Bevy's holds the mesh it writes into, and here the
/// shader writes into the buffers both materials read, so this marks the entity the mesh is made
/// for and remembers it has been, as Bevy's render world keeps a set of the meshes it has made.
/// </summary>
[Behavior]
public partial struct GenerateMesh
{
    /// <summary>Whether the compute shader has run for it.</summary>
    public bool Generated;

    /// <summary>The compute shader run once, as soon as it can run, as Bevy's runs once for each mesh to make.</summary>
    [OnUpdate]
    public void Generate(BehaviorContext ctx)
    {
        if (Generated || ComputeMesh.Pipeline.Program.State != ShaderProgramState.Ready) return;
        Shaders.Dispatch(ComputeMesh.Pipeline, 1);
        Generated = true;
    }
}

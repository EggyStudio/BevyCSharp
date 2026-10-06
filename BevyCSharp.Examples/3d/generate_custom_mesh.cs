// Bevy's generate_custom_mesh example, examples/3d/generate_custom_mesh.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// This example demonstrates how to create a custom mesh, assign a custom UV mapping for a custom
// texture, and how to change the UV mapping at run-time.
//
// A mesh made here is not changed in place, so Space builds the cube again with its texture
// coordinates moved and puts it on the entity in place of the old one.
internal static class GenerateCustomMesh
{
    // The mesh's data, kept to be changed and built again, as Bevy changes its mesh asset in place.
    internal static MeshData Mesh = null!;

    public static void Build(App app) => app.Startup(ctx =>
    {
        Mesh = CreateCubeMesh();
        var cube = ctx.Ecs.SpawnMesh(
            Render.CreateMesh(Mesh),
            Render.CreateMaterial(new MaterialSettings { BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/array_texture.png") }),
            Transform.Identity);
        ctx.Ecs.Add(cube, new CustomUV());

        var view = Transform.LookingAt(new Vec3(1.8f, 1.8f, 1.8f), Vec3.Zero, Vec3.UnitY);
        ctx.Ecs.SpawnCamera3d(view);
        ctx.Ecs.Set(ctx.Ecs.SpawnPointLight(new Vec3(1.8f, 1.8f, 1.8f)), view);

        Ui.SpawnText(
            "Controls:\nSpace: Change UVs\nX/Y/Z: Rotate\nR: Reset orientation",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }, "generate_custom_mesh.Setup");

    // Four vertices to a face, each with its own normal and its own place on the texture.
    private static MeshData CreateCubeMesh() => new()
    {
        Positions =
        [
            new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f),
            new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, 0.5f), new(-0.5f, -0.5f, 0.5f),
            new(0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, -0.5f),
            new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, -0.5f),
            new(-0.5f, -0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, -0.5f, 0.5f),
            new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, -0.5f, -0.5f),
        ],
        Uvs =
        [
            0f, 0.2f, 0f, 0f, 1f, 0f, 1f, 0.2f,
            0f, 0.45f, 0f, 0.25f, 1f, 0.25f, 1f, 0.45f,
            1f, 0.45f, 0f, 0.45f, 0f, 0.2f, 1f, 0.2f,
            1f, 0.45f, 0f, 0.45f, 0f, 0.2f, 1f, 0.2f,
            0f, 0.45f, 0f, 0.2f, 1f, 0.2f, 1f, 0.45f,
            0f, 0.45f, 0f, 0.2f, 1f, 0.2f, 1f, 0.45f,
        ],
        Normals =
        [
            .. Enumerable.Repeat(Vec3.UnitY, 4), .. Enumerable.Repeat(-Vec3.UnitY, 4),
            .. Enumerable.Repeat(Vec3.UnitX, 4), .. Enumerable.Repeat(-Vec3.UnitX, 4),
            .. Enumerable.Repeat(Vec3.UnitZ, 4), .. Enumerable.Repeat(-Vec3.UnitZ, 4),
        ],
        Indices =
        [
            0, 3, 1, 1, 3, 2,
            4, 5, 7, 5, 6, 7,
            8, 11, 9, 9, 11, 10,
            12, 13, 15, 13, 14, 15,
            16, 19, 17, 17, 19, 18,
            20, 21, 23, 21, 22, 23,
        ],
    };

    // Each coordinate's V moved half the texture down, or back up where that would leave it.
    internal static void ToggleTexture(MeshData mesh)
    {
        for (var i = 1; i < mesh.Uvs!.Length; i += 2)
            mesh.Uvs[i] = mesh.Uvs[i] + 0.5f < 1f ? mesh.Uvs[i] + 0.5f : mesh.Uvs[i] - 0.5f;
    }
}

/// <summary>The cube whose texture coordinates are made here and moved by Space.</summary>
[Behavior]
public partial struct CustomUV
{
    /// <summary>
    /// Space moves the texture coordinates and builds the cube again, the held X, Y and Z keys turn
    /// it about the world's axes, and R faces it forward again.
    /// </summary>
    [OnUpdate]
    public void InputHandler(BehaviorContext ctx, ref Transform transform)
    {
        var input = ctx.Input;
        if (input.KeyPressed(Key.Space))
        {
            GenerateCustomMesh.ToggleTexture(GenerateCustomMesh.Mesh);
            Render.SetMesh(ctx.Ecs, ctx.Entity, Render.CreateMesh(GenerateCustomMesh.Mesh));
        }

        var turn = ctx.Time.Delta / 1.2f;
        if (input.KeyDown(Key.X)) transform.Rotation = Quat.FromRotationX(turn) * transform.Rotation;
        if (input.KeyDown(Key.Y)) transform.Rotation = Quat.FromRotationY(turn) * transform.Rotation;
        if (input.KeyDown(Key.Z)) transform.Rotation = Quat.FromRotationZ(turn) * transform.Rotation;
        if (input.KeyDown(Key.R)) transform.Rotation = Quat.Identity;
    }
}

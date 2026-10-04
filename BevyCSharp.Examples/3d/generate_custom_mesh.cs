using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// This example demonstrates how to create a custom mesh, assign a custom UV mapping for a custom
// texture, and how to change the UV mapping at run-time.
//
// A mesh made here is not changed in place, so Space builds the cube again with its texture
// coordinates moved and puts it on the entity in place of the old one.
internal static class GenerateCustomMesh
{
    private static Entity _cube;
    private static MeshData _mesh = null!;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            _mesh = CreateCubeMesh();
            _cube = ctx.Ecs.Mesh(
                Render.CreateMesh(_mesh),
                Render.CreateMaterial(new MaterialSettings { BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/array_texture.png") }),
                Transform.Identity);

            var view = Transform.LookingAt(new Vec3(1.8f, 1.8f, 1.8f), Vec3.Zero, Vec3.UnitY);
            ctx.Ecs.Camera(view);
            ctx.Ecs.Set(ctx.Ecs.PointLight(new Vec3(1.8f, 1.8f, 1.8f)), view);

            Ui.SpawnText(
                "Controls:\nSpace: Change UVs\nX/Y/Z: Rotate\nR: Reset orientation",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.Update(ctx =>
        {
            var input = ctx.Input;
            if (input.KeyPressed(Key.Space))
            {
                ToggleTexture(_mesh);
                Render.SetMesh(ctx.Ecs, _cube, Render.CreateMesh(_mesh));
            }

            var transform = ctx.Ecs.GetOrDefault<Transform>(_cube);
            var turn = ctx.Time.Delta / 1.2f;
            if (input.KeyDown(Key.X)) transform.Rotation = Quat.FromRotationX(turn) * transform.Rotation;
            if (input.KeyDown(Key.Y)) transform.Rotation = Quat.FromRotationY(turn) * transform.Rotation;
            if (input.KeyDown(Key.Z)) transform.Rotation = Quat.FromRotationZ(turn) * transform.Rotation;
            if (input.KeyDown(Key.R)) transform.Rotation = Quat.Identity;
            ctx.Ecs.Set(_cube, transform);
        }, "generate_custom_mesh.InputHandler");
    }

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
    private static void ToggleTexture(MeshData mesh)
    {
        for (var i = 1; i < mesh.Uvs!.Length; i += 2)
            mesh.Uvs[i] = mesh.Uvs[i] + 0.5f < 1f ? mesh.Uvs[i] + 0.5f : mesh.Uvs[i] - 0.5f;
    }
}

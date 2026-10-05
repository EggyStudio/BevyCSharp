using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows the two ways to change what an entity's mesh looks like: Space points the right shape at
// a different mesh, swapping cube and sphere, and Return changes the left shape's mesh itself,
// doubling and halving it, which every entity drawing that mesh would show.
internal static class AlterMesh
{
    private static readonly string[] Shapes = ["models/cube/cube.gltf", "models/sphere/sphere.gltf"];

    private static Entity _left, _right;
    private static int _rightShape;
    private static bool _scaled;

    public static void Build(App app)
    {
        app.Startup(Setup, "alter_mesh.Setup");
        app.Update(AlterHandle, "alter_mesh.AlterHandle");
        app.Update(AlterMeshItself, "alter_mesh.AlterMesh");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_rightShape, _scaled) = (0, false);

        var material = Scene.Material(Scene.Srgb(0.6f, 0.8f, 0.6f));
        _left = ecs.Mesh(AssetServer.LoadGltfMesh(Shapes[0]), material, Transform.At(-3f, 0f, 0f));
        _right = ecs.Mesh(AssetServer.LoadGltfMesh(Shapes[0]), material, Transform.At(3f, 0f, 0f));
        ecs.SetName(_left, "Left Shape");
        ecs.SetName(_right, "Right Shape");

        ecs.PointLight(new Vec3(4f, 5f, 4f));
        ecs.Camera(Transform.LookingAt(new Vec3(0f, 3f, 20f), Vec3.Zero, Vec3.UnitY));

        Ui.SpawnText("Space: swap meshes by mutating a Handle<Mesh>\nReturn: mutate the mesh itself, changing all copies of it",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static void AlterHandle(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;
        _rightShape = 1 - _rightShape;
        Render.SetMesh(ctx.Ecs, _right, AssetServer.LoadGltfMesh(Shapes[_rightShape]));
    }

    // Read, every vertex moved, and written back over the same mesh, with the normals it had.
    private static void AlterMeshItself(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Enter)) return;

        var mesh = Render.MeshOf(ctx.Ecs, _left);
        if (!Render.TryReadMesh(mesh, out var triangles) || !Render.TryReadNormals(mesh, out _, out var normals)) return;

        var scale = _scaled ? 0.5f : 2f;
        Render.WriteMesh(mesh, new MeshData
        {
            Positions = [.. triangles!.Positions.Select(position => position * scale)],
            Normals = normals.Length == triangles.Positions.Length ? normals : null,
            Indices = triangles.Indices,
        });
        _scaled = !_scaled;
    }
}

// Bevy's alter_mesh example, examples/asset/alter_mesh.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows the two ways to change what an entity's mesh looks like: Space points the right shape at
// a different mesh, swapping cube and sphere, and Return changes the left shape's mesh itself,
// doubling and halving it, which every entity drawing that mesh would show.
internal static class AlterMesh
{
    // Whether the left shape's mesh is doubled, which Bevy's alter_mesh keeps in a Local.
    internal static bool Scaled;

    public static void Build(App app) => app.Startup(Setup, "alter_mesh.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Scaled = false;

        // Both carry which shape they show, and the left one is marked so the two can be told
        // apart, as Bevy's are.
        var material = Render.CreateMaterial(Color.FromSrgb(0.6f, 0.8f, 0.6f));
        var left = ecs.SpawnMesh(AssetServer.LoadGltfMesh(Shape.MeshPath(ShapeKind.Cube)), material, Transform.At(-3f, 0f, 0f));
        var right = ecs.SpawnMesh(AssetServer.LoadGltfMesh(Shape.MeshPath(ShapeKind.Cube)), material, Transform.At(3f, 0f, 0f));
        ecs.SetName(left, "Left Shape");
        ecs.SetName(right, "Right Shape");
        ecs.Add(left, new Shape());
        ecs.Add(right, new Shape());
        ecs.Add(left, new LeftShape());

        ecs.SpawnPointLight(new Vec3(4f, 5f, 4f));
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 3f, 20f), Vec3.Zero, Vec3.UnitY));

        Ui.SpawnText("Space: swap meshes by mutating a Handle<Mesh>\nReturn: mutate the mesh itself, changing all copies of it",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }
}

/// <summary>Which of the two meshes a shape shows.</summary>
public enum ShapeKind
{
    /// <summary>A cube.</summary>
    Cube,

    /// <summary>A sphere.</summary>
    Sphere,
}

/// <summary>An entity drawn with one of the shapes, and which one, as Bevy's <c>Shape</c> keeps it.</summary>
[Behavior]
public partial struct Shape
{
    /// <summary>Which mesh it shows.</summary>
    public ShapeKind Kind;

    /// <summary>The file a kind of shape is drawn from.</summary>
    public static string MeshPath(ShapeKind kind) =>
        kind == ShapeKind.Cube ? "models/cube/cube.gltf" : "models/sphere/sphere.gltf";

    /// <summary>Space points the right shape at the other mesh, as Bevy's <c>alter_handle</c> does.</summary>
    [OnUpdate]
    [Without(typeof(LeftShape))]
    public void AlterHandle(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;
        Kind = Kind == ShapeKind.Cube ? ShapeKind.Sphere : ShapeKind.Cube;
        Render.SetMesh(ctx.Ecs, ctx.Entity, AssetServer.LoadGltfMesh(MeshPath(Kind)));
    }
}

/// <summary>Bevy's <c>Left</c>, the shape on the left, whose mesh itself is changed.</summary>
[Behavior]
public partial struct LeftShape
{
    /// <summary>
    /// Return reads the left shape's mesh, moves every vertex, and writes it back over the same
    /// mesh with the normals it had, doubling and then halving it, as Bevy's <c>alter_mesh</c> does.
    /// </summary>
    [OnUpdate]
    public void AlterMeshItself(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Enter)) return;

        var mesh = Render.MeshOf(ctx.Ecs, ctx.Entity);
        if (!Render.TryReadMesh(mesh, out var triangles) || !Render.TryReadNormals(mesh, out _, out var normals)) return;

        var scale = AlterMesh.Scaled ? 0.5f : 2f;
        Render.WriteMesh(mesh, new MeshData
        {
            Positions = [.. triangles!.Positions.Select(position => position * scale)],
            Normals = normals.Length == triangles.Positions.Length ? normals : null,
            Indices = triangles.Indices,
        });
        AlterMesh.Scaled = !AlterMesh.Scaled;
    }
}

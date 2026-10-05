using System.Text.Json;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Usage;

// Shows what frustum culling keeps. A small camera in the corner, its frustum drawn, looks out from
// the middle of a ring of turning shapes, and each shape's bounding box is green while that camera
// sees it and red while it does not, a torus by a wall rising in and out of view. The big camera
// flies with WASD and the mouse, 1 puts it where the small one is, and 2 back where it began.
//
// Bevy colors a box by whether the shape is in the small camera's list of visible entities, which
// no wrapper reads. Here each shape's box is tested against that camera's frustum, read through
// Bevy's reflected Frustum, as Bevy's own culling tests it.
internal static class DebugFrustumCulling
{
    private const float ShapeRingRadius = 10f, WallShapeTimerSeconds = 8f;
    private static readonly Vec3 FreeCameraStart = new(-20f, 10f, 22f), FreeCameraTarget = new(7f, 1.5f, 0f);

    private static Entity _freeCamera, _myCamera, _ring, _wall;
    private static readonly List<Entity> Shapes = [];
    private static float _timer;

    public static void Build(App app)
    {
        app.Startup(Setup, "debug_frustum_culling.Setup");
        app.Update(MoveShapes, "debug_frustum_culling.MoveShapes");
        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.Digit1)) ctx.Ecs.Set(_freeCamera, ctx.Ecs.GetOrDefault<Transform>(_myCamera));
            if (ctx.Input.KeyPressed(Key.Digit2)) ctx.Ecs.Set(_freeCamera, Transform.LookingAt(FreeCameraStart, FreeCameraTarget, Vec3.UnitY));
        }, "debug_frustum_culling.MoveFreeCamera");
        app.Update(UpdateShapeAabbColors, "debug_frustum_culling.UpdateShapeAabbColors");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Shapes.Clear();
        _timer = 0f;
        if (Window.Entity() != Entity.None) Window.SetStyle(resizable: false);

        _freeCamera = ecs.Camera(Transform.LookingAt(FreeCameraStart, FreeCameraTarget, Vec3.UnitY));
        ecs.Add(_freeCamera, new FreeCamera());

        // A third of the window, in its bottom right corner, drawn over the big camera's picture.
        var (width, height) = Scene.Size;
        _myCamera = ecs.Camera(Transform.LookingAt(new Vec3(0f, 1.5f, 0f), new Vec3(1f, 1.5f, 0f), Vec3.UnitY), new CameraSettings
        {
            Order = 1,
            Viewport = (width * 2 / 3, height * 2 / 3, width / 3, height / 3),
            Clear = ClearMode.World,
        });
        ecs.Insert<ShowFrustumGizmoRef>(_myCamera);

        var freeView = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Camera = _freeCamera });
        ecs.SetParent(Ui.SpawnText(
            "This example utilizes free camera controls i.e. move with WASD and mouse grab to change orientation.\n"
            + "Press '1' to move the free camera to where MyCamera is, matching its view frustum.\n"
            + "Press '2' to move the free camera to its initial position in the example.",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }), freeView);
        var myView = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Camera = _myCamera });
        ecs.SetParent(Ui.SpawnText("View of MyCamera", new UiSettings { Absolute = true, Bottom = Length.Px(12f), Right = Length.Px(100f) }), myView);

        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, ShapeRingRadius * 4f, ShapeRingRadius * 4f), Scene.Material(Scene.Srgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Scene.Material(Scene.Srgb(0.3f, 0.3f, 0.5f)), new Transform(new Vec3(20f, 2.5f, 10f), Quat.FromRotationZ(MathF.PI / 2f), Vec3.One));
        ecs.PointLight(new Vec3(0f, 10f, 0f), shadows: true);

        // Boxes red unless the small camera sees them.
        Gizmos.ShowBounds(all: false, color: (1f, 0f, 0f, 1f));

        var white = Scene.Material((1f, 1f, 1f, 1f));
        AssetHandle[] meshes =
        [
            Render.CreateMesh(MeshShape.Cuboid, 4f, 1f, 2f),
            Render.CreateMesh(Tetrahedron(new Vec3(3f, 4f, 3f), new Vec3(-0.5f, 4f, -0.5f), new Vec3(-0.5f, -0.5f, 3f), new Vec3(3f, -0.5f, -0.5f))),
            Render.CreateMesh(MeshShape.Cylinder, 0.1f, 3f),
            Render.CreateMesh(MeshShape.Cuboid, 2f, 0.2f, 4f),
            Render.CreateMesh(MeshShape.Sphere, 0.5f),
        ];

        _ring = ecs.Spawn();
        ecs.Add(_ring, Transform.Identity);
        ecs.Insert<VisibilityRef>(_ring);
        for (var i = 0; i < meshes.Length; i++)
        {
            var angle = i * 2f * MathF.PI / meshes.Length;
            var shape = ecs.Mesh(meshes[i], white, new Transform(new Vec3(ShapeRingRadius * MathF.Cos(angle), 1.5f, ShapeRingRadius * MathF.Sin(angle)), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
            ecs.SetParent(shape, _ring);
            ecs.Insert<ShowAabbGizmoRef>(shape);
            Shapes.Add(shape);
        }

        _wall = ecs.Mesh(Render.CreateMesh(MeshShape.Torus, 0.5f, 1.5f), white, new Transform(new Vec3(25f, 1.5f, 12.5f), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
        ecs.Insert<ShowAabbGizmoRef>(_wall);
        Shapes.Add(_wall);
    }

    // Bevy's tetrahedron of four corners given, each face flat.
    private static MeshData Tetrahedron(Vec3 a, Vec3 b, Vec3 c, Vec3 d)
    {
        var faces = new[] { (a, b, c), (a, c, d), (a, d, b), (b, d, c) };
        var middle = (a + b + c + d) * 0.25f;
        var positions = new List<Vec3>();
        var normals = new List<Vec3>();
        foreach (var (p, q, r) in faces)
        {
            var normal = Vec3.Cross(q - p, r - p).Normalized;

            // Facing away from the middle, whichever way round the corners were given.
            var (first, second, third) = Vec3.Dot(normal, p - middle) < 0f ? (p, r, q) : (p, q, r);
            normal = Vec3.Cross(second - first, third - first).Normalized;
            positions.AddRange([first, second, third]);
            normals.AddRange([normal, normal, normal]);
        }

        return new MeshData { Positions = [.. positions], Normals = [.. normals], Uvs = new float[positions.Count * 2] };
    }

    // Every shape turns, the torus by the wall rises for four seconds and falls for four, and the
    // whole ring turns slower about the middle.
    private static void MoveShapes(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var dt = ctx.Time.Delta;
        _timer = (_timer + dt) % WallShapeTimerSeconds;

        foreach (var shape in Shapes)
        {
            var at = ecs.GetOrDefault<Transform>(shape);
            at.Rotation = Quat.FromRotationY(dt / 2f) * at.Rotation;
            if (shape == _wall)
            {
                at.Translation.Y = _timer < WallShapeTimerSeconds / 2f
                    ? 1.5f + 15f * _timer / (WallShapeTimerSeconds / 2f)
                    : 1.5f + 15f * (WallShapeTimerSeconds - _timer) / (WallShapeTimerSeconds / 2f);
            }

            ecs.Set(shape, at);
        }

        var ring = ecs.GetOrDefault<Transform>(_ring);
        ecs.Set(_ring, ring with { Rotation = Quat.FromRotationY(dt / 3f) * ring.Rotation });
    }

    // A shape the small camera sees has its box drawn green, and one it does not has the default
    // color, red. A box is outside the frustum when it lies wholly behind one of its six planes,
    // measured as far as the box reaches toward that plane.
    private static void UpdateShapeAabbColors(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (ecs.GetReflected(_myCamera, FrustumRef.TypePath) is not { } json) return;
        var planes = Planes(json);
        if (planes.Count == 0) return;

        foreach (var shape in Shapes)
        {
            if (ecs.Get<AabbRef>(shape) is not { } aabb) continue;
            var world = ecs.GetOrDefault<GlobalTransform>(shape);
            var center = world.TransformPoint(aabb.Center);
            var half = aabb.HalfExtents;

            var visible = true;
            foreach (var (normal, d) in planes)
            {
                var reach = MathF.Abs(Vec3.Dot(normal, world.XAxis)) * half.X
                    + MathF.Abs(Vec3.Dot(normal, world.YAxis)) * half.Y
                    + MathF.Abs(Vec3.Dot(normal, world.ZAxis)) * half.Z;
                if (Vec3.Dot(normal, center) + d + reach <= 0f)
                {
                    visible = false;
                    break;
                }
            }

            ecs.Wrap<ShowAabbGizmoRef>(shape).Color = visible ? new Color(0f, 1f, 0f, 1f) : null;
        }
    }

    // The frustum's six planes, each a normal pointing inward and its distance, from Bevy's
    // Frustum as reflection writes it, a list of half spaces each holding the four numbers.
    private static List<(Vec3 Normal, float D)> Planes(string json)
    {
        var planes = new List<(Vec3, float)>();
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("half_spaces", out var halfSpaces)) return planes;
        foreach (var space in halfSpaces.EnumerateArray())
        {
            var numbers = space.TryGetProperty("normal_d", out var normalD) ? normalD : space;
            var n = numbers.EnumerateArray().Select(value => value.GetSingle()).ToArray();
            if (n.Length == 4) planes.Add((new Vec3(n[0], n[1], n[2]), n[3]));
        }

        return planes;
    }
}

// Bevy's debug_frustum_culling example, examples/usage/debug_frustum_culling.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

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
    private const float ShapeRingRadius = 10f;
    internal const float WallShapeTimerSeconds = 8f;
    private static readonly Vec3 FreeCameraStart = new(-20f, 10f, 22f), FreeCameraTarget = new(7f, 1.5f, 0f);

    // The big camera, which Bevy finds as the 3D camera that is not MyCamera, and the timer the
    // torus by the wall rises and falls by, which Bevy keeps in a Local of move_shapes.
    private static Entity _freeCamera;
    internal static GameTimer WallTimer;

    public static void Build(App app)
    {
        app.Startup(Setup, "debug_frustum_culling.Setup");
        app.Update(ctx =>
        {
            WallTimer.Tick(ctx.Time.Delta);
            foreach (var camera in ctx.Ecs.Query<MyCamera>(markChanged: false))
            {
                if (ctx.Input.KeyPressed(Key.Digit1)) ctx.Ecs.Set(_freeCamera, ctx.Ecs.GetOrDefault<Transform>(camera.Entity));
                if (ctx.Input.KeyPressed(Key.Digit2)) ctx.Ecs.Set(_freeCamera, Transform.LookingAt(FreeCameraStart, FreeCameraTarget, Vec3.UnitY));
            }
        }, "debug_frustum_culling.MoveFreeCamera");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        WallTimer = GameTimer.FromSeconds(WallShapeTimerSeconds, TimerMode.Repeating);
        if (Window.Entity() != Entity.None) Window.SetStyle(resizable: false);

        _freeCamera = ecs.SpawnCamera3d(Transform.LookingAt(FreeCameraStart, FreeCameraTarget, Vec3.UnitY));
        ecs.Add(_freeCamera, new FreeCamera());

        // A third of the window, in its bottom right corner, drawn over the big camera's picture.
        var (width, height) = Window.Size();
        var myCamera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 1.5f, 0f), new Vec3(1f, 1.5f, 0f), Vec3.UnitY), new CameraSettings
        {
            Order = 1,
            Viewport = (width * 2 / 3, height * 2 / 3, width / 3, height / 3),
            Clear = ClearMode.World,
        });
        ecs.Insert<ShowFrustumGizmoRef>(myCamera);
        ecs.Add(myCamera, new MyCamera());

        var freeView = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Camera = _freeCamera });
        ecs.SetParent(Ui.SpawnText(
            "This example utilizes free camera controls i.e. move with WASD and mouse grab to change orientation.\n"
            + "Press '1' to move the free camera to where MyCamera is, matching its view frustum.\n"
            + "Press '2' to move the free camera to its initial position in the example.",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }), freeView);
        var myView = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Camera = myCamera });
        ecs.SetParent(Ui.SpawnText("View of MyCamera", new UiSettings { Absolute = true, Bottom = Length.Px(12f), Right = Length.Px(100f) }), myView);

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, ShapeRingRadius * 4f, ShapeRingRadius * 4f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.3f, 0.5f)), new Transform(new Vec3(20f, 2.5f, 10f), Quat.FromRotationZ(MathF.PI / 2f), Vec3.One));
        ecs.SpawnPointLight(new Vec3(0f, 10f, 0f), shadows: true);

        // Boxes red unless the small camera sees them.
        Gizmos.ShowBounds(all: false, color: (1f, 0f, 0f, 1f));

        var white = Render.CreateMaterial((1f, 1f, 1f, 1f));
        AssetHandle[] meshes =
        [
            Render.CreateMesh(MeshShape.Cuboid, 4f, 1f, 2f),
            Render.CreateMesh(Tetrahedron(new Vec3(3f, 4f, 3f), new Vec3(-0.5f, 4f, -0.5f), new Vec3(-0.5f, -0.5f, 3f), new Vec3(3f, -0.5f, -0.5f))),
            Render.CreateMesh(MeshShape.Cylinder, 0.1f, 3f),
            Render.CreateMesh(MeshShape.Cuboid, 2f, 0.2f, 4f),
            Render.CreateMesh(MeshShape.Sphere, 0.5f),
        ];

        var ring = ecs.Spawn();
        ecs.Add(ring, Transform.Identity);
        ecs.Insert<VisibilityRef>(ring);
        ecs.Add(ring, new ShapeRing());
        for (var i = 0; i < meshes.Length; i++)
        {
            var angle = i * 2f * MathF.PI / meshes.Length;
            var shape = ecs.SpawnMesh(meshes[i], white, new Transform(new Vec3(ShapeRingRadius * MathF.Cos(angle), 1.5f, ShapeRingRadius * MathF.Sin(angle)), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
            ecs.SetParent(shape, ring);
            ecs.Insert<ShowAabbGizmoRef>(shape);
            ecs.Add(shape, new MyShape());
        }

        var wall = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Torus, 0.5f, 1.5f), white, new Transform(new Vec3(25f, 1.5f, 12.5f), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
        ecs.Insert<ShowAabbGizmoRef>(wall);
        ecs.Add(wall, new MyShape());
        ecs.Add(wall, new WallShape());
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

    // The frustum's six planes, each a normal pointing inward and its distance, from Bevy's
    // Frustum as reflection writes it, a list of half spaces each holding the four numbers.
    internal static List<(Vec3 Normal, float D)> Planes(string json)
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

/// <summary>The ring the shapes stand in, turning slower about the middle than they turn.</summary>
[Behavior]
public partial struct ShapeRing
{
    /// <summary>Turned a third of a radian a second.</summary>
    [OnUpdate]
    public void MoveRing(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationY(ctx.Time.Delta / 3f) * transform.Rotation;
}

/// <summary>A shape whose bounding box is drawn green while the small camera sees it and red while it does not.</summary>
[Behavior]
public partial struct MyShape
{
    // The small camera's frustum, read once a frame for every shape.
    private static List<(Vec3 Normal, float D)> _planes = [];
    private static ulong _planesFrame = ulong.MaxValue;

    /// <summary>
    /// Turned half a radian a second, and the torus by the wall raised for four seconds and lowered
    /// for four, as Bevy's <c>move_shapes</c> moves each.
    /// </summary>
    [OnUpdate]
    public void MoveShapes(BehaviorContext ctx, ref Transform transform)
    {
        transform.Rotation = Quat.FromRotationY(ctx.Time.Delta / 2f) * transform.Rotation;
        if (!ctx.Ecs.Has<WallShape>(ctx.Entity)) return;

        const float Half = DebugFrustumCulling.WallShapeTimerSeconds / 2f;
        var elapsed = DebugFrustumCulling.WallTimer.Elapsed;
        transform.Translation.Y = elapsed < Half
            ? 1.5f + 15f * elapsed / Half
            : 1.5f + 15f * (DebugFrustumCulling.WallShapeTimerSeconds - elapsed) / Half;
    }

    /// <summary>
    /// Its box green while the small camera sees it, and the default red while it does not. Bevy
    /// reads the camera's list of what it sees, which no wrapper reads, and here the box is tested
    /// against that camera's frustum, read through Bevy's reflected Frustum as Bevy's own culling
    /// tests it, outside when it lies wholly behind one of the six planes, measured as far as the
    /// box reaches toward that plane.
    /// </summary>
    [OnUpdate]
    public void UpdateShapeAabbColors(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (_planesFrame != ctx.Time.FrameCount)
        {
            _planesFrame = ctx.Time.FrameCount;
            _planes = [];
            foreach (var camera in ecs.Query<MyCamera>(markChanged: false))
                if (ecs.GetReflected(camera.Entity, FrustumRef.TypePath) is { } json) _planes = DebugFrustumCulling.Planes(json);
        }

        if (_planes.Count == 0 || ecs.Get<AabbRef>(ctx.Entity) is not { } aabb) return;
        var world = ecs.GetOrDefault<GlobalTransform>(ctx.Entity);
        var center = world.TransformPoint(aabb.Center);
        var half = aabb.HalfExtents;

        var visible = true;
        foreach (var (normal, d) in _planes)
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

        ecs.Wrap<ShowAabbGizmoRef>(ctx.Entity).Color = visible ? new Color(0f, 1f, 0f, 1f) : null;
    }
}

/// <summary>The torus by the wall, which rises in and out of the small camera's view.</summary>
[Behavior]
public partial struct WallShape;

/// <summary>The small camera in the corner, whose frustum is drawn and decides each box's color.</summary>
[Behavior]
public partial struct MyCamera;

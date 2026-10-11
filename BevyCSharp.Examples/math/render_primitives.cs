// Bevy's render_primitives example, examples/math/render_primitives.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

namespace BevyCSharp.Examples.Maths;

// How each of Bevy's math primitives looks in 2D and in 3D, as a mesh on one side and as gizmos on
// the other, turning. C switches between the 2D camera and the 3D one, and the up and down arrows
// step through the primitives.
internal static class RenderPrimitives
{
    private const float LeftRightOffset2d = 200f, LeftRightOffset3d = 2f;
    private const float Small2d = 50f, Big2d = 100f, Small3d = 0.5f, Big3d = 1f;
    private const float QuarterTurn = MathF.PI / 4f;

    internal enum CameraActive { Dim2, Dim3 }

    internal enum PrimitiveSelected
    {
        RectangleAndCuboid, CircleAndSphere, Ellipse, Triangle, Plane, Line, Segment, Polyline, Polygon, ConvexPolygon,
        RegularPolygon, Capsule, Cylinder, Cone, ConicalFrustum, Torus, Tetrahedron, Arc, CircularSector, CircularSegment,
    }

    private static readonly (float X, float Y)[] Polyline2dVertices = [(-Big2d, -Small2d), (-Small2d, Small2d), (Small2d, -Small2d), (Big2d, Small2d)];
    private static readonly Vec3[] Polyline3dVertices = [new(-Big3d, -Small3d, -Small3d), new(Small3d, Small3d, 0f), new(-Small3d, -Small3d, 0f), new(Big3d, Small3d, Small3d)];
    private static readonly (float X, float Y)[] ConvexPolygonVertices = [(-Big2d, -Small2d), (Big2d, -Small2d), (Big2d, Small2d), (Big2d / 2f, Small2d * 2f), (-Big2d, Small2d)];
    private static readonly (float X, float Y)[] PolygonVertices = [(-Big2d, -Small2d), (Big2d, -Small2d), (Big2d, Small2d), (0f, 0f), (-Big2d, Small2d)];
    private static readonly (float X, float Y)[] Triangle2dVertices = [(Big2d, 0f), (0f, Big2d), (-Big2d, 0f)];
    private static readonly Vec3[] Triangle3dVertices = [new(Big3d, 0f, 0f), new(0f, Big3d, 0f), new(-Big3d, 0f, 0f)];
    private static readonly Vec3[] TetrahedronVertices = [new(-Big3d, 0f, 0f), new(Big3d, 0f, 0f), new(0f, 0f, -Big3d * 1.67f), new(0f, Big3d * 1.67f, -Big3d * 0.5f)];

    private static readonly (float R, float G, float B, float A) White = (1f, 1f, 1f, 1f);

    // Each mesh with the state it is shown in, the 2D ones turning flat and the 3D ones in space.
    private static readonly List<(Entity Entity, CameraActive Camera, PrimitiveSelected Primitive)> Meshes = [];
    private static Entity _camera2d, _camera3d, _header, _name;
    private static (PrimitiveSelected Primitive, CameraActive Camera)? _shown;

    public static void Build(App app)
    {
        Meshes.Clear();
        _shown = null;

        app.AddState(PrimitiveSelected.RectangleAndCuboid);
        app.AddState(CameraActive.Dim2);
        app.Startup(Setup, "render_primitives.Setup");
        app.Update(SwitchCameras, "render_primitives.SwitchCameras");
        app.Update(SwitchPrimitive, "render_primitives.SwitchPrimitive");
        app.Update(Show, "render_primitives.Show");
        app.Update(DrawGizmos, "render_primitives.DrawGizmos");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // The 2D camera, and the 3D one above the scene looking down, Z up on the screen, off.
        _camera2d = Render2d.SpawnCamera2d();
        _camera3d = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 10f, 0f), Vec3.Zero, Vec3.UnitZ), new CameraSettings());
        ecs.Wrap<CameraRef>(_camera3d).IsActive = false;

        Render.SetAmbientLight((1f, 1f, 1f), 50f);
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 5000f });
        ecs.Add(light, Transform.LookingAt(new Vec3(-LeftRightOffset3d, 2f, 0f), new Vec3(-LeftRightOffset3d, 0f, 0f), Vec3.UnitY));

        SpawnText(ecs);
        SpawnPrimitives2d(ecs);
        SpawnPrimitives3d(ecs);
    }

    // The header, the primitive shown and the keys, drawn by whichever camera is on.
    private static void SpawnText(EcsWorld ecs)
    {
        _header = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Justify = UiJustify.Center, Top = Length.Px(5f), Camera = _camera2d });
        var style = new UiTextSettings { Justify = TextJustify.Center };
        var text = Ui.SpawnText(string.Empty, new UiSettings(), style);
        ecs.SetParent(text, _header);
        Ui.SpawnTextSpan(text, "Primitive: ", style, White);
        _name = Ui.SpawnTextSpan(text, Name(PrimitiveSelected.RectangleAndCuboid), style, White);
        Ui.SpawnTextSpan(text, "\n\n", style, White);
        Ui.SpawnTextSpan(text, "Press 'C' to switch between 2D and 3D mode\nPress 'Up' or 'Down' to switch to the next/previous primitive", style, White);
        Ui.SpawnTextSpan(text, "\n\n", style, White);
        Ui.SpawnTextSpan(text, "(If nothing is displayed, there's no rendering support yet)", style, White);
    }

    private static string Name(PrimitiveSelected primitive) => primitive switch
    {
        PrimitiveSelected.RectangleAndCuboid => "Rectangle/Cuboid",
        PrimitiveSelected.CircleAndSphere => "Circle/Sphere",
        _ => primitive.ToString(),
    };

    private static void SpawnPrimitives2d(EcsWorld ecs)
    {
        var material = Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.White });
        (PrimitiveSelected, AssetHandle)[] meshes =
        [
            (PrimitiveSelected.RectangleAndCuboid, Render.CreateMesh(MeshShape.Rectangle, Small2d * 2f, Big2d * 2f)),
            (PrimitiveSelected.CircleAndSphere, Render.CreateMesh(MeshShape.Circle, Big2d)),
            (PrimitiveSelected.Ellipse, Render.CreateMesh(MeshShape.Ellipse, Big2d, Small2d)),
            (PrimitiveSelected.Triangle, Flat(Triangle2dVertices, [0, 1, 2])),
            (PrimitiveSelected.Segment, Render.CreateMesh(new MeshData { Positions = [new(-Big2d / 2f, 0f, 0f), new(Big2d / 2f, 0f, 0f)], Topology = MeshTopology.Lines })),
            (PrimitiveSelected.Polyline, Render.CreateMesh(new MeshData { Positions = [.. Polyline2dVertices.Select(point => new Vec3(point.X, point.Y, 0f))], Topology = MeshTopology.LineStrip })),
            (PrimitiveSelected.ConvexPolygon, Flat(ConvexPolygonVertices, [0, 1, 2, 0, 2, 3, 0, 3, 4])),
            (PrimitiveSelected.RegularPolygon, Render.CreateMesh(MeshShape.RegularPolygon, Big2d, 5f)),
            (PrimitiveSelected.Capsule, Render.CreateMesh(MeshShape.Capsule2d, Small2d, Small2d * 2f)),
            (PrimitiveSelected.Torus, Render.CreateMesh(MeshShape.Annulus, Small2d, Big2d)),
            (PrimitiveSelected.CircularSector, Render.CreateMesh(MeshShape.CircularSector, Big2d, QuarterTurn * 2f)),
            (PrimitiveSelected.CircularSegment, Render.CreateMesh(MeshShape.CircularSegment, Big2d, QuarterTurn * 2f)),
        ];

        foreach (var (primitive, mesh) in meshes)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(LeftRightOffset2d, 0f, 0f));
            Render2d.SetMesh(ecs, entity, mesh);
            Render2d.SetMaterial(ecs, entity, material);
            Meshes.Add((entity, CameraActive.Dim2, primitive));
        }
    }

    private static void SpawnPrimitives3d(EcsWorld ecs)
    {
        var material = Render.CreateMaterial(new MaterialSettings { BaseColor = White });
        (PrimitiveSelected, AssetHandle)[] meshes =
        [
            (PrimitiveSelected.RectangleAndCuboid, Render.CreateMesh(MeshShape.Cuboid, Big3d * 2f, Small3d * 2f, Big3d * 2f)),
            (PrimitiveSelected.CircleAndSphere, Render.CreateMesh(MeshShape.Sphere, Big3d)),
            (PrimitiveSelected.Triangle, Faces(Triangle3dVertices, [0, 1, 2])),
            (PrimitiveSelected.Plane, Render.CreateMesh(MeshShape.Plane, Big3d * 2f, Big3d * 2f)),
            (PrimitiveSelected.Segment, Render.CreateMesh(new MeshData { Positions = [new(-Big3d / 2f, 0f, 0f), new(Big3d / 2f, 0f, 0f)], Topology = MeshTopology.Lines })),
            (PrimitiveSelected.Polyline, Render.CreateMesh(new MeshData { Positions = [.. Polyline3dVertices], Topology = MeshTopology.LineStrip })),
            (PrimitiveSelected.Capsule, Render.CreateMesh(MeshShape.Capsule, Small3d, Small3d * 2f)),
            (PrimitiveSelected.Cylinder, Render.CreateMesh(MeshShape.Cylinder, Small3d, Small3d * 2f)),
            (PrimitiveSelected.Cone, Render.CreateMesh(MeshShape.Cone, Big3d, Big3d)),
            (PrimitiveSelected.ConicalFrustum, Render.CreateMesh(MeshShape.ConicalFrustum, Big3d, Small3d, Big3d)),
            (PrimitiveSelected.Torus, Render.CreateMesh(MeshShape.Torus, Small3d, Small3d * 2f)),
            (PrimitiveSelected.Tetrahedron, Faces(TetrahedronVertices, [0, 1, 2, 0, 2, 3, 0, 3, 1, 1, 3, 2])),
        ];

        foreach (var (primitive, mesh) in meshes)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(-LeftRightOffset3d, 0f, 0f));
            Render.SetMesh(ecs, entity, mesh);
            Render.SetMaterial(ecs, entity, material);
            Meshes.Add((entity, CameraActive.Dim3, primitive));
        }
    }

    // A flat mesh of triangles through points, facing the 2D camera.
    private static AssetHandle Flat((float X, float Y)[] points, uint[] indices) => Render.CreateMesh(new MeshData
    {
        Positions = [.. points.Select(point => new Vec3(point.X, point.Y, 0f))],
        Normals = [.. points.Select(_ => Vec3.UnitZ)],
        Indices = indices,
    });

    // A mesh of flat faces through points, each face its own corners and its normal, as Bevy's
    // triangle and tetrahedron meshes are.
    private static AssetHandle Faces(Vec3[] points, uint[] indices)
    {
        var positions = new List<Vec3>();
        var normals = new List<Vec3>();
        for (var i = 0; i < indices.Length; i += 3)
        {
            var (a, b, c) = (points[indices[i]], points[indices[i + 1]], points[indices[i + 2]]);
            var normal = Vec3.Cross(b - a, c - a).Normalized;
            positions.AddRange([a, b, c]);
            normals.AddRange([normal, normal, normal]);
        }

        return Render.CreateMesh(new MeshData { Positions = [.. positions], Normals = [.. normals] });
    }

    // C between the two cameras, the header drawn by the one on.
    private static void SwitchCameras(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.C)) return;
        ctx.SetState(App.State<CameraActive>() == CameraActive.Dim2 ? CameraActive.Dim3 : CameraActive.Dim2);
    }

    // Up to the next primitive, down to the one before, around the list.
    private static void SwitchPrimitive(BehaviorContext ctx)
    {
        var step = (ctx.Input.KeyPressed(Key.ArrowUp) ? 1 : 0) - (ctx.Input.KeyPressed(Key.ArrowDown) ? 1 : 0);
        if (step == 0) return;
        var count = Enum.GetValues<PrimitiveSelected>().Length;
        ctx.SetState((PrimitiveSelected)(((int)App.State<PrimitiveSelected>() + step + count) % count));
    }

    // The meshes of the primitive and the camera on shown, the rest hidden, the header and the
    // cameras following the states as they change, and the meshes shown turned.
    private static void Show(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var now = (App.State<PrimitiveSelected>(), App.State<CameraActive>());
        if (now != _shown)
        {
            _shown = now;
            var dim2 = now.Item2 == CameraActive.Dim2;
            ecs.Wrap<CameraRef>(_camera2d).IsActive = dim2;
            ecs.Wrap<CameraRef>(_camera3d).IsActive = !dim2;
            ecs.Wrap<UiTargetCameraRef>(_header).Value = dim2 ? _camera2d : _camera3d;
            ecs.Wrap<TextSpanRef>(_name).Value = Name(now.Item1);

            foreach (var (entity, camera, primitive) in Meshes)
                ecs.Wrap<VisibilityRef>(entity).Value = camera == now.Item2 && primitive == now.Item1 ? Visibility.Inherited : Visibility.Hidden;
        }

        var turn2d = Quat.FromRotationZ(ctx.Time.Elapsed);
        var turn3d = Turn3d(ctx.Time.Elapsed);
        foreach (var (entity, camera, primitive) in Meshes)
        {
            if (camera != now.Item2 || primitive != now.Item1) continue;
            var transform = ecs.GetOrDefault<Transform>(entity);
            transform.Rotation = camera == CameraActive.Dim2 ? turn2d : turn3d;
            ecs.Set(entity, transform);
        }
    }

    // Bevy's turn of the 3D shapes, from Z to a direction that wanders with the time.
    private static Quat Turn3d(float seconds)
    {
        var to = new Vec3(MathF.Sin(seconds), MathF.Cos(seconds), MathF.Sin(seconds) * 0.5f);
        to = to.Length > 0f ? to.Normalized : Vec3.UnitZ;
        var axis = Vec3.Cross(Vec3.UnitZ, to);
        var w = 1f + Vec3.Dot(Vec3.UnitZ, to);
        if (w < 1e-6f) return Quat.FromAxisAngle(Vec3.UnitX, MathF.PI);
        var length = MathF.Sqrt(axis.X * axis.X + axis.Y * axis.Y + axis.Z * axis.Z + w * w);
        return new Quat(axis.X / length, axis.Y / length, axis.Z / length, w / length);
    }

    // The gizmos of the primitive, on the other side of whichever camera is on.
    private static void DrawGizmos(BehaviorContext ctx)
    {
        var angle = ctx.Time.Elapsed;
        if (App.State<CameraActive>() == CameraActive.Dim2) DrawGizmos2d(App.State<PrimitiveSelected>(), angle);
        else DrawGizmos3d(App.State<PrimitiveSelected>(), Turn3d(angle));
    }

    private static void DrawGizmos2d(PrimitiveSelected primitive, float angle)
    {
        var at = (-LeftRightOffset2d, 0f);
        switch (primitive)
        {
            case PrimitiveSelected.RectangleAndCuboid: Gizmos.Rect2d(at, Small2d * 2f, Big2d * 2f, White, angle); break;
            case PrimitiveSelected.CircleAndSphere: Gizmos.Circle2d(at, Big2d, White); break;
            case PrimitiveSelected.Ellipse: Gizmos.Ellipse2d(at, Big2d, Small2d, White, angle); break;
            case PrimitiveSelected.Triangle: Gizmos.Polyline2d(Placed(Triangle2dVertices, at, angle), White, closed: true); break;
            case PrimitiveSelected.Plane: Gizmos.Plane2d(at, White, angle); break;
            case PrimitiveSelected.Line: Gizmos.EndlessLine2d(at, White, angle); break;
            case PrimitiveSelected.Segment: Gizmos.Polyline2d(Placed([(-Big2d / 2f, 0f), (Big2d / 2f, 0f)], at, angle), White); break;
            case PrimitiveSelected.Polyline: Gizmos.Polyline2d(Placed(Polyline2dVertices, at, angle), White); break;
            case PrimitiveSelected.ConvexPolygon: Gizmos.Polyline2d(Placed(ConvexPolygonVertices, at, angle), White, closed: true); break;
            case PrimitiveSelected.Polygon: Gizmos.Polyline2d(Placed(PolygonVertices, at, angle), White, closed: true); break;
            case PrimitiveSelected.RegularPolygon: Gizmos.RegularPolygon2d(at, Big2d, 5, White, angle); break;
            case PrimitiveSelected.Capsule: Gizmos.Capsule2d(at, Small2d, Small2d, White, angle); break;
            case PrimitiveSelected.Torus: Gizmos.Annulus2d(at, Small2d, Big2d, White, angle); break;

            // Bevy's arc is centered on the Y axis, which the flat arc reaches by starting half of
            // it back from there.
            case PrimitiveSelected.Arc: Gizmos.Arc2d(at, Big2d, QuarterTurn * 2f, White, from: angle - QuarterTurn); break;
            case PrimitiveSelected.CircularSector: Gizmos.CircularSector2d(at, Big2d, QuarterTurn, White, angle); break;
            case PrimitiveSelected.CircularSegment: Gizmos.CircularSegment2d(at, Big2d, QuarterTurn, White, angle); break;

            // No flat gizmo of a cylinder, a cone, a conical frustum or a tetrahedron, as in Bevy.
        }
    }

    private static void DrawGizmos3d(PrimitiveSelected primitive, Quat rotation)
    {
        var at = new Vec3(LeftRightOffset3d, 0f, 0f);
        const uint Resolution = 10;
        switch (primitive)
        {
            case PrimitiveSelected.RectangleAndCuboid: Gizmos.Box(at, rotation, new Vec3(Big3d * 2f, Small3d * 2f, Big3d * 2f), White, inFront: false); break;
            case PrimitiveSelected.CircleAndSphere: Gizmos.Sphere(at, rotation, Big3d, White, inFront: false, resolution: Resolution); break;
            case PrimitiveSelected.Triangle: Gizmos.Polyline(Placed(Triangle3dVertices, at, rotation), White, closed: true); break;
            case PrimitiveSelected.Plane: Gizmos.Plane(at, rotation, Big3d, Big3d, White); break;
            case PrimitiveSelected.Line: Gizmos.EndlessLine(at, rotation, White); break;
            case PrimitiveSelected.Segment: Gizmos.Polyline(Placed([new(-Big3d / 2f, 0f, 0f), new(Big3d / 2f, 0f, 0f)], at, rotation), White); break;
            case PrimitiveSelected.Polyline: Gizmos.Polyline(Placed(Polyline3dVertices, at, rotation), White); break;
            case PrimitiveSelected.Capsule: Gizmos.Capsule(at, rotation, Small3d, Small3d * 2f, White, inFront: false, resolution: Resolution); break;
            case PrimitiveSelected.Cylinder: Gizmos.Cylinder(at, rotation, Small3d, Small3d, White, inFront: false, resolution: Resolution); break;
            case PrimitiveSelected.Cone: Gizmos.Cone(at, rotation, Big3d, Big3d, White, inFront: false, resolution: Resolution); break;
            case PrimitiveSelected.ConicalFrustum: Gizmos.Frustum(at, rotation, Small3d, Big3d, Big3d, White, inFront: false); break;
            case PrimitiveSelected.Torus: Gizmos.Torus(at, rotation, Small3d * 1.5f, Small3d * 0.5f, White, inFront: false, resolution: Resolution); break;
            case PrimitiveSelected.Tetrahedron: Gizmos.Tetrahedron(at + rotation * TetrahedronVertices[0], at + rotation * TetrahedronVertices[1], at + rotation * TetrahedronVertices[2], at + rotation * TetrahedronVertices[3], White); break;

            // No gizmo of an ellipse, a polygon or the parts of a circle in 3D, as in Bevy.
        }
    }

    // Points of a shape where its gizmo is drawn, turned about Z, then moved.
    private static (float X, float Y)[] Placed((float X, float Y)[] points, (float X, float Y) at, float angle)
    {
        var (sin, cos) = MathF.SinCos(angle);
        return [.. points.Select(point => (at.X + point.X * cos - point.Y * sin, at.Y + point.X * sin + point.Y * cos))];
    }

    private static Vec3[] Placed(Vec3[] points, Vec3 at, Quat rotation) => [.. points.Select(point => at + rotation * point)];
}

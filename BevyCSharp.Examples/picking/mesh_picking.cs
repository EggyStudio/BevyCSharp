// Bevy's mesh_picking example, examples/picking/mesh_picking.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Pointers;

// Two rows of turning shapes, the solids and the extrusions, picked by the pointer. A shape turns
// cyan under it and yellow while pressed, the point the pointer meets is marked with its normal,
// and a drag turns the shape.
//
// Bevy observes its pointer events on each shape and keeps the nearest hit under each pointer.
// Here a ray is cast from the pointer each frame and the shape it meets is the one under it, the
// ground being left out as Bevy's is. Picking a mesh needs the editor profile of the bridge.
internal static class MeshPicking
{
    private const float ShapesXExtent = 14f, ExtrusionXExtent = 16f, ZExtent = 5f;

    private static Entity _camera, _hovered, _pressed;
    private static AssetHandle _white, _hover, _press;
    private static readonly HashSet<Entity> Shapes = [];
    private static (float X, float Y) _pointer;

    public static void Build(App app)
    {
        app.Startup(Setup, "mesh_picking.SetupScene");
        app.Update(Pick, "mesh_picking.Pick");
        app.Update(ctx =>
        {
            foreach (var shape in Shapes)
            {
                var at = ctx.Ecs.GetOrDefault<Transform>(shape);
                ctx.Ecs.Set(shape, at with { Rotation = Quat.FromRotationY(ctx.Time.Delta / 2f) * at.Rotation });
            }
        }, "mesh_picking.Rotate");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Shapes.Clear();
        (_hovered, _pressed) = (Entity.None, Entity.None);

        // Tailwind's colors at 300, white while nothing touches a shape.
        _white = Render.CreateMaterial((1f, 1f, 1f, 1f));
        _hover = Render.CreateMaterial(Color.FromSrgb8(103, 232, 249));
        _press = Render.CreateMaterial(Color.FromSrgb8(253, 224, 71));

        // Bevy's default of each, a unit across or half a unit in radius.
        AssetHandle[] shapes =
        [
            Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f),
            Render.CreateMesh(MeshShape.Tetrahedron, 1f),
            Render.CreateMesh(MeshShape.Capsule, 0.5f, 1f),
            Render.CreateMesh(MeshShape.Torus, 0.5f, 1.5f),
            Render.CreateMesh(MeshShape.Cylinder, 0.5f, 1f),
            Render.CreateMesh(MeshShape.Cone, 0.5f, 1f),
            Render.CreateMesh(MeshShape.ConicalFrustum, 0.25f, 0.5f, 0.5f),
            Render.CreateMesh(MeshShape.Sphere, 0.5f),
            Render.CreateMesh(MeshShape.UvSphere, 0.5f, 32f, 18f),
        ];
        AssetHandle[] extrusions =
        [
            Render.CreateMesh(MeshShape.Extrusion(MeshShape.Rectangle), 1f, 1f, 1f),
            Render.CreateMesh(MeshShape.Extrusion(MeshShape.Capsule2d), 0.5f, 1f, 1f),
            Render.CreateMesh(MeshShape.Extrusion(MeshShape.Annulus), 0.5f, 1f, 1f),
            Render.CreateMesh(MeshShape.Extrusion(MeshShape.Circle), 0.5f, 1f, 1f),
            Render.CreateMesh(MeshShape.Extrusion(MeshShape.Ellipse), 1f, 0.5f, 1f),
            Render.CreateMesh(MeshShape.Extrusion(MeshShape.RegularPolygon), 0.5f, 6f, 1f),
            Render.CreateMesh(MeshShape.Extrusion(MeshShape.Triangle), 1f, 1f, 1f),
        ];

        void Row(AssetHandle[] meshes, float extent, float z)
        {
            for (var i = 0; i < meshes.Length; i++)
            {
                var x = -extent / 2f + i / (float)(meshes.Length - 1) * extent;
                Shapes.Add(ecs.SpawnMesh(meshes[i], _white, new Transform(new Vec3(x, 2f, z), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One)));
            }
        }

        Row(shapes, ShapesXExtent, ZExtent / 2f);
        Row(extrusions, ExtrusionXExtent, -ZExtent / 2f);

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 50f, 50f), Render.CreateMaterial(Color.FromSrgb8(209, 213, 219)), Transform.Identity);

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 10_000_000f, Range = 100f, Shadows = true, ShadowDepthBias = 0.2f });
        ecs.Add(light, Transform.At(8f, 16f, 8f));
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 7f, 14f), new Vec3(0f, 1f, 0f), Vec3.UnitY));

        Ui.SpawnText("Hover over the shapes to pick them\nDrag to rotate", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static void Pick(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        var pointer = input.MousePosition;
        var (dx, dy) = (pointer.X - _pointer.X, pointer.Y - _pointer.Y);
        _pointer = pointer;

        // The shape under the pointer and where on it, the ground meeting the ray being nothing.
        var under = Entity.None;
        if (Render.TryRay(_camera, pointer.X, pointer.Y, out var origin, out var direction)
            && Picking.TryCast(origin, direction, out var hit, out var point, out var normal)
            && Shapes.Contains(hit))
        {
            under = hit;
            Gizmos.Sphere(point, 0.05f, Color.FromSrgb8(239, 68, 68), inFront: false);
            Gizmos.Arrow(point, point + normal * 0.5f, Color.FromSrgb8(252, 231, 243), inFront: false);
        }

        // Over and out, press and release, each giving the shape the material Bevy's observers do.
        if (under != _hovered)
        {
            if (_hovered != Entity.None && _hovered != _pressed) Render.SetMaterial(ecs, _hovered, _white);
            if (under != Entity.None && under != _pressed) Render.SetMaterial(ecs, under, _hover);
            _hovered = under;
        }

        if (input.MousePressed(MouseButton.Left) && under != Entity.None)
        {
            _pressed = under;
            Render.SetMaterial(ecs, under, _press);
        }

        if (_pressed != Entity.None && !input.MouseDown(MouseButton.Left))
        {
            Render.SetMaterial(ecs, _pressed, _pressed == _hovered ? _hover : _white);
            _pressed = Entity.None;
        }

        // A drag turns the shape it started on.
        if (_pressed != Entity.None && (dx != 0f || dy != 0f))
        {
            var at = ecs.GetOrDefault<Transform>(_pressed);
            ecs.Set(_pressed, at with { Rotation = Quat.FromRotationX(dy * 0.02f) * Quat.FromRotationY(dx * 0.02f) * at.Rotation });
        }
    }
}

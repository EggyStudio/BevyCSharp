// Bevy's mesh_picking example, examples/picking/mesh_picking.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Pointers;

// Two rows of turning shapes, the solids and the extrusions, picked by the pointer. A shape turns
// cyan under it and yellow while pressed, the point the pointer meets is marked with its normal,
// and a drag turns the shape.
//
// Bevy marks the nearest hit its pointer keeps each frame. Here the same ray is cast from the
// pointer each frame for the mark, the ground being left out as Bevy's is.
internal static class MeshPicking
{
    private const float ShapesXExtent = 14f, ExtrusionXExtent = 16f, ZExtent = 5f;

    private static Entity _camera;
    private static AssetHandle _white, _hover, _press;

    public static void Build(App app)
    {
        app.Startup(Setup, "mesh_picking.SetupScene");
        app.Update(DrawMeshIntersections, "mesh_picking.DrawMeshIntersections");
    }

    // Meshes are picked, as Bevy's adds MeshPickingPlugin.
    public static void Configure(Config config) => config.MeshPicking = true;

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

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
                var shape = ecs.SpawnMesh(meshes[i], _white, new Transform(new Vec3(x, 2f, z), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
                ecs.Add(shape, new Shape());

                // Bevy's update_material_on for each, and rotate_on_drag.
                ecs.Observe<Pointer<Over>>(shape, on => Render.SetMaterial(on.Ecs, on.Entity, _hover));
                ecs.Observe<Pointer<Out>>(shape, on => Render.SetMaterial(on.Ecs, on.Entity, _white));
                ecs.Observe<Pointer<Press>>(shape, on => Render.SetMaterial(on.Ecs, on.Entity, _press));
                ecs.Observe<Pointer<Release>>(shape, on => Render.SetMaterial(on.Ecs, on.Entity, _hover));
                ecs.Observe<Pointer<Drag>>(shape, on =>
                {
                    var delta = on.Event.Event.Delta;
                    var at = on.Ecs.GetOrDefault<Transform>(on.Entity);
                    on.Ecs.Set(on.Entity, at with { Rotation = Quat.FromRotationX(delta.Y * 0.02f) * Quat.FromRotationY(delta.X * 0.02f) * at.Rotation });
                });
            }
        }

        Row(shapes, ShapesXExtent, ZExtent / 2f);
        Row(extrusions, ExtrusionXExtent, -ZExtent / 2f);

        // The ground, which picking passes through.
        var ground = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 50f, 50f), Render.CreateMaterial(Color.FromSrgb8(209, 213, 219)), Transform.Identity);
        var ignore = ecs.Insert<PickableRef>(ground);
        (ignore.ShouldBlockLower, ignore.IsHoverable) = (false, false);

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 10_000_000f, Range = 100f, Shadows = true, ShadowDepthBias = 0.2f });
        ecs.Add(light, Transform.At(8f, 16f, 8f));
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 7f, 14f), new Vec3(0f, 1f, 0f), Vec3.UnitY));

        Ui.SpawnText("Hover over the shapes to pick them\nDrag to rotate", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // The point a ray from the pointer meets a shape, with the way the surface faces there.
    private static void DrawMeshIntersections(BehaviorContext ctx)
    {
        var (x, y) = ctx.Input.MousePosition;
        if (!Render.TryRay(_camera, x, y, out var origin, out var direction)
            || !Picking.TryCast(origin, direction, out var hit, out var point, out var normal)
            || !ctx.Ecs.Has<Shape>(hit))
        {
            return;
        }

        Gizmos.Sphere(point, 0.05f, Color.FromSrgb8(239, 68, 68), inFront: false);
        Gizmos.Arrow(point, point + normal * 0.5f, Color.FromSrgb8(252, 231, 243), inFront: false);
    }
}

/// <summary>One of the shapes to pick, which turns slowly about its own up.</summary>
[Behavior]
public partial struct Shape
{
    /// <summary>Turned by half a radian a second, as Bevy's <c>rotate</c> turns each shape.</summary>
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationY(ctx.Time.Delta / 2f) * transform.Rotation;
}

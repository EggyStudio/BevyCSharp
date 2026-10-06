// Bevy's viewport_node example, examples/ui/widgets/viewport_node.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// A viewport widget, a node showing what a camera of its own draws, through which the scene that
// camera sees is picked as any other. A drag with the left button turns the cube inside it, a drag
// with the right moves the widget, and an arrow marks where the pointer meets the cube.
//
// Bevy marks every hit its pointers keep each frame. Here the cube's own pointer events say where
// the pointer met it, which marks the same point while the pointer moves over it.
internal static class ViewportNode
{
    // Where each pointer last met the cube, while it is over it.
    private static readonly Dictionary<PointerId, (Vec3 Point, Vec3 Normal)> Hits = [];

    public static void Build(App app)
    {
        app.Startup(Setup, "viewport_node.Test");
        app.Update(DrawMeshIntersections, "viewport_node.DrawMeshIntersections");
    }

    // Meshes are picked, as Bevy's adds MeshPickingPlugin.
    public static void Configure(Config config) => config.MeshPicking = true;

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Hits.Clear();

        // The camera the interface is drawn by.
        ecs.SpawnCamera3d(Transform.Identity);

        // The camera drawing into the image the widget shows, before the interface's, its image
        // sized by the widget to its own size.
        var image = Render.CreateTarget(200, 200);
        var camera = ecs.SpawnCamera3d(Transform.Identity, new CameraSettings { Order = -1 });
        Render.SetCameraTarget(camera, image);

        // Something for it to look at, turned by a drag with the left button.
        var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 5f, 5f, 5f), Render.CreateMaterial((1f, 1f, 1f, 1f)), Transform.At(0f, 0f, -10f));
        ecs.Add(cube, new ViewportShape());
        ecs.Observe<Pointer<Drag>>(cube, on =>
        {
            if (on.Event.Event.Button != PointerButton.Primary) return;
            var delta = on.Event.Event.Delta;
            var at = on.Ecs.GetOrDefault<Transform>(on.Entity);
            on.Ecs.Set(on.Entity, at with { Rotation = Quat.FromRotationX(delta.Y * 0.02f) * Quat.FromRotationY(delta.X * 0.02f) * at.Rotation });
        });
        ecs.Observe<Pointer<Over>>(cube, on => Remember(on.Event.PointerId, on.Event.Event.Hit));
        ecs.Observe<Pointer<Bevy.Move>>(cube, on => Remember(on.Event.PointerId, on.Event.Event.Hit));
        ecs.Observe<Pointer<Out>>(cube, on => Hits.Remove(on.Event.PointerId));

        // The widget, moved by a drag with the right button.
        var widget = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Top = Length.Px(50f),
            Left = Length.Px(50f),
            Width = Length.Px(200f),
            Height = Length.Px(200f),
            Border = Sides.All(Length.Px(5f)),
            BorderColor = (1f, 1f, 1f, 1f),
        });
        ecs.Insert<ViewportNodeRef>(widget).Camera = camera;
        ecs.Observe<Pointer<Drag>>(widget, on =>
        {
            if (on.Event.Event.Button != PointerButton.Secondary) return;
            var node = on.Ecs.Wrap<NodeRef>(on.Entity);
            if (node.Top is Val.Px top && node.Left is Val.Px left)
            {
                var delta = on.Event.Event.Delta;
                (node.Left, node.Top) = (new Val.Px(left.Value + delta.X), new Val.Px(top.Value + delta.Y));
            }
        });
    }

    private static void Remember(PointerId pointer, PointerHit hit)
    {
        if (hit.Position is { } point && hit.Normal is { } normal) Hits[pointer] = (point, normal);
    }

    // A white arrow out of the cube where each pointer meets it.
    private static void DrawMeshIntersections(BehaviorContext ctx)
    {
        foreach (var (point, normal) in Hits.Values)
            Gizmos.Arrow(point, point + normal.Normalized * 0.5f, new Color(1f, 1f, 1f, 1f));
    }
}

/// <summary>
/// The cube the widget's camera looks at, Bevy's <c>Shape</c> under another name since
/// mesh_picking's turns.
/// </summary>
[Behavior]
public partial struct ViewportShape;

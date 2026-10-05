using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Pointers;

// Text to click for a cube, each landing on the last, and cubes turned by dragging them.
//
// Bevy observes its pointer's clicks and drags on each entity. Here the text's interaction is read
// as it changes, a click being a press let go over it, and a drag is found by casting a ray from
// the pointer when its button goes down and followed by how far the pointer moves while it stays
// down. Picking a mesh needs the editor profile of the bridge, which carries Bevy's mesh picking.
internal static class SimplePicking
{
    private static readonly (float R, float G, float B, float A) White = (1f, 1f, 1f, 1f), Cyan400 = Scene.Srgb8(34, 211, 238);

    private static Entity _camera, _text, _dragged;
    private static UiInteraction _last;
    private static int _count;
    private static (float X, float Y) _pointer;
    private static AssetHandle _cube, _blue;
    private static readonly HashSet<Entity> Cubes = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_count, _dragged, _last) = (0, Entity.None, UiInteraction.None);
            Cubes.Clear();
            _text = Ui.SpawnText("Click Me to get a box\nDrag cubes to rotate", new UiSettings { Interactive = true, Absolute = true, Top = Length.Percent(12f), Left = Length.Percent(12f), Color = White });

            ecs.Mesh(Render.CreateMesh(MeshShape.Circle, 4f), Scene.Material(White), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
            ecs.PointLight(new Vec3(4f, 8f, 4f), shadows: true);
            _camera = ecs.Camera(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));
            (_cube, _blue) = (Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f), Scene.Material(Scene.Srgb8(124, 144, 255)));
        }, "simple_picking.SetupScene");

        app.Update(ClickText, "simple_picking.ClickText");
        app.Update(DragCubes, "simple_picking.DragCubes");
    }

    // Cyan under the pointer and white off it, and a click on it stacks another cube.
    private static void ClickText(BehaviorContext ctx)
    {
        var interaction = Ui.InteractionOf(_text);
        if (interaction == _last) return;

        var color = interaction == UiInteraction.None ? White : Cyan400;
        ctx.Ecs.Wrap<TextColorRef>(_text).Value = new Color(color.R, color.G, color.B, color.A);
        if (_last == UiInteraction.Pressed && interaction == UiInteraction.Hovered)
        {
            var cube = ctx.Ecs.Mesh(_cube, _blue, Transform.At(0f, 0.25f + 0.55f * _count, 0f));
            Cubes.Add(cube);
            _count++;
        }

        _last = interaction;
    }

    // A press on a cube takes hold of it, and while the button is held it turns about the world's
    // up by how far the pointer moves across and about its X by how far it moves down.
    private static void DragCubes(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var pointer = input.MousePosition;
        var (dx, dy) = (pointer.X - _pointer.X, pointer.Y - _pointer.Y);
        _pointer = pointer;

        if (input.MousePressed(MouseButton.Left)
            && Render.TryRay(_camera, pointer.X, pointer.Y, out var origin, out var direction)
            && Picking.TryCast(origin, direction, out var hit, out _, out _)
            && Cubes.Contains(hit))
        {
            _dragged = hit;
        }

        if (!input.MouseDown(MouseButton.Left)) _dragged = Entity.None;
        if (_dragged == Entity.None || (dx == 0f && dy == 0f)) return;

        var at = ctx.Ecs.GetOrDefault<Transform>(_dragged);
        var turned = Quat.FromRotationX(dy * 0.02f) * Quat.FromRotationY(dx * 0.02f) * at.Rotation;
        ctx.Ecs.Set(_dragged, at with { Rotation = turned });
    }
}

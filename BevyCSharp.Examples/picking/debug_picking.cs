// Bevy's debug_picking example, examples/picking/debug_picking.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Pointers;

// Picking events for the interface and for meshes, as simple_picking shows them, with picking's
// debug overlay naming what is under the pointer. Text to click for a cube, each landing on the
// last, which turns cyan under the pointer, cubes turned by dragging them, and F3 to go from the
// overlay to the overlay with every event logged and to neither.
internal static class DebugPicking
{
    private static int _count;
    private static AssetHandle _cube, _blue;

    // Meshes are picked and picking's debug tool is there, as Bevy's adds MeshPickingPlugin and
    // DebugPickingPlugin.
    public static void Configure(Config config) => (config.MeshPicking, config.DevTools) = (true, DevTools.PickingDebug);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _count = 0;
            (_cube, _blue) = (Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f), Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)));

            // Bevy's starts in its normal mode, the overlay without the noisy events, and its F3 is
            // the picking's alone, the engine's debug overlay given no key here.
            Picking.DebugMode = PickingDebugMode.Normal;
            Debug.OverlayKey = null;

            var text = Ui.SpawnText("Click Me to get a box\nDrag cubes to rotate\nPress F3 to cycle between picking debug levels",
                new UiSettings { Absolute = true, Top = Length.Percent(12f), Left = Length.Percent(12f) });
            ecs.Observe<Pointer<Click>>(text, on => SpawnCube(on.Ecs));
            ecs.Observe<Pointer<Out>>(text, on => on.Ecs.Wrap<TextColorRef>(on.Entity).Value = new Color(1f, 1f, 1f, 1f));
            ecs.Observe<Pointer<Over>>(text, on => on.Ecs.Wrap<TextColorRef>(on.Entity).Value = Color.FromSrgb8(34, 211, 238));

            var floor = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Circle, 4f), Render.CreateMaterial((1f, 1f, 1f, 1f)), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
            ecs.SetName(floor, "Base");
            ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));
        }, "debug_picking.SetupScene");

        // F3 goes from disabled to normal to noisy and round again.
        app.Update(ctx =>
        {
            if (!ctx.Input.KeyPressed(Key.F3)) return;
            Picking.DebugMode = Picking.DebugMode switch
            {
                PickingDebugMode.Disabled => PickingDebugMode.Normal,
                PickingDebugMode.Normal => PickingDebugMode.Noisy,
                _ => PickingDebugMode.Disabled,
            };
        }, "debug_picking.CycleMode");
    }

    // A cube on top of the last, which a drag turns.
    private static void SpawnCube(EcsWorld ecs)
    {
        var cube = ecs.SpawnMesh(_cube, _blue, Transform.At(0f, 0.25f + 0.55f * _count, 0f));
        ecs.Observe<Pointer<Drag>>(cube, on =>
        {
            // About the world's up by how far the pointer moved across, then its X by how far down.
            var delta = on.Event.Event.Delta;
            var at = on.Ecs.GetOrDefault<Transform>(on.Entity);
            on.Ecs.Set(on.Entity, at with { Rotation = Quat.FromRotationX(delta.Y * 0.02f) * Quat.FromRotationY(delta.X * 0.02f) * at.Rotation });
        });
        _count++;
    }
}

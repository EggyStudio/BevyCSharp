// Bevy's simple_picking example, examples/picking/simple_picking.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Pointers;

// Picking events for the interface and for meshes. Text to click for a cube, each landing on the
// last, which turns cyan under the pointer, and cubes turned by dragging them.
internal static class SimplePicking
{
    private static int _count;
    private static AssetHandle _cube, _blue;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        _count = 0;
        (_cube, _blue) = (Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f), Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)));

        var text = Ui.SpawnText("Click Me to get a box\nDrag cubes to rotate", new UiSettings { Absolute = true, Top = Length.Percent(12f), Left = Length.Percent(12f) });
        ecs.Observe<Pointer<Click>>(text, on => SpawnCube(on.Ecs));
        ecs.Observe<Pointer<Out>>(text, on => on.Ecs.Wrap<TextColorRef>(on.Entity).Value = new Color(1f, 1f, 1f, 1f));
        ecs.Observe<Pointer<Over>>(text, on => on.Ecs.Wrap<TextColorRef>(on.Entity).Value = Color.FromSrgb8(34, 211, 238));

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Circle, 4f), Render.CreateMaterial((1f, 1f, 1f, 1f)), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));
    }, "simple_picking.SetupScene");

    // Meshes are picked, as Bevy's adds MeshPickingPlugin.
    public static void Configure(Config config) => config.MeshPicking = true;

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

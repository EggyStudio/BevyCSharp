// Bevy's infinite_grid example, examples/dev_tools/infinite_grid.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.DevTool;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Bevy's infinite grid, a ground plane of lines that fade with distance, as an editor shows one for
// the way things face and how large they are, with a see-through cube above it and one below. The
// free camera flies with WASD, E and Q and turns with the mouse while its left button is held, and
// P shows or hides the grid.
internal static class InfiniteGrid
{
    private static Entity _grid;

    // Bevy's FreeCameraPlugin and InfiniteGridPlugin.
    public static void Configure(Config config) => (config.CameraControllers, config.DevTools) = (CameraControllers.Free, DevTools.InfiniteGrid);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            // The grid, its settings Bevy's defaults.
            _grid = ecs.Spawn();
            ecs.Insert<InfiniteGridRef>(_grid);
            ecs.Insert<InfiniteGridSettingsRef>(_grid);

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-12.5f, 5f, 10f), Vec3.Zero, Vec3.UnitY));
            Render.SetFreeCamera(camera, new FreeCameraSettings());

            // Bevy's default directional light, which casts no shadow.
            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
            ecs.Add(light, Transform.LookingAt(new Vec3(15f, 20f, 0f), Vec3.Zero, Vec3.UnitY));

            // White at half its opacity, blended as Bevy's material from a color of less than full
            // alpha is.
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
            var glass = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 0.5f), AlphaMode = AlphaMode.Blend });
            ecs.SpawnMesh(cube, glass, Transform.At(0f, 2f, 0f));
            ecs.SpawnMesh(cube, glass, Transform.At(0f, -2f, 0f));
        }, "infinite_grid.SetupSystem");

        // Bevy's toggle_visibility, P hiding the grid where it is shown and showing it where not.
        app.Update(ctx =>
        {
            if (!ctx.Input.KeyPressed(Key.P)) return;
            var shown = ctx.Ecs.Get<InheritedVisibilityRef>(_grid)?.Value == true;
            ctx.Ecs.Wrap<VisibilityRef>(_grid).Value = shown ? Visibility.Hidden : Visibility.Visible;
        }, "infinite_grid.ToggleVisibility");
    }
}

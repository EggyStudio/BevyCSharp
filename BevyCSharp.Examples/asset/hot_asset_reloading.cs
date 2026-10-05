using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows a scene loaded from a glTF file that is loaded again whenever the file changes, so an edit
// to torus.gltf shows while the app runs.
//
// Bevy watches its asset folder when its file_watcher feature is built, which the editor profile
// builds here, so a game watching its files runs on that profile.
internal static class HotAssetReloading
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
            ecs.Add(sun, Transform.LookingAt(new Vec3(4f, 5f, 4f), Vec3.Zero, Vec3.UnitY));
            ecs.Camera(Transform.LookingAt(new Vec3(2f, 2f, 6f), Vec3.Zero, Vec3.UnitY));
        }, "hot_asset_reloading.Setup");

        app.SpawnGltf("models/torus/torus.gltf");
    }
}

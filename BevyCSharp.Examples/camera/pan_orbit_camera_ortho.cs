// Bevy's pan_orbit_camera_ortho example, examples/camera/pan_orbit_camera_ortho.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Cameras;

// The pan-orbit camera on an orthographic projection, over a cube of twenty seven engine models.
// The controller places the camera's near and far planes about the point it orbits, since depth is
// linear in an orthographic view, so the camera starts with the depth of that point, and the sky is
// drawn by a camera of its own with a field of view, which an orthographic one has none of.
internal static class PanOrbitCameraOrtho
{
    private static AssetHandle _scene = AssetHandle.None;
    private static bool _spawned;

    public static void Configure(Config config) =>
        (config.CameraControllers, config.MeshPicking) = (CameraControllers.PanOrbit, true);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_scene, _spawned) = (AssetHandle.None, false);

            var diffuse = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2");
            var specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
            var translation = new Vec3(10f, 10f, 10f);

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(translation, Vec3.Zero, Vec3.UnitY));
            // Bevy's orthographic projection for 3D, a pixel a hundredth of a unit.
            ecs.Wrap<ProjectionRef>(camera).Value = new Projection.Orthographic(
                0f, 1000f, new Vec2(0.5f, 0.5f), new ScalingMode.WindowSize(), 0.01f, new Vec2(-1f, -1f), new Vec2(1f, 1f));
            Render.SetEnvironmentMap(camera, diffuse, specular, 5000f);

            // The depth of the point it orbits, where the controller puts the near and far planes.
            ecs.Insert<PanOrbitCameraRef>(camera).LastAnchorDepth = -translation.Length;

            // The sky drawn by a camera that follows this one with its own field of view.
            var sky = ecs.Insert<IndependentSkyboxRef>(camera);
            sky.Skybox = diffuse;
            sky.Brightness = 2500f;

            Ui.SpawnText("Left Mouse - Pan\nRight Mouse - Orbit\nScroll - Zoom", new UiSettings { Margin = Sides.All(Length.Px(20f)) }, 20f);
        }, "pan_orbit_camera_ortho.Setup");

        // Three across, three up and three deep, two apart, once the model has loaded.
        app.Update(ctx =>
        {
            if (_spawned) return;
            if (_scene == AssetHandle.None) _scene = AssetServer.LoadGltfScene("PlaneEngine/scene.gltf", 0);
            if (AssetServer.StateOf(_scene) != AssetLoadState.Loaded) return;

            _spawned = true;
            var halfWidth = (int)((MathF.Pow(27f, 1f / 3f) - 1f) / 2f);
            for (var x = -halfWidth; x <= halfWidth; x++)
            {
                for (var y = -halfWidth; y <= halfWidth; y++)
                {
                    for (var z = -halfWidth; z <= halfWidth; z++)
                    {
                        var root = ctx.Ecs.SpawnScene(_scene);
                        ctx.Ecs.Set(root, Transform.At(x * 2f, y * 2f, z * 2f));
                    }
                }
            }
        }, "pan_orbit_camera_ortho.SpawnEngines");
    }
}

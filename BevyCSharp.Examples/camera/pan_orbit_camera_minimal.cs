// Bevy's pan_orbit_camera_minimal example, examples/camera/pan_orbit_camera_minimal.rs at v0.20.0,
// by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Cameras;

// The fewest steps to the pan-orbit camera, the left button panning, the right orbiting and the
// wheel zooming about the point under the pointer, which mesh picking finds, around an engine
// model lit by an environment map.
internal static class PanOrbitCameraMinimal
{
    public static void Configure(Config config) =>
        (config.CameraControllers, config.MeshPicking) = (CameraControllers.PanOrbit, true);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var camera = ctx.Ecs.SpawnCamera3d(Transform.At(0f, 0f, 1f));
            ctx.Ecs.Insert<PanOrbitCameraRef>(camera);
            Render.SetEnvironmentMap(
                camera,
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
                2500f);
        }, "pan_orbit_camera_minimal.Setup");

        // Bevy's example loads the model over the network from Bevy's asset_files repository, which
        // the examples here fetch as they are built.
        app.SpawnGltf("PlaneEngine/scene.gltf");
    }
}

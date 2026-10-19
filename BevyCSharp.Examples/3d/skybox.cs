// Bevy's skybox example, examples/3d/skybox.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Load a cubemap texture onto a cube like a skybox and cycle through different compressed texture
// formats, those the GPU decodes.
internal static class Skybox
{
    // Each copy of the sky, with the format a device decodes to load it.
    private static readonly (string Path, CompressedImageFormats Format)[] Cubemaps =
    [
        ("textures/Ryfjallet_cubemap.png", CompressedImageFormats.None),
        ("textures/Ryfjallet_cubemap_astc4x4.ktx2", CompressedImageFormats.Astc),
        ("textures/Ryfjallet_cubemap_bc7.ktx2", CompressedImageFormats.Bc),
        ("textures/Ryfjallet_cubemap_etc2.ktx2", CompressedImageFormats.Etc2),
    ];
    private const float SwapDelay = 3f;

    private static Entity _camera;
    private static int _index;
    private static float _nextSwap;

    public static void Configure(Config config) => config.CameraControllers = CameraControllers.Free;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_index, _nextSwap) = (0, 0f);

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 32_000f, Shadows = false });
            ecs.Add(sun, new Transform(new Vec3(0f, 2f, 0f), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
            ecs.Add(sun, new Sun());

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 8f), Vec3.Zero, Vec3.UnitY));
            Render.SetFreeCamera(_camera, new FreeCameraSettings());
            Render.SetPostProcessing(_camera, new PostSettings { Msaa = 1, AntiAlias = AntiAliasPass.Temporal });
            Render.SetAmbientOcclusion(_camera, AmbientOcclusionQuality.High);
            Show(Cubemaps[0].Path);

            var sky = Color.FromSrgb8(210, 220, 240);
            Render.SetAmbientLight((sky.R, sky.G, sky.B), 1f);
        });

        // Another format every three seconds.
        app.Update(ctx =>
        {
            var now = ctx.Time.Elapsed;
            if (_nextSwap == 0f)
            {
                _nextSwap = now + SwapDelay;
                return;
            }

            if (now < _nextSwap) return;
            _nextSwap += SwapDelay;

            // The next the device decodes, skipping those it does not, and the same one again where
            // it decodes none of the others.
            var decoded = Render.CompressedFormats;
            var next = _index;
            for (var i = 0; i < Cubemaps.Length; i++)
            {
                next = (next + 1) % Cubemaps.Length;
                if (decoded.HasFlag(Cubemaps[next].Format)) break;
                Console.WriteLine($"Skipping format which is not supported by current hardware: {Cubemaps[next]}");
            }

            if (next == _index) return;
            _index = next;
            Console.WriteLine($"Swapping to {Cubemaps[_index].Path}...");
            Show(Cubemaps[_index].Path);
        }, "skybox.CycleCubemapAsset");
    }

    // A picture of six faces stacked in a column, made a cubemap once it has loaded.
    private static void Show(string path)
    {
        var image = AssetServer.Load(AssetKind.Image, path);
        Render.MakeCubemap(image);
        Render.SetSkybox(_camera, image, 1000f);
    }
}

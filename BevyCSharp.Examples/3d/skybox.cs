// Bevy's skybox example, examples/3d/skybox.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Load a cubemap texture onto a cube like a skybox and cycle through different compressed texture
// formats.
//
// Bevy asks the GPU which compressed formats it decodes and cycles through those. The bridge does
// not say, so this cycles through the uncompressed picture and BC7, which every desktop GPU
// decodes, and leaves ASTC and ETC2 out.
internal static class Skybox
{
    private static readonly string[] Cubemaps = ["textures/Ryfjallet_cubemap.png", "textures/Ryfjallet_cubemap_bc7.ktx2"];
    private const float SwapDelay = 3f;

    private static Entity _camera;
    private static int _index;
    private static float _nextSwap;

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
            ecs.Add(_camera, new FreeCamera());
            Render.SetPostProcessing(_camera, new PostSettings { Msaa = 1, AntiAlias = AntiAliasPass.Temporal });
            Render.SetAmbientOcclusion(_camera, AmbientOcclusionQuality.High);
            Show(Cubemaps[0]);

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

            _index = (_index + 1) % Cubemaps.Length;
            Console.WriteLine($"Swapping to {Cubemaps[_index]}...");
            Show(Cubemaps[_index]);
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

// Bevy's many_sprite_meshes example, examples/stress_tests/many_sprite_meshes.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.StressTests;

// many_sprites with sprite meshes, sprites Bevy draws through its mesh pipeline. A great many are
// drawn at different sizes, turns and scales, and the camera is moved over them to see how well
// what is off screen is left out of the drawing. With --colored they are tinted in three colors,
// which draws them in more batches and so more slowly.
internal static class ManySpriteMeshes
{
    private const float CameraSpeed = 1000f;

    // Blue, white and red, from Bevy's palette of CSS colors.
    private static readonly Color[] Colors = [new(0f, 0f, 1f, 1f), new(1f, 1f, 1f, 1f), new(1f, 0f, 0f, 1f)];

    // Whether the sprites are tinted, which Bevy keeps in a resource read from its arguments.
    private static bool _colorTint;

    private static Entity _camera;
    private static int _sprites;

    // Bevy's timer for the count, a second long, kept by its printing system.
    private static float _printing;

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and its frame times logged once a second by Bevy's plugins.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
    }

    public static void Build(App app)
    {
        _colorTint = Environment.GetCommandLineArgs().Contains("--colored");
        (_sprites, _printing) = (0, 0f);

        app.Startup(Setup, "many_sprite_meshes.Setup");
        app.Update(PrintSpriteCount, "many_sprite_meshes.PrintSpriteCount");
        app.Update(MoveCamera, "many_sprite_meshes.MoveCamera");
    }

    private static void Setup(BehaviorContext ctx)
    {
        StressTest.Warn();
        var ecs = ctx.Ecs;
        var random = new Random();
        const float TileSize = 64f;
        const int Half = 320 / 2;
        var image = AssetServer.Load(AssetKind.Image, "branding/icon.png");

        _camera = Render2d.SpawnCamera2d();

        for (var y = -Half; y < Half; y++)
        {
            for (var x = -Half; x < Half; x++)
            {
                var translation = new Vec3(x * TileSize, y * TileSize, random.NextSingle());
                var rotation = Quat.FromRotationZ(random.NextSingle());
                var scale = new Vec3(random.NextSingle() * 2f);

                var sprite = ecs.Spawn();
                ecs.Add(sprite, new Transform(translation, rotation, scale));
                var mesh = ecs.Insert<SpriteMeshRef>(sprite);
                mesh.Image = image;
                mesh.CustomSize = new Vec2(TileSize, TileSize);
                if (_colorTint) mesh.Color = Colors[random.Next(3)];
                _sprites++;
            }
        }
    }

    // Turns the camera and moves it along the way it faces.
    private static void MoveCamera(BehaviorContext ctx)
    {
        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        var delta = ctx.Time.Delta;
        camera.Rotation = Quat.FromRotationZ(delta * 0.5f) * camera.Rotation;
        camera.Translation += camera.Rotation * (Vec3.UnitX * CameraSpeed * delta);
        ctx.Ecs.Set(_camera, camera);
    }

    // The count of sprite meshes once a second. Bevy counts them with a query each time, and as nothing
    // here spawns or despawns one after the start, the count spawned is the same number.
    private static void PrintSpriteCount(BehaviorContext ctx)
    {
        _printing += ctx.Time.Delta;
        if (_printing < 1f) return;

        _printing -= 1f;
        Console.WriteLine($"Sprites: {_sprites}");
    }
}

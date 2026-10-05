// Bevy's many_animated_sprites example, examples/stress_tests/many_animated_sprites.rs at v0.19.1,
// by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.StressTests;

// Draws a great many sprites, each running through the frames of a sheet on a timer of its own, at
// different sizes, turns and scales, and moves the camera over them to see how well what is off
// screen is left out of the drawing.
internal static class ManyAnimatedSprites
{
    private const float CameraSpeed = 1000f;
    internal const float TileSize = 64f;

    private static Entity _camera;
    private static int _sprites;
    private static float _printing;

    // The sheet and its layout, which every sprite shows a frame of.
    internal static AssetHandle Texture, Layout;

    public static void Configure(Config config) => StressTest.Configure(config);

    public static void Build(App app)
    {
        (_sprites, _printing) = (0, 0f);
        StressTest.Add(app);

        app.Startup(Setup, "many_animated_sprites.Setup");
        app.Update(PrintSpriteCount, "many_animated_sprites.PrintSpriteCount");
        app.Update(MoveCamera, "many_animated_sprites.MoveCamera");
    }

    private static void Setup(BehaviorContext ctx)
    {
        StressTest.Warn();
        var ecs = ctx.Ecs;
        var random = new Random();
        const int Half = 320 / 2;

        Texture = AssetServer.Load(AssetKind.Image, "textures/rpg/chars/gabe/gabe-idle-run.png");
        Layout = Render2d.CreateAtlas(24, 24, 7, 1);
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
                Render2d.SetSprite(ecs, sprite, Texture, new SpriteSettings { Size = (TileSize, TileSize), Atlas = Layout, Frame = 0 });

                // A timer a tenth of a second long, started at a random point up to a second in,
                // as Bevy sets it, so the sprites do not all turn their frames at once.
                ecs.Add(sprite, new AnimationTimer { Elapsed = random.NextSingle() });
                _sprites++;
            }
        }
    }

    private static void MoveCamera(BehaviorContext ctx)
    {
        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        var delta = ctx.Time.Delta;
        camera.Rotation = Quat.FromRotationZ(delta * 0.5f) * camera.Rotation;
        camera.Translation += camera.Rotation * (Vec3.UnitX * CameraSpeed * delta);
        ctx.Ecs.Set(_camera, camera);
    }

    // The count once a second, the number spawned, since nothing despawns one.
    private static void PrintSpriteCount(BehaviorContext ctx)
    {
        _printing += ctx.Time.Delta;
        if (_printing < 1f) return;

        _printing -= 1f;
        Console.WriteLine($"Sprites: {_sprites}");
    }
}

/// <summary>A sprite's timer and the frame of the sheet it shows.</summary>
[Behavior]
public partial struct AnimationTimer
{
    private const float Duration = 0.1f;
    private const uint Frames = 7;

    private static readonly List<(Entity Sprite, uint Frame)> Turned = [];

    /// <summary>How far the timer has run, which may start past its length.</summary>
    public float Elapsed;

    /// <summary>The frame of the sheet shown.</summary>
    public uint Frame;

    /// <summary>
    /// Each timer ticked, and the sprite of each that finished on to its next frame.
    /// </summary>
    /// <remarks>
    /// Bevy's system walks the timers and the sprites together. A frame is a sprite set again
    /// here, which reaches the world, so the timers are walked first and the sprites set after
    /// the walk, on the thread that holds the world.
    /// </remarks>
    [OnUpdate]
    public static void AnimateSprite(BehaviorContext ctx)
    {
        var delta = ctx.Time.Delta;
        Turned.Clear();
        foreach (var row in ctx.Ecs.Query<AnimationTimer>())
        {
            ref var timer = ref row.Component;
            timer.Elapsed += delta;
            if (timer.Elapsed < Duration) continue;

            timer.Elapsed %= Duration;
            timer.Frame = (timer.Frame + 1) % Frames;
            Turned.Add((row.Entity, timer.Frame));
        }

        foreach (var (sprite, frame) in Turned)
            Render2d.SetSprite(ctx.Ecs, sprite, ManyAnimatedSprites.Texture, new SpriteSettings { Size = (ManyAnimatedSprites.TileSize, ManyAnimatedSprites.TileSize), Atlas = ManyAnimatedSprites.Layout, Frame = frame });
    }
}

// Bevy's bevymark example, examples/stress_tests/bevymark.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.StressTests;

// Birds thrown from the top left corner that fall and bounce off the window's edges, ten thousand
// a second while the left button is held, or in waves with --waves and --per-wave, to measure how
// many 2D things can be drawn and moved. --mode sprite, sprite_mesh or mesh2d draws them as
// sprites, sprite meshes or 2D meshes, --benchmark spawns every wave at once and steps them by the
// same time each frame, --vary-per-instance gives each a color or material of its own,
// --material-texture-count draws them from more textures, --ordered-z stacks them in order, and
// --alpha-mode opaque, blend or alpha_mask sets how they blend.
//
// Bevy's example also turns off its static transform optimizations, a resource Bevy reflects but
// not as a resource, which the bridge cannot set, so here they are left on.
internal static class Bevymark
{
    private const int BirdsPerSecond = 10_000;
    internal const float Gravity = -9.8f * 100f;
    private const float MaxVelocity = 750f;
    private const float BirdScale = 0.15f;
    private const int BirdTextureSize = 256;
    internal const float HalfBirdSize = BirdTextureSize * BirdScale * 0.5f;
    internal const float FixedDeltaTime = 1f / 60f;
    private const float FixedTimestep = 0.2f;

    // Bevy's Args, read from the command line.
    private static string _mode = "sprite", _alphaMode = "blend";
    internal static bool Benchmark;
    private static bool _varyPerInstance, _orderedZ;
    private static int _perWave, _waves, _materialTextureCount = 1;

    // Bevy's BevyCounter, BirdScheduled and BirdResources.
    private static int _count;
    private static (float R, float G, float B, float A) _color = (1f, 1f, 1f, 1f);
    private static int _scheduledWaves;
    private static AssetHandle[] _textures = [];
    private static AssetHandle[] _materials = [];
    private static AssetHandle _quad;
    private static Random _colorRandom = new(42), _materialRandom = new(42), _velocityRandom = new(42), _transformRandom = new(42);

    // The mouse handler's own generator and wave, Locals in Bevy.
    private static Random _mouseRandom = new(42);
    private static int _wave;

    private static Entity _countSpan, _rawSpan, _smaSpan, _emaSpan;
    private static int _shownCount = -1;
    private static readonly Queue<double> Fps = new();

    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
        config.FixedHz = 1.0 / FixedTimestep;
    }

    public static void Build(App app)
    {
        var arguments = Environment.GetCommandLineArgs();
        string Option(string name, string fallback)
        {
            var at = Array.IndexOf(arguments, name);
            return at >= 0 && at + 1 < arguments.Length ? arguments[at + 1] : fallback;
        }

        (_mode, _alphaMode) = (Option("--mode", "sprite"), Option("--alpha-mode", "blend"));
        (Benchmark, _varyPerInstance, _orderedZ) = (arguments.Contains("--benchmark"), arguments.Contains("--vary-per-instance"), arguments.Contains("--ordered-z"));
        _perWave = int.Parse(Option("--per-wave", "0"));
        _waves = int.Parse(Option("--waves", "0"));
        _materialTextureCount = int.Parse(Option("--material-texture-count", "1"));
        (_count, _color, _wave, _shownCount) = (0, (1f, 1f, 1f, 1f), 0, -1);
        (_colorRandom, _materialRandom, _velocityRandom, _transformRandom, _mouseRandom) = (new(42), new(42), new(42), new(42), new(42));
        Fps.Clear();

        app.Startup(Setup, "bevymark.Setup");
        app.On(Stage.FixedUpdate, ScheduledSpawner, "bevymark.ScheduledSpawner");
        app.Update(MouseHandler, "bevymark.MouseHandler");
        app.Update(CounterSystem, "bevymark.CounterSystem");
    }

    private static void Setup(BehaviorContext ctx)
    {
        StressTest.Warn();
        var ecs = ctx.Ecs;

        List<AssetHandle> textures = [];
        if (_mode == "sprite" || _materialTextureCount > 0) textures.Add(AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        InitTextures(textures);
        _textures = [.. textures];
        _materials = InitMaterials(_textures);
        _quad = Render.CreateMesh(MeshShape.Rectangle, BirdTextureSize, BirdTextureSize);
        Bird.HalfExtents = HalfExtents();

        Render2d.SpawnCamera2d();

        // The counter, over everything, on a backing three quarters black.
        var panel = Ui.SpawnNode(new UiSettings { Absolute = true, Padding = Sides.All(Length.Px(5f)), Color = (0f, 0f, 0f, 0.75f) });
        ecs.Insert<GlobalZIndexRef>(panel).Value = int.MaxValue;
        var text = Ui.SpawnText("", new UiSettings(), 40f);
        ecs.SetParent(text, panel);
        (float, float, float, float) lime = Color.FromSrgb(0f, 1f, 0f), aqua = Color.FromSrgb(0f, 1f, 1f);
        var style = new UiTextSettings { FontSize = 40f };
        Ui.SpawnTextSpan(text, "Bird Count: ", style, lime);
        _countSpan = Ui.SpawnTextSpan(text, "", style, aqua);
        Ui.SpawnTextSpan(text, "\nFPS (raw): ", style, lime);
        _rawSpan = Ui.SpawnTextSpan(text, "", style, aqua);
        Ui.SpawnTextSpan(text, "\nFPS (SMA): ", style, lime);
        _smaSpan = Ui.SpawnTextSpan(text, "", style, aqua);
        Ui.SpawnTextSpan(text, "\nFPS (EMA): ", style, lime);
        _emaSpan = Ui.SpawnTextSpan(text, "", style, aqua);

        _scheduledWaves = _waves;

        // Every wave at once, each moved on as though it had been thrown at its time, so the run
        // starts at its heaviest.
        if (Benchmark)
        {
            for (var wave = _waves - 1; wave >= 0; wave--)
                SpawnBirds(ecs, _perWave, wave, wave);

            _scheduledWaves = 0;
        }
    }

    // Half the window's size, which the birds bounce inside.
    private static Vec2 HalfExtents()
    {
        var (width, height) = Window.Size();
        return new Vec2(width * 0.5f, height * 0.5f);
    }

    private static void ScheduledSpawner(BehaviorContext ctx)
    {
        if (_scheduledWaves <= 0) return;
        SpawnBirds(ctx.Ecs, _perWave, null, _scheduledWaves - 1);
        _scheduledWaves--;
    }

    // Birds thrown while the left button is held, and the next wave's color changed when it comes up.
    private static void MouseHandler(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (input.MouseReleased(MouseButton.Left))
            _color = (_mouseRandom.NextSingle(), _mouseRandom.NextSingle(), _mouseRandom.NextSingle(), 1f);

        if (input.MouseDown(MouseButton.Left))
        {
            SpawnBirds(ctx.Ecs, (int)(BirdsPerSecond * ctx.Time.DeltaSeconds), null, _wave);
            _wave++;
        }
    }

    private static void SpawnBirds(EcsWorld ecs, int count, int? wavesToSimulate, int wave)
    {
        var half = HalfExtents();
        var (birdX, birdY) = (-half.X + HalfBirdSize, half.Y - HalfBirdSize);

        for (var i = 0; i < count; i++)
        {
            var z = _orderedZ ? (_count + i) * 0.00001f : _transformRandom.NextSingle();
            var (at, velocity) = BirdVelocityTransform(half, new Vec3(birdX, birdY, z), wavesToSimulate);
            var color = _varyPerInstance ? (_colorRandom.NextSingle(), _colorRandom.NextSingle(), _colorRandom.NextSingle(), 1f) : _color;

            var bird = ecs.Spawn();
            ecs.Add(bird, new Transform(at, Quat.Identity, new Vec3(BirdScale)));
            switch (_mode)
            {
                case "sprite_mesh":
                {
                    var sprite = ecs.Insert<SpriteMeshRef>(bird);
                    sprite.Image = _textures[_materialRandom.Next(_textures.Length)];
                    sprite.Color = new Color(color.Item1, color.Item2, color.Item3, color.Item4);
                    sprite.AlphaMode = _alphaMode switch
                    {
                        "opaque" => new SpriteAlphaMode.Opaque(),
                        "alpha_mask" => new SpriteAlphaMode.Mask(0.5f),
                        _ => new SpriteAlphaMode.Blend(),
                    };
                    break;
                }

                case "mesh2d":
                {
                    var material = _varyPerInstance || _materialTextureCount > _waves
                        ? _materials[_materialRandom.Next(_materials.Length)]
                        : _materials[wave % _materials.Length];
                    Render2d.SetMesh(ecs, bird, _quad);
                    Render2d.SetMaterial(ecs, bird, material);
                    break;
                }

                default:
                    Render2d.SetSprite(ecs, bird, _textures[_materialRandom.Next(_textures.Length)], new SpriteSettings { Color = color });
                    break;
            }

            ecs.Add(bird, new Bird { Velocity = velocity });
        }

        _count += count;
        _color = (_colorRandom.NextSingle(), _colorRandom.NextSingle(), _colorRandom.NextSingle(), 1f);
    }

    // A bird's place and velocity, moved on as though thrown that many waves ago where it is.
    private static (Vec3, Vec3) BirdVelocityTransform(Vec2 half, Vec3 translation, int? waves)
    {
        var velocity = new Vec3(MaxVelocity * (_velocityRandom.NextSingle() - 0.5f), 0f, 0f);
        if (waves is { } simulated)
        {
            var steps = simulated * (int)MathF.Round(FixedTimestep / FixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                Bird.StepMovement(ref translation, ref velocity, FixedDeltaTime);
                Bird.HandleCollision(half, translation, ref velocity);
            }
        }

        return (translation, velocity);
    }

    // Single colors 256 a side, as many as asked for beyond the icon.
    private static void InitTextures(List<AssetHandle> textures)
    {
        var random = new Random(42);
        while (textures.Count < _materialTextureCount)
        {
            var pixel = new byte[] { (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), 255 };
            var texels = new byte[BirdTextureSize * BirdTextureSize * 4];
            for (var i = 0; i < texels.Length; i += 4) pixel.CopyTo(texels, i);
            textures.Add(Render.CreateImage(texels, BirdTextureSize, BirdTextureSize));
        }
    }

    // White on the first texture, then colors on textures picked at random, one for each bird with
    // --vary-per-instance, or one a texture or a wave.
    private static AssetHandle[] InitMaterials(AssetHandle[] textures)
    {
        var capacity = Math.Max(_varyPerInstance ? _perWave * _waves : Math.Max(_materialTextureCount, _waves), 1);
        var alphaMode = _alphaMode switch { "opaque" => AlphaMode2d.Opaque, "alpha_mask" => AlphaMode2d.Mask, _ => AlphaMode2d.Blend };
        var materials = new List<AssetHandle>
        {
            Render2d.CreateMaterial(new ColorMaterialSettings { Texture = textures.FirstOrDefault(), AlphaMode = alphaMode }),
        };

        var (colors, picks) = (new Random(42), new Random(42));
        while (materials.Count < capacity)
        {
            materials.Add(Render2d.CreateMaterial(new ColorMaterialSettings
            {
                Color = Color.FromSrgb8((byte)colors.Next(256), (byte)colors.Next(256), (byte)colors.Next(256)),
                Texture = textures.Length > 0 ? textures[picks.Next(textures.Length)] : AssetHandle.None,
                AlphaMode = alphaMode,
            }));
        }

        return [.. materials];
    }

    // The count when it changes, and the frames a second as they are, averaged and smoothed.
    private static void CounterSystem(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (_count != _shownCount)
        {
            ecs.Wrap<TextSpanRef>(_countSpan).Value = _count.ToString();
            _shownCount = _count;
        }

        var raw = ctx.Time.RawDeltaSeconds > 0 ? 1.0 / ctx.Time.RawDeltaSeconds : 0.0;
        Fps.Enqueue(raw);
        if (Fps.Count > 120) Fps.Dequeue();

        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        ecs.Wrap<TextSpanRef>(_rawSpan).Value = raw.ToString("F2", invariant);
        ecs.Wrap<TextSpanRef>(_smaSpan).Value = Fps.Average().ToString("F2", invariant);
        ecs.Wrap<TextSpanRef>(_emaSpan).Value = ctx.Time.SmoothedFps.ToString("F2", invariant);
    }
}

/// <summary>A bird, falling and bouncing off the window's edges.</summary>
[Behavior]
public partial struct Bird
{
    /// <summary>Half the window's size, which the birds bounce inside.</summary>
    public static Vec2 HalfExtents;

    /// <summary>How fast it moves, in pixels a second.</summary>
    public Vec3 Velocity;

    /// <summary>Moved by its velocity, which gravity pulls down, by a fixed step with --benchmark.</summary>
    [OnUpdate]
    public void MovementSystem(BehaviorContext ctx, ref Transform transform)
    {
        var dt = Bevymark.Benchmark ? Bevymark.FixedDeltaTime : ctx.Time.Delta;
        StepMovement(ref transform.Translation, ref Velocity, dt);
    }

    /// <summary>Turned back at an edge it is moving out through, and stopped rising at the top.</summary>
    [OnUpdate]
    public void CollisionSystem(BehaviorContext ctx, ref Transform transform) =>
        HandleCollision(HalfExtents, transform.Translation, ref Velocity);

    internal static void StepMovement(ref Vec3 translation, ref Vec3 velocity, float dt)
    {
        translation.X += velocity.X * dt;
        translation.Y += velocity.Y * dt;
        velocity.Y += Bevymark.Gravity * dt;
    }

    internal static void HandleCollision(Vec2 half, Vec3 translation, ref Vec3 velocity)
    {
        if ((velocity.X > 0f && translation.X + Bevymark.HalfBirdSize > half.X)
            || (velocity.X <= 0f && translation.X - Bevymark.HalfBirdSize < -half.X))
        {
            velocity.X = -velocity.X;
        }

        var velocityY = velocity.Y;
        if (velocityY < 0f && translation.Y - Bevymark.HalfBirdSize < -half.Y) velocity.Y = -velocityY;
        if (translation.Y + Bevymark.HalfBirdSize > half.Y && velocityY > 0f) velocity.Y = 0f;
    }
}

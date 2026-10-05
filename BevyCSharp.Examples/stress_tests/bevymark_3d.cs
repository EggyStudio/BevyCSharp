// Bevy's bevymark_3d example, examples/stress_tests/bevymark_3d.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.StressTests;

// Cubes thrown along the far edge of a box fifty units a side that fall and bounce off its walls,
// ten thousand a second while the left button is held, or in waves with --waves and --per-wave, to
// measure how many 3D things can be drawn and moved. --benchmark spawns every wave at once and
// steps them by the same time each frame, --vary-per-instance gives each a material of its own,
// --material-texture-count draws them from textures, and --alpha-mode opaque, blend or alpha_mask
// sets how they blend.
internal static class Bevymark3d
{
    private const int CubesPerSecond = 10_000;
    internal const float Gravity = -9.8f;
    private const float MaxVelocity = 10f;
    private const float CubeScale = 1f;
    private const int CubeTextureSize = 256;
    internal const float HalfCubeSize = CubeScale * 0.5f;
    internal const float VolumeWidth = 50f;
    internal const float FixedDeltaTime = 1f / 60f;
    private const float FixedTimestep = 0.2f;

    // Bevy's Args, read from the command line.
    private static string _alphaMode = "opaque";
    internal static bool Benchmark;
    private static bool _varyPerInstance;
    private static int _perWave, _waves, _materialTextureCount;

    // Bevy's BevyCounter, CubeScheduled and CubeResources.
    private static int _count;
    private static int _scheduledWaves;
    private static AssetHandle[] _materials = [];
    private static AssetHandle _cubeMesh;
    private static Random _materialRandom = new(12), _velocityRandom = new(97), _transformRandom = new(26);

    // The mouse handler's own wave, a Local in Bevy.
    private static int _wave;

    private static Entity _countSpan, _rawSpan, _smaSpan, _emaSpan;
    private static int _shownCount = -1;
    private static readonly Queue<double> Fps = new();

    public static void Configure(Config config)
    {
        StressTest.Configure(config);
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

        _alphaMode = Option("--alpha-mode", "opaque");
        (Benchmark, _varyPerInstance) = (arguments.Contains("--benchmark"), arguments.Contains("--vary-per-instance"));
        _perWave = int.Parse(Option("--per-wave", "0"));
        _waves = int.Parse(Option("--waves", "0"));
        _materialTextureCount = int.Parse(Option("--material-texture-count", "0"));
        (_count, _wave, _shownCount) = (0, 0, -1);
        (_materialRandom, _velocityRandom, _transformRandom) = (new(12), new(97), new(26));
        Fps.Clear();

        StressTest.Add(app);
        app.Startup(Setup, "bevymark_3d.Setup");
        app.On(Stage.FixedUpdate, ScheduledSpawner, "bevymark_3d.ScheduledSpawner");
        app.Update(MouseHandler, "bevymark_3d.MouseHandler");
        app.Update(CounterSystem, "bevymark_3d.CounterSystem");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        List<AssetHandle> textures = [];
        if (_materialTextureCount > 0) textures.Add(AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        InitTextures(textures);
        _materials = InitMaterials([.. textures]);
        _cubeMesh = Render.CreateMesh(MeshShape.Cuboid, CubeScale, CubeScale, CubeScale);

        ecs.Camera(Transform.LookingAt(new Vec3(VolumeWidth * 1.3f), Vec3.Zero, Vec3.UnitY));
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
        ecs.Add(light, Transform.LookingAt(new Vec3(1f, 2f, 3f), Vec3.Zero, Vec3.UnitY));

        // The counter, over everything, on a backing three quarters black.
        var panel = Ui.SpawnNode(new UiSettings { Absolute = true, Padding = Sides.All(Length.Px(5f)), Color = (0f, 0f, 0f, 0.75f) });
        ecs.Insert<GlobalZIndexRef>(panel).Value = int.MaxValue;
        var text = Ui.SpawnText("", new UiSettings(), 40f);
        ecs.SetParent(text, panel);
        (float, float, float, float) lime = Scene.Srgb(0f, 1f, 0f), aqua = Scene.Srgb(0f, 1f, 1f);
        var style = new UiTextSettings { FontSize = 40f };
        Ui.SpawnTextSpan(text, "Cube Count: ", style, lime);
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
                SpawnCubes(ecs, _perWave, wave, wave);

            _scheduledWaves = 0;
        }
    }

    private static void ScheduledSpawner(BehaviorContext ctx)
    {
        if (_scheduledWaves <= 0) return;
        SpawnCubes(ctx.Ecs, _perWave, null, _scheduledWaves - 1);
        _scheduledWaves--;
    }

    // Cubes thrown while the left button is held. Bevy's example also gives its counter a new color
    // as the button comes up, which nothing of the 3D test reads, and so it is left out.
    private static void MouseHandler(BehaviorContext ctx)
    {
        if (!ctx.Input.MouseDown(MouseButton.Left)) return;
        SpawnCubes(ctx.Ecs, (int)(CubesPerSecond * ctx.Time.DeltaSeconds), null, _wave);
        _wave++;
    }

    private static void SpawnCubes(EcsWorld ecs, int count, int? wavesToSimulate, int wave)
    {
        var batchMaterial = _materials[wave % _materials.Length];
        var (spawnY, spawnZ) = (VolumeWidth / 2f - HalfCubeSize, -VolumeWidth / 2f + HalfCubeSize);

        for (var i = 0; i < count; i++)
        {
            var at = new Vec3((_transformRandom.NextSingle() - 0.5f) * VolumeWidth, spawnY, spawnZ);
            var velocity = new Vec3(0f, 0f, MaxVelocity * _velocityRandom.NextSingle());
            if (wavesToSimulate is { } simulated)
            {
                var steps = simulated * (int)MathF.Round(FixedTimestep / FixedDeltaTime);
                for (var step = 0; step < steps; step++)
                {
                    Cube.StepMovement(ref at, ref velocity, FixedDeltaTime);
                    Cube.HandleCollision(at, ref velocity);
                }
            }

            var material = _varyPerInstance ? _materials[_materialRandom.Next(_materials.Length)] : batchMaterial;
            var cube = ecs.Mesh(_cubeMesh, material, Transform.At(at.X, at.Y, at.Z));
            ecs.Add(cube, new Cube { Velocity = velocity });
        }

        _count += count;
    }

    // Single colors 256 a side, as many as asked for beyond the icon.
    private static void InitTextures(List<AssetHandle> textures)
    {
        var random = new Random(42);
        while (textures.Count < _materialTextureCount)
        {
            var pixel = new byte[] { (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), 255 };
            var texels = new byte[CubeTextureSize * CubeTextureSize * 4];
            for (var i = 0; i < texels.Length; i += 4) pixel.CopyTo(texels, i);
            textures.Add(Render.CreateImage(texels, CubeTextureSize, CubeTextureSize));
        }
    }

    // White on the first texture, then colors on textures picked at random, one for each cube with
    // --vary-per-instance, or one a texture or a wave, and at least 256 unless benchmarking.
    private static AssetHandle[] InitMaterials(AssetHandle[] textures)
    {
        var capacity = _varyPerInstance ? _perWave * _waves : Math.Max(_materialTextureCount, _waves);
        if (!Benchmark) capacity = Math.Max(capacity, 256);
        capacity = Math.Max(capacity, 1);

        var alphaMode = _alphaMode switch { "blend" => AlphaMode.Blend, "alpha_mask" => AlphaMode.Mask, _ => AlphaMode.Opaque };
        var materials = new List<AssetHandle>
        {
            Render.CreateMaterial(new MaterialSettings { BaseColorTexture = textures.FirstOrDefault(), AlphaMode = alphaMode }),
        };

        var (colors, picks) = (new Random(42), new Random(42));
        while (materials.Count < capacity)
        {
            materials.Add(Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (colors.NextSingle(), colors.NextSingle(), colors.NextSingle(), 1f),
                BaseColorTexture = textures.Length > 0 ? textures[picks.Next(textures.Length)] : AssetHandle.None,
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

/// <summary>A cube, falling and bouncing off the walls of the box.</summary>
[Behavior]
public partial struct Cube
{
    /// <summary>How fast it moves, in units a second.</summary>
    public Vec3 Velocity;

    /// <summary>Moved by its velocity, which gravity pulls down, by a fixed step with --benchmark.</summary>
    [OnUpdate]
    public void MovementSystem(BehaviorContext ctx, ref Transform transform)
    {
        var dt = Bevymark3d.Benchmark ? Bevymark3d.FixedDeltaTime : ctx.Time.Delta;
        StepMovement(ref transform.Translation, ref Velocity, dt);
    }

    /// <summary>Turned back at a wall it is moving out through, and stopped rising at the top.</summary>
    [OnUpdate]
    public void CollisionSystem(BehaviorContext ctx, ref Transform transform) =>
        HandleCollision(transform.Translation, ref Velocity);

    internal static void StepMovement(ref Vec3 translation, ref Vec3 velocity, float dt)
    {
        translation += velocity * dt;
        velocity.Y += Bevymark3d.Gravity * dt;
    }

    internal static void HandleCollision(Vec3 translation, ref Vec3 velocity)
    {
        const float Half = Bevymark3d.VolumeWidth / 2f;
        if ((velocity.X > 0f && translation.X + Bevymark3d.HalfCubeSize > Half) || (velocity.X <= 0f && translation.X - Bevymark3d.HalfCubeSize < -Half))
            velocity.X = -velocity.X;

        if ((velocity.Z > 0f && translation.Z + Bevymark3d.HalfCubeSize > Half) || (velocity.Z <= 0f && translation.Z - Bevymark3d.HalfCubeSize < -Half))
            velocity.Z = -velocity.Z;

        var velocityY = velocity.Y;
        if (velocityY < 0f && translation.Y - Bevymark3d.HalfCubeSize < -Half) velocity.Y = -velocityY;
        if (translation.Y + Bevymark3d.HalfCubeSize > Half && velocityY > 0f) velocity.Y = 0f;
    }
}

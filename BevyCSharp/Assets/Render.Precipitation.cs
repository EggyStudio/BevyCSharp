using System.Numerics;
using Bevy.Reflected;

namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Lets rain and snow fall about a camera, or with <see langword="null"/> stops them. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The drops and flakes are moved on the GPU every frame, each a point that falls, drifts and
    /// comes back at the top of its box once it lands, and as many as stand in front of the camera
    /// are counted into an indirect draw, so the CPU never sees them. Each is drawn with the light it
    /// sends toward the eye, the sun's through the air, its shadows and the clouds', the sky's and
    /// the lamps', behind whatever stands in front of it, after temporal antialiasing, which would
    /// average a speck with no motion of its own in the prepass away, and after the sky's fog, which
    /// it is not fogged by. <see cref="PrecipitationSettings"/> says how they fall.
    /// </para>
    /// <para>
    /// The map of what stands over the ground is drawn by a camera of its own looking down, which
    /// draws the scene again on the frames it is drawn, a few once the camera has moved some 12
    /// meters, and none between. Settings given again every frame, as
    /// <see cref="SetWeather"/> gives them, take the numbers alone.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="settings">How it rains and snows, or null to stop.</param>
    /// <exception cref="ArgumentOutOfRangeException">An amount is outside 0 to 1, the wind is not a number, the reach is under 4 meters or the quality names none.</exception>
    /// <example>
    /// <code>
    /// Render.SetPrecipitation(camera, new PrecipitationSettings { Rain = 0.6f, Wind = new Vec3(3f, 0f, 1f) });
    /// </code>
    /// </example>
    public static void SetPrecipitation(Entity camera, PrecipitationSettings? settings)
    {
        if (settings is null)
        {
            Precipitation.Clear(camera);
            return;
        }

        if (!(settings.Rain is >= 0f and <= 1f) || !(settings.Snow is >= 0f and <= 1f))
            throw new ArgumentOutOfRangeException(nameof(settings), "How hard it rains and snows is from 0 to 1.");
        if (!float.IsFinite(settings.Wind.X) || !float.IsFinite(settings.Wind.Y) || !float.IsFinite(settings.Wind.Z))
            throw new ArgumentOutOfRangeException(nameof(settings), settings.Wind, "The wind is not a number.");
        if (!(settings.Reach >= 4f) || !float.IsFinite(settings.Reach))
            throw new ArgumentOutOfRangeException(nameof(settings), settings.Reach, "The drops reach 4 meters from the camera at least.");
        if (!Enum.IsDefined(settings.Quality))
            throw new ArgumentOutOfRangeException(nameof(settings), settings.Quality, "The quality names none.");

        Precipitation.Set(camera, settings);
    }
}

/// <summary>The rain and snow <see cref="Render.SetPrecipitation"/> lets fall about cameras, an app's own.</summary>
internal static class Precipitation
{
    internal const string Owner = "weather";

    /// <summary>How many splashes are kept at once, each lasting <see cref="SplashLife"/>.</summary>
    internal const int SplashSlots = 4096;

    /// <summary>How wide the map of what stands over the ground is, in meters, and its texels a side.</summary>
    internal const float RoofSize = 96f;

    internal const int RoofTexels = 256;

    // How long a splash lasts, how long a camera's shutter is open for a drop and for a flake, which
    // each one's streak is as long as it moves in, and how far above the camera the roofs camera
    // looks down from.
    private const float SplashLife = 0.35f;
    private const float DropShutter = 1f / 30f;
    private const float FlakeShutter = 1f / 120f;
    private const float RoofHeight = 300f;

    private static App? _app;
    private static readonly Dictionary<ulong, Camera> Cameras = [];
    // Roofs cameras let go of, despawned at the next tick, which has the world.
    private static readonly List<Entity> Dropped = [];
    private static AssetHandle _noShadow = AssetHandle.None;
    private static AssetHandle _noRoofs = AssetHandle.None;
    private static AssetHandle _noFrame = AssetHandle.None;

    private sealed class Camera
    {
        internal required ShaderInstance Clear { get; init; }
        internal required ShaderInstance Simulate { get; init; }
        internal required ShaderInstance Drops { get; init; }
        internal required ShaderInstance Splashes { get; init; }
        internal required AssetHandle Particles { get; init; }
        internal required AssetHandle Drawn { get; init; }
        internal required AssetHandle Counts { get; init; }
        internal required AssetHandle SplashBuffer { get; init; }
        internal required int Capacity { get; init; }
        internal required PrecipitationSettings Settings { get; set; }
        internal Roofs? Roofs { get; set; }
        internal uint Frame { get; set; }
        internal double Seconds { get; set; }
        internal float Step { get; set; }
    }

    // The camera looking down that draws what stands over the ground, the map its depth is turned
    // into and the map's frame, the middle it was last drawn about, and how many frames it is drawn
    // for yet.
    private sealed class Roofs
    {
        internal required Entity Camera { get; init; }
        internal required ShaderInstance Map { get; init; }
        internal required AssetHandle Heights { get; init; }
        internal required AssetHandle Frame { get; init; }
        internal required uint Layers { get; init; }
        internal Vec3? Middle { get; set; }
        internal int FramesLeft { get; set; }
        internal bool Active { get; set; } = true;
    }

    internal static int CapacityOf(PrecipitationQuality quality) => quality switch
    {
        PrecipitationQuality.Low => 16_384,
        PrecipitationQuality.High => 65_536,
        _ => 32_768,
    };

    internal static void Set(Entity camera, PrecipitationSettings settings)
    {
        ForgetOtherApps();
        var capacity = CapacityOf(settings.Quality);
        var had = Cameras.TryGetValue(camera.Bits, out var found);
        var rain = found!;
        if (!had || rain.Capacity != capacity)
        {
            var roofs = had ? rain.Roofs : null;
            rain = Make(capacity, settings);
            rain.Roofs = roofs;
            Cameras[camera.Bits] = rain;
        }

        // A camera whose drops stand as they are takes the numbers alone, so the weather can give
        // them every frame, its dispatches and draws left as they were.
        var standing = had && found!.Capacity == capacity && found.Settings.Splashes == settings.Splashes;
        rain.Settings = settings;
        Feed(camera, rain);
        if (standing) return;

        Shaders.SetEngineViewDispatches(camera, Owner,
            ViewDispatch.Fixed(rain.Clear, FramePoint.AfterPrepass, 1),
            ViewDispatch.Fixed(rain.Simulate, FramePoint.AfterPrepass, (uint)(capacity / 64)));
        // After temporal antialiasing, whose history would average away drops that move with no
        // motion of their own in the prepass, and after the sky's fog, which is not laid over them
        // as if they stood where the scene behind them does.
        var drops = ViewDraw.Indirect(rain.Drops, FramePoint.AfterTemporalAntialiasing, rain.Counts, 0, DrawBlend.Alpha, writesDepth: false);
        if (settings.Splashes)
            Shaders.SetEngineViewDraws(camera, Owner, drops, ViewDraw.Fixed(rain.Splashes, FramePoint.AfterTemporalAntialiasing, 6, SplashSlots, DrawBlend.Alpha, writesDepth: false));
        else
            Shaders.SetEngineViewDraws(camera, Owner, drops);
    }

    // A camera's programs and buffers, every particle asleep and no splash kept.
    private static Camera Make(int capacity, PrecipitationSettings settings)
    {
        var asleep = new Vector4[capacity];
        Array.Fill(asleep, new Vector4(0f, 0f, 0f, -1f));
        var counts = Shaders.CreateBuffer<uint>([6u, 0u, 0u, 0u, 0u, 0u, 0u, 0u]);
        var rain = new Camera
        {
            Clear = Shaders.CreateInstance(SceneField.KernelProgram("precipitation.slang", entry: "clear")),
            Simulate = Shaders.CreateInstance(SceneField.KernelProgram("precipitation.slang")),
            Drops = SceneField.Draw("precipitation_drops.slang"),
            Splashes = SceneField.Draw("precipitation_splashes.slang"),
            Particles = Shaders.CreateBuffer<Vector4>(asleep),
            Drawn = Shaders.CreateBuffer(capacity * 3 * 16),
            Counts = counts,
            SplashBuffer = Shaders.CreateBuffer(SplashSlots * 2 * 16),
            Capacity = capacity,
            Settings = settings,
        };

        rain.Clear.SetBuffer("counts", counts);
        rain.Simulate.SetBuffer("particles", rain.Particles)
            .SetBuffer("drawn", rain.Drawn)
            .SetBuffer("counts", counts)
            .SetBuffer("splashes", rain.SplashBuffer)
            .SetSampler("cloud_shadow_sampler", SamplerSettings.Linear);
        rain.Drops.SetBuffer("drawn", rain.Drawn);
        rain.Splashes.SetBuffer("splashes", rain.SplashBuffer);
        return rain;
    }

    // The numbers a frame's drops are moved and drawn with, the map of what stands over the ground
    // and the clouds' shadow.
    private static void Feed(Entity camera, Camera rain)
    {
        var settings = rain.Settings;
        // A flake, small and slow, shows less than a drop's streak, so it takes more of them to
        // snow as hard as it rains.
        var awake = MathF.Round(rain.Capacity * Math.Clamp(settings.Rain + (1.5f * settings.Snow), 0f, 1f));
        var seconds = (float)(rain.Seconds % 3600.0);
        rain.Simulate.Set("amount", new Vector4(settings.Rain, settings.Snow, awake, settings.Splashes ? SplashSlots : 0f))
            .Set("reach", new Vector4(settings.Reach / 4f, settings.Reach, Math.Min(15f, settings.Reach / 2f), 0f))
            .Set("wind", new Vector4(settings.Wind.X, settings.Wind.Y, settings.Wind.Z, 0f))
            .Set("clock", new Vector4(seconds, rain.Step, rain.Frame, rain.Capacity))
            .SetTexture("roof_heights", rain.Roofs?.Heights ?? NoRoofs())
            .SetBuffer("roof_frame", rain.Roofs?.Frame ?? NoFrame());
        rain.Splashes.Set("clock", new Vector4(seconds, SplashLife, 0f, 0f));
        rain.Drops.Set("look", new Vector4(DropShutter, FlakeShutter, settings.Reach, 0f));

        // The clouds' shadow where the camera's clouds cast one, which dims the sun on the drops.
        if (Clouds.ShadowFor(camera) is { } shadow)
        {
            rain.Simulate.SetTexture("cloud_shadow", shadow.Image)
                .Set("cloud_frame", new Vector4(shadow.Middle.X, shadow.Middle.Y, shadow.Middle.Z, shadow.Half))
                .Set("cloud_across", new Vector4(shadow.Across.X, shadow.Across.Y, shadow.Across.Z, 1f))
                .Set("cloud_up", new Vector4(shadow.Up.X, shadow.Up.Y, shadow.Up.Z, 0f));
        }
        else
        {
            rain.Simulate.SetTexture("cloud_shadow", NoShadow()).Set("cloud_across", Vector4.Zero);
        }
    }

    internal static void Clear(Entity camera)
    {
        ForgetOtherApps();
        if (!Cameras.Remove(camera.Bits, out var rain)) return;
        if (rain.Roofs is { } roofs) Drop(roofs);
        Shaders.SetEngineViewDraws(camera, Owner);
        Shaders.SetEngineViewDispatches(camera, Owner);
    }

    /// <summary>The cameras with rain or snow in the running app, with the settings each was given.</summary>
    internal static IEnumerable<(Entity Camera, PrecipitationSettings Settings)> All()
    {
        ForgetOtherApps();
        return Cameras.Select(rain => (new Entity(rain.Key), rain.Value.Settings));
    }

    /// <summary>A camera's counts, the drops' draw's four and the splashes' next slot, for a test to read.</summary>
    internal static AssetHandle CountsOf(Entity camera)
    {
        ForgetOtherApps();
        return Cameras.TryGetValue(camera.Bits, out var rain) ? rain.Counts : AssetHandle.None;
    }

    /// <summary>Whether every camera's programs have compiled and its map of what stands over the ground is drawn, so a frame draws the rain.</summary>
    internal static bool Ready()
    {
        ForgetOtherApps();
        return Cameras.Values.All(rain =>
            new[] { rain.Clear, rain.Simulate, rain.Drops, rain.Splashes }.All(i => i.Program.State == ShaderProgramState.Ready)
            && (!rain.Settings.Roofs || rain.Roofs is { Middle: not null, FramesLeft: 0 } && rain.Roofs.Map.Program.State == ShaderProgramState.Ready));
    }

    /// <summary>
    /// Moves every camera's rain on a frame, drawing its map of what stands over the ground again
    /// where the camera has moved far enough from where it was drawn.
    /// </summary>
    internal static void Tick(World world)
    {
        ForgetOtherApps();
        var ecs = world.Resource<EcsWorld>();
        foreach (var dropped in Dropped)
            if (ecs.IsAlive(dropped)) ecs.Despawn(dropped);
        Dropped.Clear();
        if (Cameras.Count == 0) return;

        var delta = world.Resource<Time>().Delta;
        foreach (var (bits, rain) in Cameras)
        {
            var camera = new Entity(bits);
            rain.Frame++;
            rain.Step = Math.Clamp(delta, 0f, 0.1f);
            rain.Seconds += rain.Step;
            Roof(ecs, camera, rain);
            Feed(camera, rain);
        }
    }

    // Draws the map of what stands over the ground about the camera where it has none yet or the
    // camera has moved an eighth of the map's width from where it was drawn, its middle moved by
    // whole steps of eight texels, for a few frames once pipelines are ready, and stops its camera
    // drawing between.
    private static void Roof(EcsWorld ecs, Entity camera, Camera rain)
    {
        var settings = rain.Settings;
        if (rain.Roofs is { } kept && (!settings.Roofs || kept.Layers != settings.RoofLayers))
        {
            Drop(kept);
            rain.Roofs = null;
        }
        if (!settings.Roofs || !ecs.IsAlive(camera)) return;

        var roofs = rain.Roofs ??= MakeRoofs(settings.RoofLayers);
        var eye = ecs.GetOrDefault<GlobalTransform>(camera).Translation;
        if (roofs.Middle is not { } middle
            || MathF.Abs(middle.X - eye.X) > RoofSize / 8f
            || MathF.Abs(middle.Z - eye.Z) > RoofSize / 8f
            || MathF.Abs(middle.Y - eye.Y) > RoofHeight / 3f)
        {
            var step = RoofSize / RoofTexels * 8f;
            var snapped = new Vec3(MathF.Round(eye.X / step) * step, eye.Y, MathF.Round(eye.Z / step) * step);
            ecs.Set(roofs.Camera, Transform.LookingAt(snapped + new Vec3(0f, RoofHeight, 0f), snapped, new Vec3(0f, 0f, -1f)));
            roofs.Middle = snapped;
            roofs.FramesLeft = 3;
        }

        var ready = Render.PipelinesReady();
        var drawing = roofs.FramesLeft > 0 || !ready;
        if (ready && roofs.FramesLeft > 0) roofs.FramesLeft--;
        if (drawing == roofs.Active) return;
        ecs.Wrap<CameraRef>(roofs.Camera).IsActive = drawing;
        roofs.Active = drawing;
    }

    // The camera looking down at what stands over the ground, drawing the layers given into a small
    // image of its own with no multisampling, so its depth reads, and the dispatch turning that depth
    // into heights.
    private static Roofs MakeRoofs(uint layers)
    {
        var camera = Render.SpawnCamera3d(new CameraSettings
        {
            Projection = CameraProjection.Orthographic,
            Height = RoofSize,
            Order = -100,
            Layers = layers,
        });
        Render.SetCameraTarget(camera, Render.CreateTarget(RoofTexels, RoofTexels));
        Render.SetPostProcessing(camera, new PostSettings { Msaa = 1, Tonemapper = Tonemapper.None, Dither = false });

        var heights = Shaders.CreateImage(RoofTexels, RoofTexels, ShaderImageFormat.R32Float);
        var frame = Shaders.CreateBuffer<Vector4>([Vector4.Zero]);
        var map = Shaders.CreateInstance(SceneField.KernelProgram("precipitation.slang", entry: "roofs"))
            .SetTexture("roof_map", heights)
            .SetBuffer("roof_map_frame", frame)
            .Set("roof_size", new Vector4(RoofSize, RoofTexels, 0f, 0f));
        Shaders.SetEnginePrepass(camera, Owner, 1u);
        Shaders.SetEngineViewDispatches(camera, Owner, ViewDispatch.Fixed(map, FramePoint.AfterPrepass, RoofTexels / 8, RoofTexels / 8));
        return new Roofs { Camera = camera, Map = map, Heights = heights, Frame = frame, Layers = layers };
    }

    // Lets a roofs camera go, its lists taken off it now and the camera despawned at the next tick.
    private static void Drop(Roofs roofs)
    {
        Shaders.SetEngineViewDispatches(roofs.Camera, Owner);
        Shaders.SetEnginePrepass(roofs.Camera, Owner, 0);
        Dropped.Add(roofs.Camera);
    }

    // What a camera with no map, or no clouds' shadow, is bound, which it never reads.
    private static AssetHandle NoRoofs()
    {
        if (!_noRoofs.IsValid) _noRoofs = Shaders.CreateImage(1, 1, ShaderImageFormat.R32Float);
        return _noRoofs;
    }

    private static AssetHandle NoFrame()
    {
        if (!_noFrame.IsValid) _noFrame = Shaders.CreateBuffer<Vector4>([Vector4.Zero]);
        return _noFrame;
    }

    private static AssetHandle NoShadow()
    {
        if (!_noShadow.IsValid) _noShadow = Shaders.CreateImage(1, 1, ShaderImageFormat.Rgba8);
        return _noShadow;
    }

    private static void ForgetOtherApps()
    {
        if (ReferenceEquals(_app, App.Running)) return;
        _app = App.Running;
        Cameras.Clear();
        Dropped.Clear();
        _noShadow = AssetHandle.None;
        _noRoofs = AssetHandle.None;
        _noFrame = AssetHandle.None;
    }
}

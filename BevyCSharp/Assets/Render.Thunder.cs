using System.Numerics;
using Bevy.Interop;
using Bevy.Reflected;

namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Brings a thunderstorm about a camera, lightning striking as <paramref name="settings"/> says,
    /// or with <see langword="null"/> ends it. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The strikes are timed and placed on the CPU every frame from the game's clock. A strike's
    /// flash lights the scene through a point light at its bolt, which the fog in froxels takes as
    /// it takes any lamp, lights the camera's clouds from within (<see cref="SetClouds"/>), and
    /// draws its bolt added to the picture after temporal antialiasing, from the clouds' base, or a
    /// kilometer and a half up where the camera has no clouds, to the ground at the camera's height
    /// less two meters. The thunder is written into a stream of samples as it plays (see
    /// <see cref="Audio.CreateSampleStream"/>), where the app has audio, and none is heard where it
    /// has none.
    /// </para>
    /// <para>
    /// Settings given again every frame, as <see cref="SetWeather"/> gives them, keep the storm's
    /// strikes and clock and take the numbers alone.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="settings">How the storm strikes, or null to end it.</param>
    /// <exception cref="ArgumentOutOfRangeException">The rate, the brightness or the volume is under none or not a number, or the nearest strike is not nearer than the farthest.</exception>
    /// <example>
    /// <code>
    /// Render.SetThunder(camera, new ThunderSettings { Rate = 4f, Struck = strike => Shake(strike.Distance) });
    /// </code>
    /// </example>
    public static void SetThunder(Entity camera, ThunderSettings? settings)
    {
        if (settings is null)
        {
            SkyThunder.Clear(camera);
            return;
        }

        float[] numbers = [settings.Rate, settings.Brightness, settings.Volume];
        if (!numbers.All(n => float.IsFinite(n) && n >= 0f))
            throw new ArgumentOutOfRangeException(nameof(settings), "The rate, the brightness or the volume is under none or not a number.");
        if (!(settings.Nearest > 0f) || !(settings.Farthest > settings.Nearest) || !float.IsFinite(settings.Farthest))
            throw new ArgumentOutOfRangeException(nameof(settings), "A strike lands nearer than its farthest and past none.");

        SkyThunder.Set(camera, settings);
    }

    /// <summary>
    /// Strikes lightning about a camera with a thunderstorm at once, at a place on the ground or
    /// somewhere between its storm's nearest and farthest. Only valid inside a system.
    /// </summary>
    /// <remarks>For a storm a game times itself, a strike at a story's turn or at a tower.</remarks>
    /// <param name="camera">A camera given a storm by <see cref="SetThunder"/>.</param>
    /// <param name="at">Where on the ground the bolt lands, or none for the storm to choose. A height not a number lands it at the camera's height less two meters.</param>
    /// <returns>The strike, or none where the camera has no storm.</returns>
    public static LightningStrike? StrikeLightning(Entity camera, Vec3? at = null) => SkyThunder.Strike(camera, at, toGround: true);
}

/// <summary>The thunderstorms <see cref="Render.SetThunder"/> brings about cameras, an app's own.</summary>
internal static class SkyThunder
{
    // Drawn before the rain, its owner's name sorting before the rain's, so the drops in front of a
    // bolt are over it.
    internal const string Owner = "thunder";

    private const float FlashLife = 0.55f;
    private const float SpeedOfSound = 343f;
    private const float GlowRadius = 600f;
    private const float CoreLight = 1e7f;
    private static readonly Vector3 FlashColor = new(0.82f, 0.88f, 1f);

    private static App? _app;
    private static readonly Dictionary<ulong, Storm> Storms = [];
    private static readonly List<Entity> Dropped = [];
    // Streams the thunder is written into, kept for the next clap once one is heard out, and whether
    // the app has audio to play them.
    private static readonly Stack<AssetHandle> Streams = [];
    private static bool _soundless;

    private sealed class Storm
    {
        internal required ThunderSettings Settings { get; set; }
        internal required Random Random { get; init; }
        internal float NextIn { get; set; } = float.NaN;
        internal Flash? Flash { get; set; }
        internal Entity Light { get; set; } = Entity.None;
        internal float Shown { get; set; }
        internal ShaderInstance Bolt { get; set; }
        internal AssetHandle Segments { get; set; } = AssetHandle.None;
        internal int SegmentCount { get; set; }
        internal int Strikes { get; set; }
        internal List<Clap> Claps { get; } = [];
    }

    // A strike's flash as it flickers, where it landed and where its glow and light are.
    private sealed class Flash
    {
        internal required Vec3 Ground { get; init; }
        internal required Vec3 Glow { get; init; }
        internal required bool ToGround { get; init; }
        internal required uint Seed { get; init; }
        internal float Elapsed { get; set; }
        internal float Strength => Envelope(Elapsed, FlashLife, Seed);
    }

    // A clap on its way, the seconds until it is heard, then the stream it is written into and the
    // entity playing it.
    private sealed class Clap
    {
        internal required ThunderSound Sound { get; init; }
        internal required string? Bus { get; init; }
        internal float Delay { get; set; }
        internal AssetHandle Stream { get; set; } = AssetHandle.None;
        internal Entity Playing { get; set; } = Entity.None;
    }

    internal static void Set(Entity camera, ThunderSettings settings)
    {
        ForgetOtherApps();
        if (!Storms.TryGetValue(camera.Bits, out var storm))
        {
            storm = new Storm { Settings = settings, Random = new Random((int)(settings.Seed % int.MaxValue)) };
            Storms[camera.Bits] = storm;
        }
        storm.Settings = settings;

        // The bolt's program made with the storm, so it has compiled by the first strike, whose
        // flash is over in half a second.
        if (settings.Bolts && !storm.Bolt.IsValid) storm.Bolt = SceneField.Draw("lightning_bolt.slang");
    }

    /// <summary>Whether every storm's bolt has compiled, so a strike draws it.</summary>
    internal static bool Ready()
    {
        ForgetOtherApps();
        return Storms.Values.All(storm => !storm.Bolt.IsValid || storm.Bolt.Program.State == ShaderProgramState.Ready);
    }

    internal static void Clear(Entity camera)
    {
        ForgetOtherApps();
        if (!Storms.Remove(camera.Bits, out var storm)) return;
        if (!storm.Light.IsNone) Dropped.Add(storm.Light);
        Shaders.SetEngineViewDraws(camera, Owner);
        foreach (var clap in storm.Claps) Hush(clap);
    }

    /// <summary>The cameras with a storm in the running app, with the settings each was given.</summary>
    internal static IEnumerable<(Entity Camera, ThunderSettings Settings)> All()
    {
        ForgetOtherApps();
        return Storms.Select(storm => (new Entity(storm.Key), storm.Value.Settings));
    }

    /// <summary>The glow of the strike flashing in a camera's clouds this frame, its middle and its light, or none.</summary>
    internal static (Vec3 Middle, float Light, Vector3 Color, float Radius)? GlowFor(Entity camera)
    {
        ForgetOtherApps();
        if (!Storms.TryGetValue(camera.Bits, out var storm) || storm.Flash is not { } flash) return null;
        var strength = flash.Strength;
        if (strength <= 0f) return null;
        // What a cloud lit evenly from the strike's light a glow's width off sends back, white
        // scattering it all.
        var light = storm.Settings.Brightness / (4f * MathF.PI * GlowRadius * GlowRadius) / MathF.PI;
        return (flash.Glow, light * strength, FlashColor, GlowRadius);
    }

    /// <summary>Strikes at once about a camera with a storm, at a place or where the storm chooses.</summary>
    internal static LightningStrike? Strike(Entity camera, Vec3? at, bool toGround)
    {
        ForgetOtherApps();
        if (!Storms.TryGetValue(camera.Bits, out var storm) || App.Running is not { } app) return null;
        return Begin(app.World.Resource<EcsWorld>(), camera, storm, at, toGround);
    }

    /// <summary>
    /// Moves every storm on a frame, striking where its time has come, flickering the flash, its
    /// light and its bolt, and writing the thunder that has arrived.
    /// </summary>
    internal static void Tick(World world)
    {
        ForgetOtherApps();
        var ecs = world.Resource<EcsWorld>();
        foreach (var dropped in Dropped)
            if (ecs.IsAlive(dropped)) ecs.Despawn(dropped);
        Dropped.Clear();
        if (Storms.Count == 0) return;

        var delta = world.Resource<Time>().Delta;
        foreach (var (bits, storm) in Storms)
        {
            var camera = new Entity(bits);
            if (!ecs.IsAlive(camera)) continue;

            // The next strike's time, a gap from an exponential spread, so the strikes are a Poisson
            // process, as many a minute on average with no beat to them.
            var rate = storm.Settings.Rate;
            if (rate <= 0f) storm.NextIn = float.NaN;
            else if (float.IsNaN(storm.NextIn)) storm.NextIn = Gap(rate, storm.Random);
            else
            {
                storm.NextIn -= delta;
                if (storm.NextIn <= 0f)
                {
                    Begin(ecs, camera, storm, null, storm.Random.NextSingle() < 2f / 3f);
                    storm.NextIn = Gap(rate, storm.Random);
                }
            }

            Flicker(ecs, camera, storm, delta);
            Hear(storm, delta);
        }
    }

    /// <summary>
    /// The brightness of a strike's flash, from 0 to 1, so far through it, a fast rise and a fall with
    /// the flicker of two or three return strokes at the seed's times on top.
    /// </summary>
    internal static float Envelope(float elapsed, float duration, uint seed)
    {
        if (duration <= 0f || elapsed < 0f || elapsed > duration) return 0f;
        var t = elapsed / duration;
        var rise = Math.Min(t / 0.04f, 1f);
        var fall = MathF.Exp(-4.5f * t);
        var flicker = 1f;
        var random = new Random((int)(seed % int.MaxValue));
        for (var stroke = 0; stroke < 3; stroke++)
        {
            var d = (t - (0.12f + (random.NextSingle() * 0.55f))) / 0.05f;
            flicker += 0.9f * MathF.Exp(-d * d);
        }
        return Math.Clamp(rise * fall * flicker, 0f, 1f);
    }

    /// <summary>A gap until the next strike at a rate a minute, from an exponential spread.</summary>
    internal static float Gap(float rate, Random random) =>
        -MathF.Log(Math.Clamp(random.NextSingle(), 1e-6f, 1f - 1e-6f)) / (rate / 60f);

    // Begins a strike about the camera, at a place or at a distance spread as evenly over the ground
    // as rings of it grow, and tells the game.
    private static LightningStrike Begin(EcsWorld ecs, Entity camera, Storm storm, Vec3? at, bool toGround)
    {
        var settings = storm.Settings;
        var eye = ecs.GetOrDefault<GlobalTransform>(camera).Translation;
        var cloudBase = Clouds.All().FirstOrDefault(c => c.Camera == camera).Settings?.Bottom ?? 1500f;
        Vec3 ground;
        if (at is { } given)
        {
            ground = float.IsNaN(given.Y) ? new Vec3(given.X, eye.Y - 2f, given.Z) : given;
        }
        else
        {
            var (near, far) = (settings.Nearest, settings.Farthest);
            var reach = MathF.Sqrt((near * near) + (storm.Random.NextSingle() * ((far * far) - (near * near))));
            var angle = storm.Random.NextSingle() * MathF.Tau;
            ground = new Vec3(eye.X + (MathF.Cos(angle) * reach), eye.Y - 2f, eye.Z + (MathF.Sin(angle) * reach));
        }

        var seed = (settings.Seed * 2654435761u) + (uint)storm.Strikes++;
        var top = new Vec3(ground.X, cloudBase + 200f, ground.Z);
        var glow = new Vec3(ground.X, cloudBase + 300f, ground.Z);
        storm.Flash = new Flash { Ground = ground, Glow = glow, ToGround = toGround, Seed = seed };

        // The bolt's segments into a buffer of their own for the camera's draw, two numbers each.
        if (toGround && settings.Bolts)
        {
            var segments = LightningBolt.Grow(top, ground, seed);
            var numbers = new Vector4[segments.Count * 2];
            for (var i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                numbers[i * 2] = new Vector4(s.From.X, s.From.Y, s.From.Z, s.Width);
                numbers[(i * 2) + 1] = new Vector4(s.To.X, s.To.Y, s.To.Z, s.Brightness);
            }
            if (!storm.Bolt.IsValid) storm.Bolt = SceneField.Draw("lightning_bolt.slang");
            if (!storm.Segments.IsValid || Shaders.BufferSize(storm.Segments) < numbers.Length * 16)
                storm.Segments = Shaders.CreateBuffer<Vector4>(numbers, Math.Max(numbers.Length, 1024) * 16);
            else
                Shaders.WriteBuffer<Vector4>(storm.Segments, numbers);
            storm.Bolt.SetBuffer("segments", storm.Segments);
            storm.SegmentCount = segments.Count;
            Shaders.SetEngineViewDraws(camera, Owner,
                ViewDraw.Fixed(storm.Bolt, FramePoint.AfterTemporalAntialiasing, 6, (uint)segments.Count, DrawBlend.Add, writesDepth: false));
        }
        else
        {
            Shaders.SetEngineViewDraws(camera, Owner);
        }

        // The light at the bolt, low on it so the ground about it is lit from above and aside.
        if (settings.Light)
        {
            var lit = toGround ? ground + new Vec3(0f, Math.Min(200f, (cloudBase - ground.Y) * 0.3f), 0f) : glow;
            if (storm.Light.IsNone || !ecs.IsAlive(storm.Light))
            {
                storm.Light = Render.SpawnLight(new LightSettings
                {
                    Kind = LightKind.Point,
                    Intensity = 0f,
                    Range = 3_000f,
                    Shadows = false,
                    Color = (FlashColor.X, FlashColor.Y, FlashColor.Z),
                });
            }
            ecs.Set(storm.Light, Transform.At(lit.X, lit.Y, lit.Z));
        }

        var distance = (new Vec3(ground.X, eye.Y, ground.Z) - eye).Length;
        var strike = new LightningStrike(toGround ? ground : glow, distance, distance / SpeedOfSound, toGround);
        if (settings.Sound && !_soundless)
            storm.Claps.Add(new Clap { Sound = new ThunderSound(Math.Max(distance, settings.Nearest), settings.Volume, seed), Bus = settings.Bus, Delay = strike.ThunderDelay });
        settings.Struck?.Invoke(strike);
        return strike;
    }

    // The flash's flicker on its light and its bolt, and the strike's end once it has died away.
    private static void Flicker(EcsWorld ecs, Entity camera, Storm storm, float delta)
    {
        if (storm.Flash is not { } flash) return;
        flash.Elapsed += delta;
        var strength = flash.Strength;
        if (flash.Elapsed > FlashLife)
        {
            storm.Flash = null;
            strength = 0f;
            Shaders.SetEngineViewDraws(camera, Owner);
        }

        // Through Bevy's reflected light, and only where it changes, so a light at rest between
        // strikes costs nothing.
        var shown = storm.Settings.Light ? storm.Settings.Brightness * strength : 0f;
        if (!storm.Light.IsNone && ecs.IsAlive(storm.Light) && shown != storm.Shown)
        {
            ecs.Wrap<PointLightRef>(storm.Light).Intensity = shown;
            storm.Shown = shown;
        }

        if (storm.Bolt.IsValid)
        {
            var cloudBase = Clouds.All().FirstOrDefault(c => c.Camera == camera).Settings?.Bottom ?? 1500f;
            var core = FlashColor * CoreLight * strength;
            storm.Bolt.Set("bolt", new Vector4(core.X, core.Y, core.Z, cloudBase));
        }
    }

    // Counts each clap down to when it is heard, then plays it into a stream and writes it a sixth
    // of a second ahead each frame, until it has all been heard.
    private static void Hear(Storm storm, float delta)
    {
        for (var i = storm.Claps.Count - 1; i >= 0; i--)
        {
            var clap = storm.Claps[i];
            try
            {
                if (clap.Playing.IsNone)
                {
                    clap.Delay -= delta;
                    if (clap.Delay > 0f) continue;
                    clap.Stream = Streams.Count > 0 ? Streams.Pop() : Audio.CreateSampleStream(ThunderSound.SampleRate);
                    Write(clap);
                    clap.Playing = Audio.Play(clap.Stream, new AudioSettings { Bus = clap.Bus });
                    continue;
                }

                Write(clap);
                if (clap.Sound.Done && Audio.QueuedSamples(clap.Stream) == 0)
                {
                    Hush(clap);
                    storm.Claps.RemoveAt(i);
                }
            }
            catch (BevyNativeException)
            {
                // The app has no audio, so no storm in it is heard.
                _soundless = true;
                storm.Claps.Clear();
                return;
            }
        }
    }

    // Writes a clap's next samples where the stream holds under a sixth of a second.
    private static void Write(Clap clap)
    {
        var missing = (ThunderSound.SampleRate / 6) - Audio.QueuedSamples(clap.Stream);
        if (missing <= 0 || clap.Sound.Done) return;
        var samples = new float[missing];
        var made = clap.Sound.Fill(samples);
        if (made > 0) Audio.QueueSamples(clap.Stream, samples.AsSpan(0, made));
    }

    // Stops a clap and keeps its stream for the next.
    private static void Hush(Clap clap)
    {
        if (!clap.Playing.IsNone)
        {
            Audio.Stop(clap.Playing);
            clap.Playing = Entity.None;
        }
        if (clap.Stream.IsValid) Streams.Push(clap.Stream);
        clap.Stream = AssetHandle.None;
    }

    private static void ForgetOtherApps()
    {
        if (ReferenceEquals(_app, App.Running)) return;
        _app = App.Running;
        Storms.Clear();
        Dropped.Clear();
        Streams.Clear();
        _soundless = false;
    }
}

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The time of day, which carries the sun and a moon across the sky as two directional lights,
/// their light, the ambient light and the stars following the hour.
/// </summary>
/// <remarks>
/// <para>
/// The hour runs on from the one the settings hold at <see cref="FeatureSettings.DaySpeed"/> hours
/// a minute of the game's clock, and the panel's Time page and <c>day.hour</c> set it. Where the
/// sun stands is worked out for the equinox at <see cref="FeatureSettings.Latitude"/>, so it rises
/// at six and sets at eighteen anywhere but at the poles, climbing as high as the latitude lets it
/// at noon. The moon is full, opposite the sun and half an hour behind it, so it rises as the sun
/// sets. North is along negative Z, the way the light hall stands from the hub, and east along
/// positive X.
/// </para>
/// <para>
/// Bevy's atmosphere scatters the light of every directional light and draws each one's disk, so
/// the moon's disk and the pale blue of a moonlit sky come from it with nothing drawn here, and
/// <see cref="Render.SetSkyLighting"/> lights the scene from that sky as it changes. The atmosphere
/// also dims and reddens a low sun's light on whatever it falls on, so the sun's color curve warms
/// it a little only. The lights' strengths and colors are curves by the hour, each light's
/// strength brought to nothing as it reaches the horizon, and the one that is up casts the shadows,
/// so a night costs no more shadow views than a day.
/// </para>
/// <para>
/// The stars are a cubemap drawn here (<see cref="Stars"/>), behind the atmosphere, which draws
/// over them with as much of them as the air lets through, so they fade toward the horizon. Their
/// brightness is a curve by the hour that is nothing by day, and they turn about the pole star with
/// the hour, as the sun does. Under the dusk backdrop the sky stays the dusk one, and the sun and
/// the moon go on moving.
/// </para>
/// </remarks>
[Behavior]
public partial struct DayNight
{
    /// <summary>A light's strength and color at an hour, one key of a curve.</summary>
    private readonly record struct Key(float Hour, float Light, float R, float G, float B);

    /// <summary>The sun's illuminance in lux and its color, by the hour.</summary>
    private static readonly Key[] Sunlight =
    [
        new(0f, 0f, 1f, 0.6f, 0.4f),
        new(5.75f, 0f, 1f, 0.6f, 0.4f),
        new(6.5f, 2_500f, 1f, 0.78f, 0.6f),
        new(8f, 9_000f, 1f, 0.9f, 0.78f),
        new(10.5f, 12_500f, 1f, 0.96f, 0.88f),
        new(13.5f, 12_500f, 1f, 0.96f, 0.88f),
        new(16f, 9_000f, 1f, 0.9f, 0.78f),
        new(17.5f, 2_500f, 1f, 0.78f, 0.6f),
        new(18.25f, 0f, 1f, 0.6f, 0.4f),
        new(24f, 0f, 1f, 0.6f, 0.4f),
    ];

    /// <summary>The moon's illuminance in lux and its color, by the hour.</summary>
    /// <remarks>
    /// Far brighter than a real moon, which lights a scene a few thousand times more dimly than
    /// this, so a night here can still be walked in by a tester.
    /// </remarks>
    private static readonly Key[] Moonlight =
    [
        new(0f, 900f, 0.62f, 0.72f, 1f),
        new(5.5f, 900f, 0.62f, 0.72f, 1f),
        new(6.5f, 0f, 0.62f, 0.72f, 1f),
        new(17.75f, 0f, 0.62f, 0.72f, 1f),
        new(18.75f, 900f, 0.62f, 0.72f, 1f),
        new(24f, 900f, 0.62f, 0.72f, 1f),
    ];

    /// <summary>The ambient light's brightness and color, by the hour.</summary>
    private static readonly Key[] Ambient =
    [
        new(0f, 20f, 0.55f, 0.65f, 1f),
        new(5.5f, 20f, 0.55f, 0.65f, 1f),
        new(7f, 80f, 1f, 1f, 1f),
        new(17f, 80f, 1f, 1f, 1f),
        new(18.5f, 20f, 0.55f, 0.65f, 1f),
        new(24f, 20f, 0.55f, 0.65f, 1f),
    ];

    /// <summary>
    /// The stars' brightness, in the skybox's candelas a square meter, by the hour.
    /// </summary>
    private static readonly Key[] Starlight =
    [
        new(0f, 1_200f, 1f, 1f, 1f),
        new(5f, 1_200f, 1f, 1f, 1f),
        new(6.25f, 0f, 1f, 1f, 1f),
        new(17.75f, 0f, 1f, 1f, 1f),
        new(19f, 1_200f, 1f, 1f, 1f),
        new(24f, 1_200f, 1f, 1f, 1f),
    ];

    private static float _hour = 8f;
    private static Entity _moon = Entity.None;
    private static AssetHandle _stars = AssetHandle.None;

    /// <summary>
    /// What was last given each light, so a light is written only where it changed.
    /// </summary>
    private static readonly Dictionary<Entity, (float Light, Color Color, bool Shadows)> Given = [];

    private static (float Light, Color Color) _ambient = (-1f, default);

    /// <summary>The hour of the day now, from midnight at zero.</summary>
    internal static float Hour => _hour;

    /// <summary>
    /// Starts the day at the hour the settings hold, and puts the moon in the sky.
    /// </summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        _hour = Wrap(Settings.Current.Hour);
        _moon = Entity.None;
        _stars = AssetHandle.None;
        _ambient = (-1f, default);
        Given.Clear();
        Settings.Changed -= OnChanged;
        Settings.Changed += OnChanged;

        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        _moon = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Directional,
            Intensity = 0f,
            Color = (0.62f, 0.72f, 1f),
            // Solari traces the shadows itself, as it does the sun's.
            Shadows = !Render.RayTracingActive,
        });
        ctx.Ecs.SetName(_moon, "Moon");
        Turn(ctx);
    }

    /// <summary>Moves the day on, and the lights and the stars with it.</summary>
    [OnUpdate]
    public static void Turn(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var settings = Settings.Current;
        _hour = Wrap(_hour + (settings.DaySpeed * ctx.Time.Delta / 60f));

        var sun = Toward(_hour, settings.Latitude, 0f);
        var moon = Toward(_hour + 12.5f, settings.Latitude, -0.08f);
        var shadows = settings.Shadows && !Render.RayTracingActive;

        Shine(ctx.Ecs, Scene.Sun, sun, At(Sunlight, _hour), shadows && sun.Y > 0f);
        Shine(ctx.Ecs, _moon, moon, At(Moonlight, _hour), shadows && sun.Y <= 0f && moon.Y > 0f);

        var (light, color) = At(Ambient, _hour);
        if (MathF.Abs(light - _ambient.Light) > 0.1f)
        {
            Render.SetAmbientLight((color.R, color.G, color.B), light);
            _ambient = (light, color);
        }

        TurnStars(ctx.Ecs, settings.Latitude);
    }

    /// <summary>
    /// Sets the hour, which the day runs on from, as the panel and the console do.
    /// </summary>
    internal static void SetHour(float hour)
    {
        _hour = Wrap(hour);
        Settings.Change(s => s with { Hour = _hour });
    }

    /// <summary>Sets the hour, or says it and where the sun and the moon stand.</summary>
    [Command("day.hour", "Sets the hour of the day, from midnight at zero, or says it and where the sun and the moon stand: day.hour [hour]")]
    internal static string HourCommand(string hour)
    {
        if (hour.Trim().Length > 0)
        {
            if (!float.TryParse(hour, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var asked) || asked is < 0f or > 24f)
            {
                ConsoleHost.Fail("BAD_VALUE", $"{hour.Trim()} is not an hour from 0 to 24.");
                return "day.hour [hour]";
            }

            SetHour(asked);
        }

        var latitude = Settings.Current.Latitude;
        var sun = Toward(_hour, latitude, 0f);
        var moon = Toward(_hour + 12.5f, latitude, -0.08f);
        var minutes = (int)MathF.Round(_hour * 60f) % (24 * 60);
        return $"{minutes / 60:00}:{minutes % 60:00}, the sun {Elevation(sun):0} degrees and the moon {Elevation(moon):0} degrees above the horizon";
    }

    /// <summary>
    /// Jumps to an hour set from elsewhere than <see cref="SetHour"/>, as the setting command sets
    /// one.
    /// </summary>
    private static void OnChanged(FeatureSettings was)
    {
        if (Settings.Current.Hour != was.Hour) _hour = Wrap(Settings.Current.Hour);
    }

    /// <summary>
    /// The direction toward a body of the sky at an hour, for a latitude and the body's declination
    /// in radians, from the hour angle by the usual spherical astronomy.
    /// </summary>
    private static Vec3 Toward(float hour, float latitude, float declination)
    {
        var angle = (Wrap(hour) - 12f) * MathF.PI / 12f;
        var phi = latitude * MathF.PI / 180f;

        var east = -MathF.Cos(declination) * MathF.Sin(angle);
        var north = (MathF.Sin(declination) * MathF.Cos(phi)) - (MathF.Cos(declination) * MathF.Cos(angle) * MathF.Sin(phi));
        var up = (MathF.Sin(declination) * MathF.Sin(phi)) + (MathF.Cos(declination) * MathF.Cos(angle) * MathF.Cos(phi));
        return new Vec3(east, up, -north).Normalized;
    }

    /// <summary>How high a direction stands above the horizon, in degrees.</summary>
    private static float Elevation(Vec3 direction) => MathF.Asin(Math.Clamp(direction.Y, -1f, 1f)) * 180f / MathF.PI;

    /// <summary>
    /// Points a light from a direction and gives it its strength and color, brought to nothing at
    /// the horizon, writing each only where it changed.
    /// </summary>
    private static void Shine(EcsWorld ecs, Entity light, Vec3 toward, (float Light, Color Color) given, bool shadows)
    {
        if (light == Entity.None || !ecs.IsAlive(light)) return;

        // Looking at the scene from where the light comes from, with an up that is never along it.
        var up = MathF.Abs(toward.Y) > 0.99f ? Vec3.UnitZ : Vec3.UnitY;
        ecs.Set(light, Transform.LookingAt(toward, Vec3.Zero, up));

        var strength = given.Light * Math.Clamp(toward.Y / 0.05f, 0f, 1f);
        if (Given.TryGetValue(light, out var was)
            && MathF.Abs(was.Light - strength) <= MathF.Max(0.5f, strength * 0.002f)
            && was.Color == given.Color
            && was.Shadows == shadows) return;

        var wrapped = ecs.Wrap<DirectionalLightRef>(light);
        wrapped.Illuminance = strength;
        wrapped.Color = given.Color;
        wrapped.ShadowMapsEnabled = shadows;
        Given[light] = (strength, given.Color, shadows);
    }

    /// <summary>
    /// Keeps the stars' brightness and their turn about the pole star to the hour, where the
    /// camera's skybox is the stars, which <see cref="Applied"/> puts there under the atmosphere.
    /// </summary>
    private static void TurnStars(EcsWorld ecs, float latitude)
    {
        if (Scene.Camera is not { } camera || !ecs.IsAlive(camera) || !_stars.IsValid) return;
        if (ecs.Get<SkyboxRef>(camera) is not { } skybox || skybox.Image != _stars) return;

        // The sky turns west about the pole star, which stands as high as the latitude, a full
        // turn a day, so the stars set where the sun does.
        var phi = latitude * MathF.PI / 180f;
        var pole = new Vec3(0f, MathF.Sin(phi), -MathF.Cos(phi));
        skybox.Rotation = Quat.FromAxisAngle(pole, -(_hour - 12f) * MathF.PI / 12f);

        var brightness = At(Starlight, _hour).Light;
        if (MathF.Abs(skybox.Brightness - brightness) > 0.5f) skybox.Brightness = brightness;
    }

    /// <summary>
    /// The stars as a cubemap, drawn here a pixel at a time the first time they are asked for, on
    /// black, a faint band of the galaxy across them.
    /// </summary>
    /// <remarks>
    /// Each face's pixel is turned into the direction it is seen along, by the layout every cubemap
    /// shares, as the dusk sky's are (<see cref="Applied"/>). A star comes from a hash of the
    /// pixel, the same each run, most of them faint and a few bright, a little blue or a little
    /// yellow.
    /// </remarks>
    internal static AssetHandle Stars()
    {
        if (_stars.IsValid) return _stars;

        const int Size = 512;
        var band = new Vec3(0.3f, 0.55f, 0.78f).Normalized;
        var pixels = new byte[Size * Size * 6 * 4];
        for (var face = 0; face < 6; face++)
        {
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var u = (2f * (x + 0.5f) / Size) - 1f;
                    var v = (2f * (y + 0.5f) / Size) - 1f;
                    var direction = (face switch
                    {
                        0 => new Vec3(1f, -v, -u),
                        1 => new Vec3(-1f, -v, u),
                        2 => new Vec3(u, 1f, v),
                        3 => new Vec3(u, -1f, -v),
                        4 => new Vec3(u, -v, 1f),
                        _ => new Vec3(-u, -v, -1f),
                    }).Normalized;

                    var hash = (uint)((face * 73_856_093) ^ (y * 19_349_663) ^ (x * 83_492_791));
                    hash = (hash ^ (hash >> 13)) * 0x5bd1e995u;
                    hash ^= hash >> 15;

                    // The galaxy's band, faint and a little uneven, where the direction is near
                    // its plane.
                    var across = Vec3.Dot(direction, band);
                    var glow = 0.035f * MathF.Exp(-(across * across) / 0.02f) * (0.7f + (0.3f * ((hash >> 8) % 100) / 100f));
                    var (r, g, b) = (glow * 0.9f, glow * 0.92f, glow);

                    if (hash % 420 == 0)
                    {
                        var bright = MathF.Pow(((hash >> 9) % 1000) / 1000f, 4f);
                        var shine = 0.08f + (0.92f * bright);
                        (r, g, b) = ((hash >> 20) % 3) switch
                        {
                            0 => (shine * 0.8f, shine * 0.88f, shine),
                            1 => (shine, shine * 0.92f, shine * 0.75f),
                            _ => (shine, shine, shine),
                        };
                    }

                    var at = ((((face * Size) + y) * Size) + x) * 4;
                    (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) =
                        (Byte(r), Byte(g), Byte(b), 255);
                }
            }
        }

        _stars = Render.CreateImage(pixels, Size, Size * 6);
        Render.MakeCubemap(_stars);
        return _stars;
    }

    private static byte Byte(float value) => (byte)Math.Clamp(value * 255f, 0f, 255f);

    /// <summary>
    /// A curve's strength and color at an hour, straight between the keys either side.
    /// </summary>
    private static (float Light, Color Color) At(Key[] keys, float hour)
    {
        hour = Wrap(hour);
        for (var i = 1; i < keys.Length; i++)
        {
            if (hour > keys[i].Hour) continue;

            var (from, to) = (keys[i - 1], keys[i]);
            var t = to.Hour > from.Hour ? (hour - from.Hour) / (to.Hour - from.Hour) : 0f;
            return (
                from.Light + ((to.Light - from.Light) * t),
                new Color(from.R + ((to.R - from.R) * t), from.G + ((to.G - from.G) * t), from.B + ((to.B - from.B) * t)));
        }

        var last = keys[^1];
        return (last.Light, new Color(last.R, last.G, last.B));
    }

    /// <summary>An hour brought into the day, from zero up to twenty four.</summary>
    private static float Wrap(float hour) => ((hour % 24f) + 24f) % 24f;
}

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
/// sun, the moon and the stars stand is the engine's sky clock's (<see cref="SkyClock"/>), at the
/// March equinox at <see cref="FeatureSettings.Latitude"/>, so the sun rises near six and sets near
/// eighteen, and the moon starts the day nearly full, rising as the sun sets, waxing and waning as
/// the days go. North is along negative Z, the way the light hall stands from the hub, and east
/// along positive X.
/// </para>
/// <para>
/// Bevy's atmosphere scatters the light of every directional light, so the pale blue of a moonlit
/// sky comes from it, and <see cref="Render.SetSkyLighting"/> lights the scene from that sky as it
/// changes. The moon's own disc, with its phase, is the night sky's, so its light draws none. The atmosphere
/// also dims and reddens a low sun's light on whatever it falls on, so the sun's color curve warms
/// it a little only. The lights' strengths and colors are curves by the hour, each light's
/// strength brought to nothing as it reaches the horizon, and the one that is up casts the shadows,
/// so a night costs no more shadow views than a day.
/// </para>
/// <para>
/// The stars, the galaxy and the moon are the engine's night sky (<see cref="Render.SetNightSky"/>),
/// which <see cref="Applied"/> gives the camera under the atmosphere with this clock, so they turn
/// about the pole star with the hours and come out as the sun goes down. The moon's light is its
/// curve by the hour times the share of its face the sun lights. Under the dusk backdrop the sky
/// stays the dusk one, and the sun and the moon go on moving.
/// </para>
/// <para>
/// Where the weather runs, the day hands it the sun and the moon, marked as its own, and its clock,
/// held still and set each frame to this hour and latitude, so the panel's Time page and
/// <c>day.hour</c> go on setting the hour. The earth's tilt is taken off its clock, so every day of
/// its calendar is the equinox and its sun stands where the one here would, rising at six. The
/// weather then steers the two lights, the ambient light and its own stars and moon, and the curves
/// here wait until it is gone.
/// </para>
/// </remarks>
[Behavior]
public partial struct DayNight
{
    /// <summary>A light's strength and color at an hour, one key of a curve.</summary>
    private readonly record struct Key(float Hour, float Light, float R, float G, float B);

    /// <summary>The sun above the air, the curve's noon strength in lux and white, which the atmosphere dims.</summary>
    private static readonly (float Light, Color Color) RawSun = (12_500f, new Color(1f, 0.98f, 0.95f));

    /// <summary>The sun's illuminance in lux and its color, by the hour, where no atmosphere dims it.</summary>
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

    // The sky's clock, at the March equinox, the moon half a day short of full at its start.
    private static readonly SkyClock Sky = new() { Hour = 8f, DayOfYear = 79f, MoonAgeAtStart = (SkyClock.SynodicMonth / 2f) - 79.5f };
    private static Entity _moon = Entity.None;

    /// <summary>
    /// What was last given each light, so a light is written only where it changed.
    /// </summary>
    private static readonly Dictionary<Entity, (float Light, Color Color, bool Shadows)> Given = [];

    private static (float Light, Color Color) _ambient = (-1f, default);

    /// <summary>The hour of the day now, from midnight at zero.</summary>
    internal static float Hour => Sky.Hour;

    /// <summary>The sky's clock, which the night sky stands by.</summary>
    internal static SkyClock Clock => Sky;

    /// <summary>
    /// Starts the day at the hour the settings hold, and puts the moon in the sky.
    /// </summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        Sky.Hour = Wrap(Settings.Current.Hour);
        Sky.DayOfYear = 79f;
        Sky.Latitude = Settings.Current.Latitude;
        _moon = Entity.None;
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

        // The weather's moon where it runs, which it steers from then on, so it makes none of its
        // own, and elsewhere a light with no disc, the night sky drawing the moon's.
        if (Weather.Active) ctx.Ecs.Insert<MoonLightRef>(_moon);
        else Render.SetSunDisk(_moon, SunDisk.Off);
        Turn(ctx);
    }

    /// <summary>Moves the day on, and the lights and the stars with it.</summary>
    [OnUpdate]
    public static void Turn(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var settings = Settings.Current;
        Sky.Latitude = settings.Latitude;
        Sky.Advance(settings.DaySpeed * ctx.Time.Delta / 60f);
        var hour = Sky.Hour;

        if (Weather.Active)
        {
            Hand(ctx.Ecs, settings.Latitude);
            return;
        }

        var sun = Sky.SunDirection;
        var moon = Sky.MoonDirection;
        var shadows = settings.Shadows && !Render.RayTracingActive;

        // Under the atmosphere the sun shines as it does above the air, its noon strength and white,
        // and Bevy's lighting dims and reddens it through the air the atmosphere describes, setting
        // it behind the planet, so the curve's reddening and its fade at the horizon, which would
        // count the air twice and leave the sunset no light to scatter, are kept for the dusk sky.
        var raw = Applied.AtmosphereDrawn;
        Shine(ctx.Ecs, Scene.Sun, sun, raw ? RawSun : At(Sunlight, hour), shadows && sun.Y > 0f, fade: !raw);
        var (moonlight, moonColor) = At(Moonlight, hour);
        Shine(ctx.Ecs, _moon, moon, (moonlight * Sky.MoonLit, moonColor), shadows && sun.Y <= 0f && moon.Y > 0f);

        var (light, color) = At(Ambient, hour);
        if (MathF.Abs(light - _ambient.Light) > 0.1f)
        {
            Render.SetAmbientLight((color.R, color.G, color.B), light);
            _ambient = (light, color);
        }

        // The look follows the sun's height, its numbers alone changing on the camera.
        if (settings.Look == Look.Day && Scene.Camera is { } camera && ctx.Ecs.IsAlive(camera))
            Render.SetLook(camera, Cycle.At(Sky.SunAltitude));
    }

    /// <summary>The day's look, keyed by the sun's height, which the camera follows while the graphics page asks for it.</summary>
    private static readonly LookCycle Cycle = LookCycle.Day();

    /// <summary>The day's look at the hour now.</summary>
    internal static LookSettings LookNow(FeatureSettings settings)
    {
        Sky.Latitude = settings.Latitude;
        return Cycle.At(Sky.SunAltitude);
    }

    /// <summary>
    /// Sets the weather's clock to this hour and latitude, held still between frames.
    /// </summary>
    private static void Hand(EcsWorld ecs, float latitude)
    {
        if (ecs.Resource<WeatherTimeRef>() is not { } clock) return;

        var day = Sky.Hour / 24f;
        if (!clock.Paused) clock.Paused = true;
        if (MathF.Abs(clock.TimeOfDay - day) > 1e-6f) clock.TimeOfDay = day;
        if (clock.Latitude != latitude) clock.Latitude = latitude;
        if (clock.AxialTilt != 0f) clock.AxialTilt = 0f;
    }

    /// <summary>
    /// Sets the hour, which the day runs on from, as the panel and the console do.
    /// </summary>
    internal static void SetHour(float hour)
    {
        Sky.Hour = Wrap(hour);
        Settings.Change(s => s with { Hour = Sky.Hour });
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

        Sky.Latitude = Settings.Current.Latitude;
        var minutes = (int)MathF.Round(Sky.Hour * 60f) % (24 * 60);
        return $"{minutes / 60:00}:{minutes % 60:00}, the sun {Sky.SunAltitude:0} degrees and the moon {Sky.MoonAltitude:0} degrees above the horizon, the moon {Sky.MoonLit:0%} lit";
    }

    /// <summary>
    /// Jumps to an hour set from elsewhere than <see cref="SetHour"/>, as the setting command sets
    /// one.
    /// </summary>
    private static void OnChanged(FeatureSettings was)
    {
        if (Settings.Current.Hour != was.Hour) Sky.Hour = Wrap(Settings.Current.Hour);
    }

    /// <summary>
    /// Points a light from a direction and gives it its strength and color, brought to nothing at
    /// the horizon, writing each only where it changed.
    /// </summary>
    private static void Shine(EcsWorld ecs, Entity light, Vec3 toward, (float Light, Color Color) given, bool shadows, bool fade = true)
    {
        if (light == Entity.None || !ecs.IsAlive(light)) return;

        // Looking at the scene from where the light comes from, with an up that is never along it.
        var up = MathF.Abs(toward.Y) > 0.99f ? Vec3.UnitZ : Vec3.UnitY;
        ecs.Set(light, Transform.LookingAt(toward, Vec3.Zero, up));

        var strength = fade ? given.Light * Math.Clamp(toward.Y / 0.05f, 0f, 1f) : given.Light;
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

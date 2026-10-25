using System.Numerics;

namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Draws the sky behind the atmosphere on a camera, the stars, the galaxy and the moon, standing
    /// where the settings' clock puts them, or with <see langword="null"/> takes it off. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// The sky is added over the sky's pixels before tonemapping, ahead of the clouds and the fog,
    /// so a cloud hides the stars behind it, each frame reading the clock the program moves on. It
    /// needs a camera drawn once a pixel (<see cref="PostSettings.Msaa"/> of one), whose depth says
    /// where the sky is. The setting lasts while the app runs.
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="settings">The sky, or null to take it off.</param>
    /// <exception cref="ArgumentOutOfRangeException">A brightness or the moon's size is under none or not a number, or the stars' grid has none.</exception>
    /// <example>
    /// <code>
    /// var clock = new SkyClock { Latitude = 50f, DayOfYear = 172f };
    /// Render.SetNightSky(camera, new NightSkySettings { Clock = clock });
    /// clock.Advance(ctx.Time.Delta / 60f);   // an hour a minute
    /// </code>
    /// </example>
    public static void SetNightSky(Entity camera, NightSkySettings? settings)
    {
        if (settings is null)
        {
            NightSky.Clear(camera);
            return;
        }

        ArgumentNullException.ThrowIfNull(settings.Clock);
        float[] numbers = [settings.Stars, settings.Twinkle, settings.Galaxy, settings.Moon, settings.MoonSize, settings.Earthshine];
        if (!numbers.All(n => float.IsFinite(n) && n >= 0f))
            throw new ArgumentOutOfRangeException(nameof(settings), "A brightness, the twinkle, the moon's size or the earthshine is under none or not a number.");
        if (!(settings.StarGrid >= 1f))
            throw new ArgumentOutOfRangeException(nameof(settings), settings.StarGrid, "The stars' grid has a cell along each face at least.");

        NightSky.Set(camera, settings);
    }
}

/// <summary>The draws <see cref="Render.SetNightSky"/> puts on cameras, an instance a camera, an app's own.</summary>
internal static class NightSky
{
    /// <summary>The name the sky's draw and prepass are kept under, ahead of the clouds and the fog by its order.</summary>
    internal const string Owner = "celestial";

    private static App? _app;
    private static float _seconds;
    private static readonly Dictionary<ulong, (ShaderInstance Draw, NightSkySettings Settings)> Skies = [];

    internal static void Set(Entity camera, NightSkySettings settings)
    {
        ForgetOtherApps();
        var draw = Skies.TryGetValue(camera.Bits, out var sky) ? sky.Draw : SceneField.Draw("night_sky.slang");
        Skies[camera.Bits] = (draw, settings);
        Feed(draw, settings);
        Shaders.SetEnginePrepass(camera, Owner, 1u);
        Shaders.SetEngineViewDraws(camera, Owner, ViewDraw.Fixed(draw, FramePoint.BeforeTonemapping, 3, 1, DrawBlend.Add, writesDepth: false));
    }

    internal static void Clear(Entity camera)
    {
        ForgetOtherApps();
        if (!Skies.Remove(camera.Bits)) return;
        Shaders.SetEngineViewDraws(camera, Owner);
        Shaders.SetEnginePrepass(camera, Owner, 0);
    }

    /// <summary>Whether every camera's sky has compiled, so a frame draws it.</summary>
    internal static bool Ready()
    {
        ForgetOtherApps();
        return Skies.Values.All(s => s.Draw.Program.State == ShaderProgramState.Ready);
    }

    /// <summary>Gives every camera's sky where its clock stands this frame, and the time it twinkles by.</summary>
    internal static void Tick(World world)
    {
        ForgetOtherApps();
        if (Skies.Count == 0) return;
        _seconds += world.Resource<Time>().Delta;
        foreach (var (draw, settings) in Skies.Values) Feed(draw, settings);
    }

    private static void Feed(ShaderInstance draw, NightSkySettings settings)
    {
        var clock = settings.Clock;
        var (moon, sun) = (clock.MoonDirection, clock.SunDirection);
        draw.Set("clock", new Vector4(clock.Latitude * MathF.PI / 180f, clock.SkyTurn, _seconds % 3600f, settings.Twinkle))
            .Set("moon", new Vector4(moon.X, moon.Y, moon.Z, settings.MoonSize * 0.5f * MathF.PI / 180f))
            .Set("sun", new Vector4(sun.X, sun.Y, sun.Z, settings.Earthshine))
            .Set("bright", new Vector4(settings.Stars, settings.Galaxy, settings.Moon, settings.StarGrid));
    }

    private static void ForgetOtherApps()
    {
        if (ReferenceEquals(_app, App.Running)) return;
        _app = App.Running;
        _seconds = 0f;
        Skies.Clear();
    }
}

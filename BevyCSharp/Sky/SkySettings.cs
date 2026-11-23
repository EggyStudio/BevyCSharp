using System.Runtime.CompilerServices;
using Diagnostics;

namespace Bevy;

/// <summary>
/// The sky's values the console's sky commands set, as settings of the Diagnostics module's
/// registry on the settings panel's <c>sky</c> page.
/// </summary>
/// <remarks>
/// <para>
/// The fog, its froxels and the clouds are a camera's, so each setting reads the first camera
/// drawing its part and sets every camera the command of its part sets, and a row of the panel
/// moves what <c>sky.fog</c>, <c>sky.froxels</c> and <c>sky.clouds</c> move. The commands stand
/// beside them for the scripts and guides that name them. A part no camera draws reads its default
/// and refuses a set with the reason, save the fog's visibility and the clouds' quality, which
/// start their part on the cameras the commands would start it on, and take it off at 0 and off.
/// </para>
/// <para>
/// Where the engine's weather gives a camera its fog or its clouds, it gives them again each frame
/// from the settings it starts from, so a set of that part there is refused with the reason, which
/// names <c>sky.weather</c> and <c>sky.forecast</c>, the weather's own. The weather's preset is no
/// setting, since the weather eases toward it and the forecast writes over it, so it holds no value
/// that reads back, and a strike of <c>sky.strike</c> lasts a moment. None is saved, as none of the
/// engine's settings is, since a game's scene gives its sky its own.
/// </para>
/// </remarks>
internal static class SkySettings
{
    internal static IReadOnlyList<Setting> All { get; } = [.. Fog(), .. Froxels(), .. CloudsOf(), .. Weather()];

    // Added as the assembly loads, as its commands are, so a line names them before an app draws.
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The area's commands are added from its assembly's initializer too, and the settings join them in the same registry.")]
    internal static void Register()
    {
        foreach (var setting in All) Registry.Process.Add(setting);
    }

    /// <summary>
    /// A part's quality as the commands take it, or off where no camera draws it.
    /// </summary>
    internal enum PartQuality
    {
        Off,
        Low,
        Medium,
        High,
    }

    // Why a set is refused where the part it sets is drawn on no camera.
    private const string NoFog = "No camera draws the sky's fog, which sky.fog_visibility starts.";
    private const string NoFroxels = "The sky's fog is worked out in closed form alone, which sky.froxel_quality changes.";
    private const string NoClouds = "No camera draws clouds, which sky.cloud_quality starts.";
    private const string NoWeather = "No camera has the engine's weather, which Render.SetWeather gives one.";
    private const string WeatherFog = "The engine's weather gives the camera its fog each frame, which sky.weather and sky.forecast set.";
    private const string WeatherClouds = "The engine's weather gives the camera its clouds each frame, which sky.weather and sky.forecast set.";

    // Whether the weather gives any of the cameras its fog, or its clouds.
    private static bool FogByWeather(IEnumerable<Entity> cameras) => cameras.Any(camera => SkyWeather.Gives(camera).Fog);
    private static bool CloudsByWeather(IEnumerable<Entity> cameras) => cameras.Any(camera => SkyWeather.Gives(camera).Clouds);

    // The fog's settings and the clouds' on the first camera drawing them, or none.
    private static SkyFogSettings? FirstFog() => SkyFog.All().Select(f => f.Settings).FirstOrDefault();
    private static CloudSettings? FirstClouds() => Clouds.All().Select(c => c.Settings).FirstOrDefault();

    // The sky's fog, how far it has gone to 95% at its base, how far up it thins by e and where its
    // base lies, which sky.fog sets together.
    private static IEnumerable<Setting> Fog()
    {
        var made = new SkyFogSettings();

        yield return new Setting<float>("sky.fog_visibility",
            "How far the sky's fog has gone to 95% at its base on the cameras drawing it, in meters, or 0 for none, as sky.fog sets it.", 0f,
            () => FirstFog()?.Visibility ?? 0f, apply: Visibility)
        { Low = 0, Step = 50, Saved = false };

        yield return new Setting<float>("sky.fog_height", "How far above its base the sky's fog thins by e, in meters, as sky.fog sets it.", made.Height,
            () => FirstFog()?.Height ?? made.Height, apply: Refog<float>((fog, height) => fog.Height = height))
        { Low = 1, Step = 10, Saved = false };

        yield return new Setting<float>("sky.fog_base", "The height the sky's fog is thickest at, in meters, as sky.fog sets it.", made.Base,
            () => FirstFog()?.Base ?? made.Base, apply: Refog<float>((fog, @base) => fog.Base = @base))
        { Step = 10, Saved = false };

        // Takes the fog off every camera drawing it at none, and otherwise sets it where sky.fog
        // would, which starts it where none draws it.
        static string? Visibility(float far)
        {
            if (far == 0f)
            {
                var fogged = SkyFog.All().Select(f => f.Camera).ToList();
                if (FogByWeather(fogged)) return WeatherFog;
                foreach (var camera in fogged) Render.ClearSkyFog(camera);
                return null;
            }

            var cameras = ConsoleSkyCommands.FogCameras();
            if (cameras.Count == 0) return "No camera draws the sky's fog, and the scene's distance field lies around none.";
            if (FogByWeather(cameras.Select(c => c.Camera))) return WeatherFog;
            foreach (var (camera, was) in cameras)
            {
                var fog = SkyWeather.Copy(was);
                fog.Visibility = far;
                Render.SetSkyFog(camera, fog);
            }
            return null;
        }
    }

    // A setter of a part of the fog on every camera drawing it, which refuses where none does.
    private static Func<T, string?> Refog<T>(Action<SkyFogSettings, T> change) => value =>
    {
        var fogged = SkyFog.All().ToList();
        if (fogged.Count == 0) return NoFog;
        if (FogByWeather(fogged.Select(f => f.Camera))) return WeatherFog;
        foreach (var (camera, was, _) in fogged)
        {
            var fog = SkyWeather.Copy(was);
            change(fog, value);
            Render.SetSkyFog(camera, fog);
        }
        return null;
    };

    // The froxels the fog is worked out in near the cameras, their quality and how far they reach,
    // which sky.froxels sets together.
    private static IEnumerable<Setting> Froxels()
    {
        var made = new FroxelFogSettings();

        yield return new Setting<PartQuality>("sky.froxel_quality",
            "The quality the sky's fog is worked out in froxels at near the cameras drawing it, or off for closed form alone, as sky.froxels sets it.", PartQuality.Off,
            () => FirstFog()?.Froxels is { } froxels ? Of(froxels.Quality) : PartQuality.Off, apply: Quality)
        { Saved = false };

        yield return new Setting<float>("sky.froxel_distance", "How far from the cameras the sky's froxels reach, in meters, as sky.froxels sets it.", made.Distance,
            () => FirstFog()?.Froxels?.Distance ?? made.Distance, apply: Distance)
        { Low = 2, Step = 25, Saved = false };

        static string? Quality(PartQuality quality)
        {
            var fogged = SkyFog.All().ToList();
            if (fogged.Count == 0) return NoFog;
            if (FogByWeather(fogged.Select(f => f.Camera))) return WeatherFog;
            FroxelQuality? asked = quality switch
            {
                PartQuality.Low => FroxelQuality.Low,
                PartQuality.Medium => FroxelQuality.Medium,
                PartQuality.High => FroxelQuality.High,
                _ => null,
            };
            foreach (var (camera, was, _) in fogged)
            {
                // A quality keeps the reach the froxels had, or takes the froxels' default.
                var fog = SkyWeather.Copy(was);
                fog.Froxels = asked is { } level
                    ? ConsoleSkyCommands.KeptFroxels(was.Froxels, level, was.Froxels?.Distance ?? new FroxelFogSettings().Distance)
                    : null;
                Render.SetSkyFog(camera, fog);
            }
            return null;
        }

        static string? Distance(float distance)
        {
            var froxeled = SkyFog.All().Where(f => f.Settings.Froxels is not null).ToList();
            if (froxeled.Count == 0) return SkyFog.All().Any() ? NoFroxels : NoFog;
            if (FogByWeather(froxeled.Select(f => f.Camera))) return WeatherFog;
            foreach (var (camera, was, _) in froxeled)
            {
                var fog = SkyWeather.Copy(was);
                fog.Froxels = ConsoleSkyCommands.KeptFroxels(was.Froxels, was.Froxels!.Quality, distance);
                Render.SetSkyFog(camera, fog);
            }
            return null;
        }
    }

    // The clouds over the cameras' sky, their quality and how much of it they cover, which
    // sky.clouds sets together.
    private static IEnumerable<Setting> CloudsOf()
    {
        var made = new CloudSettings();

        yield return new Setting<PartQuality>("sky.cloud_quality",
            "The quality the clouds over the cameras' sky are marched at, or off for none, as sky.clouds sets it.", PartQuality.Off,
            () => FirstClouds() is { } clouds ? Of(clouds.Quality) : PartQuality.Off, apply: Quality)
        { Saved = false };

        yield return new Setting<float>("sky.cloud_cover", "How much of the sky the clouds over the cameras cover, from 0 to 1, as sky.clouds sets it.", made.Coverage,
            () => FirstClouds()?.Coverage ?? made.Coverage, apply: Cover)
        { Low = 0, High = 1, Step = 0.05, Saved = false };

        // Takes the clouds off every camera drawing them at off, and otherwise sets them where
        // sky.clouds would, which starts them over the cameras drawing the sky's fog.
        static string? Quality(PartQuality quality)
        {
            if (quality == PartQuality.Off)
            {
                var clouded = Clouds.All().Select(c => c.Camera).ToList();
                if (CloudsByWeather(clouded)) return WeatherClouds;
                foreach (var camera in clouded) Render.SetClouds(camera, null);
                return null;
            }

            var cameras = ConsoleSkyCommands.CloudCameras();
            if (cameras.Count == 0) return "No camera draws clouds or the sky's fog.";
            if (CloudsByWeather(cameras.Select(c => c.Camera))) return WeatherClouds;
            foreach (var (camera, was) in cameras)
            {
                var clouds = was is null ? new CloudSettings() : SkyWeather.Copy(was);
                clouds.Quality = quality switch
                {
                    PartQuality.Low => CloudQuality.Low,
                    PartQuality.High => CloudQuality.High,
                    _ => CloudQuality.Medium,
                };
                Render.SetClouds(camera, clouds);
            }
            return null;
        }

        static string? Cover(float cover)
        {
            var clouded = Clouds.All().ToList();
            if (clouded.Count == 0) return NoClouds;
            if (CloudsByWeather(clouded.Select(c => c.Camera))) return WeatherClouds;
            foreach (var (camera, was) in clouded)
            {
                var clouds = SkyWeather.Copy(was);
                clouds.Coverage = cover;
                Render.SetClouds(camera, clouds);
            }
            return null;
        }
    }

    // Whether the forecast writes the weather on the cameras that have the engine's, which
    // sky.weather forecast and sky.weather still set.
    private static IEnumerable<Setting> Weather()
    {
        yield return new Setting<bool>("sky.forecast",
            "Whether the forecast writes the weather on the cameras that have the engine's, as sky.weather forecast and sky.weather still set it.", false,
            () => SkyWeather.All().Any(w => w.Weather.Forecast), apply: Forecast)
        { Saved = false };

        static string? Forecast(bool on)
        {
            var weathers = SkyWeather.All().ToList();
            if (weathers.Count == 0) return NoWeather;
            foreach (var (_, weather) in weathers) weather.Forecast = on;
            return null;
        }
    }

    private static PartQuality Of(FroxelQuality quality) => quality switch
    {
        FroxelQuality.Low => PartQuality.Low,
        FroxelQuality.High => PartQuality.High,
        _ => PartQuality.Medium,
    };

    private static PartQuality Of(CloudQuality quality) => quality switch
    {
        CloudQuality.Low => PartQuality.Low,
        CloudQuality.High => PartQuality.High,
        _ => PartQuality.Medium,
    };
}

namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Gives a camera's clouds and fog the weather, moving it on every frame, or with
    /// <see langword="null"/> takes it off and leaves them as they last were. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every frame the weather moves on by the game's clock (<see cref="WeatherState.Advance"/>)
    /// and its conditions are laid over the settings given here. The clouds take its cover, its
    /// share of a flat deck, its base, top and density, and its wind, which the fog's froxels
    /// drift on too. The fog's visibility thins toward some eighty meters as its fog thickens. The
    /// rain and the snow fall as hard as it rains and snows, on the gusting wind. Any set of settings
    /// can be none, and that part is left to the program.
    /// </para>
    /// <para>
    /// The settings given are kept as the weather's starting point and not changed, and the camera
    /// is given new ones every frame, which change its clouds', fog's and rain's numbers alone.
    /// Thunder strikes once the engine draws it, and the ground's wetness and snow cover are the
    /// state's for a program's materials to read.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="weather">The weather, or null to take it off.</param>
    /// <param name="clouds">The clouds the weather starts from, or none to leave the camera's clouds alone.</param>
    /// <param name="fog">The fog the weather starts from, or none to leave the camera's fog alone.</param>
    /// <param name="precipitation">The rain and snow the weather starts from, or none to leave the camera's alone.</param>
    /// <example>
    /// <code>
    /// var weather = new WeatherState { Forecast = true, Clock = clock };
    /// Render.SetWeather(camera, weather, new CloudSettings { ShadowLight = sun }, new SkyFogSettings { Visibility = 900f }, new PrecipitationSettings());
    /// weather.Set(WeatherPreset.Rain);   // eases into rain over a minute or so
    /// </code>
    /// </example>
    public static void SetWeather(Entity camera, WeatherState? weather, CloudSettings? clouds = null, SkyFogSettings? fog = null, PrecipitationSettings? precipitation = null)
    {
        if (weather is null)
        {
            SkyWeather.Clear(camera);
            return;
        }

        SkyWeather.Set(camera, weather, clouds, fog, precipitation);
        SkyWeather.Apply(camera, weather, clouds, fog, precipitation);
    }
}

/// <summary>The weathers <see cref="Render.SetWeather"/> puts on cameras, an app's own, moved on every frame.</summary>
internal static class SkyWeather
{
    private static App? _app;
    private static readonly Dictionary<ulong, (WeatherState Weather, CloudSettings? Clouds, SkyFogSettings? Fog, PrecipitationSettings? Precipitation)> Weathers = [];

    internal static void Set(Entity camera, WeatherState weather, CloudSettings? clouds, SkyFogSettings? fog, PrecipitationSettings? precipitation)
    {
        ForgetOtherApps();
        Weathers[camera.Bits] = (weather, clouds, fog, precipitation);
    }

    internal static void Clear(Entity camera)
    {
        ForgetOtherApps();
        Weathers.Remove(camera.Bits);
    }

    /// <summary>The cameras with the engine's weather in the running app, and each one's weather.</summary>
    internal static IEnumerable<(Entity Camera, WeatherState Weather)> All()
    {
        ForgetOtherApps();
        return Weathers.Select(w => (new Entity(w.Key), w.Value.Weather));
    }

    /// <summary>Moves every camera's weather on a frame and gives the camera its clouds and fog.</summary>
    internal static void Tick(World world)
    {
        ForgetOtherApps();
        if (Weathers.Count == 0) return;
        var delta = world.Resource<Time>().Delta;
        foreach (var (camera, (weather, clouds, fog, precipitation)) in Weathers)
        {
            weather.Advance(delta);
            Apply(new Entity(camera), weather, clouds, fog, precipitation);
        }
    }

    /// <summary>The camera's clouds, fog and rain under the weather's conditions now, laid over the settings it starts from.</summary>
    internal static void Apply(Entity camera, WeatherState weather, CloudSettings? clouds, SkyFogSettings? fog, PrecipitationSettings? precipitation)
    {
        var now = weather.Current;
        if (clouds is not null)
        {
            var given = Copy(clouds);
            given.Coverage = Math.Clamp(now.CloudCover, 0f, 1f);
            given.Stratus = Math.Clamp(now.Stratus, 0f, 1f);
            given.Bottom = Math.Max(0f, now.CloudBase);
            given.Top = Math.Max(given.Bottom + 100f, now.CloudTop);
            given.Density = clouds.Density * Math.Max(0.01f, now.CloudDensity);
            given.Wind = weather.Wind;
            Render.SetClouds(camera, given);
        }

        if (fog is not null)
        {
            // Thinning in log space toward some eighty meters as the fog thickens, as visibility
            // falls by tenfold steps from a haze to a fog.
            var given = Copy(fog);
            var thick = Math.Clamp(now.Fog, 0f, 1f);
            given.Visibility = MathF.Exp(MathF.Log(fog.Visibility) + ((MathF.Log(80f) - MathF.Log(fog.Visibility)) * thick));
            if (fog.Froxels is { } froxels)
            {
                given.Froxels = new FroxelFogSettings
                {
                    Quality = froxels.Quality,
                    Distance = froxels.Distance,
                    Lumps = froxels.Lumps,
                    LumpSize = froxels.LumpSize,
                    Wind = weather.Wind,
                    Lights = froxels.Lights,
                    Boxes = froxels.Boxes,
                };
            }
            Render.SetSkyFog(camera, given);
        }

        if (precipitation is not null)
        {
            Render.SetPrecipitation(camera, new PrecipitationSettings
            {
                Rain = Math.Clamp(now.Rain, 0f, 1f),
                Snow = Math.Clamp(now.Snow, 0f, 1f),
                Wind = weather.Wind,
                Quality = precipitation.Quality,
                Reach = precipitation.Reach,
                Roofs = precipitation.Roofs,
                RoofLayers = precipitation.RoofLayers,
                Splashes = precipitation.Splashes,
            });
        }
    }

    internal static CloudSettings Copy(CloudSettings was) => new()
    {
        Quality = was.Quality,
        Coverage = was.Coverage,
        Stratus = was.Stratus,
        Bottom = was.Bottom,
        Top = was.Top,
        Density = was.Density,
        ShapeSize = was.ShapeSize,
        DetailSize = was.DetailSize,
        Erosion = was.Erosion,
        WeatherSize = was.WeatherSize,
        Wind = was.Wind,
        Distance = was.Distance,
        Ambient = was.Ambient,
        Seed = was.Seed,
        ShadowLight = was.ShadowLight,
        ShadowSize = was.ShadowSize,
        ShadowStrength = was.ShadowStrength,
    };

    internal static SkyFogSettings Copy(SkyFogSettings was) => new()
    {
        Visibility = was.Visibility,
        Start = was.Start,
        Height = was.Height,
        Base = was.Base,
        HorizonDegrees = was.HorizonDegrees,
        GroundShade = was.GroundShade,
        BandDegrees = was.BandDegrees,
        Color = was.Color,
        Brightness = was.Brightness,
        Froxels = was.Froxels,
    };

    private static void ForgetOtherApps()
    {
        if (ReferenceEquals(_app, App.Running)) return;
        _app = App.Running;
        Weathers.Clear();
    }
}
